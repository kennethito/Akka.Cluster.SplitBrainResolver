using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver
{
    public class ClusterListener : ReceiveActor, IWithUnboundedStash
    {
        private readonly Cluster cluster;
        private readonly TimeSpan stableAfter;
        private readonly IDowningStrategy downingStrategy;

        private readonly ILoggingAdapter log = Logging.GetLogger(Context);

        public IStash Stash { get; set; }

        public ClusterListener(TimeSpan stableAfter, IDowningStrategy downingStrategy)
        {
            this.downingStrategy = downingStrategy;

            cluster = Cluster.Get(Context.System);
            this.stableAfter = stableAfter;

            cluster.Subscribe(
                Self,
                SubscriptionInitialStateMode.InitialStateAsSnapshot,
                new[] { typeof(IClusterDomainEvent) });

            Receive<CurrentClusterState>(msg => 
                Become(() => WaitingForStability(msg)));
        }

        private void Stable(CurrentClusterState clusterState)
        {
            Receive<IClusterDomainEvent>(msg => 
            {
                Stash.Stash();
                Become(() => WaitingForStability(clusterState));
                Stash.Unstash();
            });

            if (clusterState.Leader != null && clusterState.Leader.Equals(cluster.SelfAddress))
            {
                log.Info($"Checking downing strategy {downingStrategy.GetType().Name} for leader {clusterState.Leader} on node {cluster.SelfAddress}");

                foreach (var victim in downingStrategy.GetVictims(clusterState))
                {
                    log.Info($"Downing victim {victim}");
                    cluster.Down(victim.Address);
                }
            }
        }

        private void WaitingForStability(CurrentClusterState clusterState)
        {
            log.Info($"Waiting {stableAfter.TotalSeconds} seconds for cluster stability");

            var timeoutHandle = 
                Context.System.Scheduler.ScheduleTellOnceCancelable(
                    stableAfter,
                    Self,
                    new Timeout(),
                    Self);

            Receive<Timeout>(msg => 
            {
                Become(() => Stable(clusterState));
            });

            Receive<IClusterDomainEvent>(msg =>
            {
                timeoutHandle.CancelIfNotNull();

                CurrentClusterState newState;

                switch(msg)
                {
                    case LeaderChanged changed:
                        newState = clusterState.Copy(leader: changed.Leader);
                        break;
                    case RoleLeaderChanged changed:
                        var roleLeaders = 
                            clusterState.AllRoles
                                .Select(role => (Role: role, Leader: clusterState.RoleLeader(role)))
                                .Where(t => t.Leader != null)
                                .ToImmutableDictionary(t => t.Role, t => t.Leader);

                        newState = clusterState.Copy(roleLeaderMap: roleLeaders.SetItem(changed.Role, changed.Leader));
                        break;
                    case MemberRemoved member:
                        newState = clusterState.Copy(members: clusterState.Members.Remove(member.Member));
                        break;
                    case MemberUp member:
                        newState = clusterState.Copy(members: clusterState.Members.Add(member.Member));
                        break;
                    case UnreachableMember member:
                        newState = clusterState.Copy(unreachable: clusterState.Unreachable.Add(member.Member));
                        break;
                    case ReachableMember member:
                        newState = clusterState.Copy(unreachable: clusterState.Unreachable.Remove(member.Member));
                        break;
                    default:
                        newState = clusterState;
                        break;
                }

                Become(() => WaitingForStability(newState));
            });
        }

        private class Timeout { }
    }
}
