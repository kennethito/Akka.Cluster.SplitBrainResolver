using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Remote;
using System.Threading;

namespace Akka.Cluster.SplitBrainResolver
{
    [Flags]
    public enum ClusterManagerRestartMode
    {
        Never = 0,
        WhenQuarantined = 1 << 0,
        WhenTerminated = 1 << 1,
        WhenRemoved = 1 << 2,
        Always = WhenQuarantined | WhenTerminated | WhenRemoved
    }

    public class ClusterManagerSettings
    {
        public ClusterManagerSettings(
            IClusterMemberStore memberStore, 
            Func<ActorSystem> actorSystemFactory,
            Action<ActorSystem> bootstrapActors,
            ClusterManagerRestartMode restartMode)
        {
            MemberStore = memberStore;
            ActorSystemFactory = actorSystemFactory;
            BootstrapActors = bootstrapActors;
            RestartMode = restartMode;
        }

        public IClusterMemberStore MemberStore { get; }
        public Func<ActorSystem> ActorSystemFactory { get; }
        public Action<ActorSystem> BootstrapActors { get; }
        public ClusterManagerRestartMode RestartMode { get; }
    }

    /// <summary>
    /// This class holds the current actor system.  Every actor system restart will
    /// result in a new actor system, which will be updated into this reference
    /// </summary>
    public class ActorSystemRef
    {
        public ActorSystem Value { get; internal set; }
    }

    public static class ClusterManager
    {
        public static async Task<ActorSystemRef> Start(ClusterManagerSettings settings)
        {
            var systemRef = new ActorSystemRef();
            await Start(systemRef, settings);

            return systemRef;
        }

        public static async Task Stop(ActorSystemRef systemRef, IClusterMemberStore memberStore)
        {
            if (systemRef.Value == null)
                return;

            var cluster = Cluster.Get(systemRef.Value);
            var clusterAddress = cluster.SelfAddress;

            await memberStore.Remove(clusterAddress);
            await systemRef.Value.Terminate();
            systemRef.Value.Dispose();
        }

        private static async Task Start(ActorSystemRef systemRef, ClusterManagerSettings settings)
        {
            var system = settings.ActorSystemFactory();
            systemRef.Value = system;

            var cluster = Cluster.Get(system);

            var restartLock = new object();
            bool isRestarting = false;
            void StopNowAndRestartIn(TimeSpan scheduleAt)
            {
                if (isRestarting)
                    return;

                lock (restartLock)
                {
                    if (isRestarting)
                        return;

                    isRestarting = true;
                }

                Stop(systemRef, settings.MemberStore).ContinueWith(previous =>
                {
                    if (previous.Exception != null)
                        system.Log.Error(previous.Exception, "Error Stopping Actor System");

                    ScheduleIn(scheduleAt, () =>
                    {
                        Start(systemRef, settings).Unawaited(system);
                    });
                });
            }

            void RestartNow() => StopNowAndRestartIn(TimeSpan.Zero);

            bool isUp = false;
            ScheduleIn(TimeSpan.FromSeconds(10), () =>
            {
                if (!isUp)
                {
                    system.Log.Info($"This actor system [{cluster.SelfAddress}] not able to join seed nodes, attempting to restart");
                    StopNowAndRestartIn(TimeSpan.FromSeconds(5));
                }
            });

            cluster.RegisterOnMemberUp(() =>
            {
                system.Log.Info($"This actor system [{cluster.SelfAddress}] UP, bootstrapping actors");

                isUp = true;
                settings.MemberStore.Add(cluster.SelfAddress).Unawaited(system);
                settings.BootstrapActors(system);
            });

            if (settings.RestartMode.HasFlag(ClusterManagerRestartMode.WhenQuarantined))
            {
                system.ActorOf((dsl, context) =>
                {
                    context.System.EventStream.Subscribe(context.Self, typeof(ThisActorSystemQuarantinedEvent));

                    dsl.Receive<ThisActorSystemQuarantinedEvent>((msg, ctx) =>
                    {
                        system.Log.Info($"This actor system [{cluster.SelfAddress}] quarantined, attempting to restart");

                        RestartNow();
                    });
                });
            }

            if (settings.RestartMode.HasFlag(ClusterManagerRestartMode.WhenRemoved))
            {
                cluster.RegisterOnMemberRemoved(() =>
                {
                    system.Log.Info($"This actor system [{cluster.SelfAddress}] removed from cluster, attempting to restart");

                    RestartNow();
                });
            }

            if (settings.RestartMode.HasFlag(ClusterManagerRestartMode.WhenTerminated))
            {
                system.RegisterOnTermination(() =>
                {
                    system.Log.Info($"This actor system [{cluster.SelfAddress}] terminating, attempting to restart");

                    RestartNow();
                });
            }

            await JoinCluster(cluster, settings.MemberStore);
        }

        private static async Task JoinCluster(Cluster cluster, IClusterMemberStore memberStore)
        {
            var members = await memberStore.GetMembers();

            if(members.Any())
            {
                //Start trying to join existing members, we will only add ourself
                //as a member (which other nodes may try to join) if we are able to come up
                //in the existing cluster
                cluster.JoinSeedNodes(members);
            }
            else
            {
                var success = await memberStore.Add(cluster.SelfAddress);

                if(success)
                    //if we successfully register ourself as the first member
                    //then go ahead and start a cluster where we are the first node
                    cluster.JoinSeedNodes(new List<Address>{ cluster.SelfAddress });
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await JoinCluster(cluster, memberStore);
                }
            }
        }

        private static void ScheduleIn(TimeSpan due, Action action)
        {
            Timer timer = null;
            timer = new Timer(
                state =>
                {
                    try
                    {
                        action();
                    }
                    finally
                    {
                        timer?.Dispose();
                    }
                },
                state: null,
                dueTime: due,
                period: TimeSpan.FromMilliseconds(-1));
        }
    }

    internal static class TaskExtensions
    {
        public static void Unawaited(this Task task, ActorSystem actorSystem)
        {
            task.ContinueWith(previous => 
            {
                if (previous.Exception != null)
                    actorSystem.Log.Error(previous.Exception, "Error in unawaited task");
            });
        }
    }
}