using Akka.Cluster.TestKit;
using Akka.Configuration;
using Akka.Remote.TestKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster.Tests.MultiNode;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver.Tests
{
    public class ClusterListenerConfig : MultiNodeConfig
    {
        public RoleName First { get; set; }
        public RoleName Second { get; set; }
        public RoleName Third { get; set; }
        public RoleName Fourth { get; set; }
        public RoleName Fifth { get; set; }

        public ClusterListenerConfig()
        {
            First = Role("first");
            Second = Role("second");
            Third = Role("third");
            Fourth = Role("fourth");
            Fifth = Role("fifth");

            //Logfiles will be found at
            // \Akka.Cluster.SplitBrainResolver\tests\Akka.Cluster.SplitBrainResolver.Tests\bin\Debug\net452\
            //      mntr\Akka.MultiNodeTestRunner.1.3.2\lib\net452\Akka.Cluster.SplitBrainResolver.Tests.ClusterListenerSpec
            bool enabledVerboseLogging = false;

            CommonConfig =
                MultiNodeLoggingConfig.LoggingConfig
                    .WithFallback(DebugConfig(enabledVerboseLogging))
                    .WithFallback(ConfigurationFactory.ParseString(@"
                         akka {
                             cluster {
                                 downing-provider-class = 
                                    ""Akka.Cluster.SplitBrainResolver.Tests.ClusterListenerDowningProvider, 
                                      Akka.Cluster.SplitBrainResolver.Tests""
                                 split-brain-resolver {
                                     stable-after = 1s
                                 }
                                 auto-down-unreachable-after = off
                             }
                         }
                     "))
                    .WithFallback(MultiNodeClusterSpec.ClusterConfig(true));

            TestTransport = true;
        }
    }

    public class BroadcastingDowningStrategy : IDowningStrategy
    {
        private readonly ActorSystem _system;

        public BroadcastingDowningStrategy(ActorSystem system)
        {
            _system = system;
        }

        public IEnumerable<Member> GetVictims(CurrentClusterState clusterState)
        {
            _system.EventStream.Publish(new Execution());

            return Enumerable.Empty<Member>();
        }

        public class Execution { }
    }

    public class ClusterListenerDowningProvider : StrategizedDowningProvider
    {
        public ClusterListenerDowningProvider(ActorSystem system)
            : base(system, "split-brain-resolver")
        {

        }

        protected override IDowningStrategy GetDowningStrategy()
        {
            return new BroadcastingDowningStrategy(System);
        }
    }

    public class ClusterListenerSpec : MultiNodeClusterSpec
    {
        private readonly ClusterListenerConfig _config;
        private List<RoleName> _nodes;

        public ClusterListenerSpec() : this(new ClusterListenerConfig())
        {
        }

        private ClusterListenerSpec(ClusterListenerConfig config) : base(config, typeof(ClusterListenerSpec))
        {
            _config = config;
            _nodes = new List<RoleName> { _config.First, _config.Second, _config.Third, _config.Fourth, _config.Fifth };
        }

        [MultiNodeFact]
        public void ClusterListenerSpecs()
        {
            AwaitClusterUp(_nodes.ToArray());
            AwaitMembersUp(_nodes.Count);

            EnterBarrier("cluster up");

            ClusterListenerShouldExecuteOnLeaderWhenStable();
        }

        private void ClusterListenerShouldExecuteOnLeaderWhenStable()
        {
            RunOn(() =>
            {
                Sys.EventStream.Subscribe(TestActor, typeof(BroadcastingDowningStrategy.Execution));

                if (Cluster.State.Leader.Equals(Cluster.SelfAddress))
                {
                    Log.Info($"Leader {Cluster.SelfAddress}, expecting check downing execution");
                    ExpectMsg<BroadcastingDowningStrategy.Execution>();
                }
                else
                {
                    Log.Info($"Non leader {Cluster.SelfAddress} (leader = {Cluster.State.Leader}), no message expected");
                    ExpectNoMsg();
                }
            }, _nodes.ToArray());
        }
    }
}
