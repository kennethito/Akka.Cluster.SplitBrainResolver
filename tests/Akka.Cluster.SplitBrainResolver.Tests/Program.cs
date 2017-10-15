using Akka.Actor;
using Akka.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver.Tests
{
    //Just for some manual testing convenience
    public class Program
    {
        public static void Main(string[] args)
        {
            var config =
                ConfigurationFactory
                    .ParseString(@"
                        akka {
                            loglevel = INFO
                            actor.provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                            remote {
                                dot-netty.tcp {
                                    hostname = localhost
                                    port = 8001
                                }
                            }
                            cluster {
                                downing-provider-class = ""Akka.Cluster.SplitBrainResolver.Tests.TestDowningProvider, Akka.Cluster.SplitBrainResolver.Tests""
                                seed-nodes = [""akka.tcp://test-system@localhost:8001""]
                            }
                        }   
                    ")
                    .WithFallback(ConfigurationFactory.Default());

            var system = ActorSystem.Create("test-system", config);
            Console.Read();
        }
    }

    class TestDowningProvider : StrategizedDowningProvider
    {
        public TestDowningProvider(ActorSystem system)
            : base(system)
        {
        }

        protected override IDowningStrategy GetDowningStrategy()
        {
            return new TestStrategy();
        }
    }

    class TestStrategy : IDowningStrategy
    {
        public IEnumerable<Member> GetVictims(CurrentClusterState clusterState)
        {
            return Enumerable.Empty<Member>();
        }
    }
}
