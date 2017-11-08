using Akka.Actor;
using Akka.Configuration;
using System;

namespace CustomDowningProvider
{
    class Program
    {
        static void Main(string[] args)
        {
            var config =
                ConfigurationFactory
                    .ParseString(@"
                         akka {
                             actor.provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                             remote {
                                 dot-netty.tcp {
                                     hostname = localhost
                                     port = 8001
                                 }
                             }
                             cluster {
                                 downing-provider-class = ""CustomDowningProvider.MyDowningProvider, CustomDowningProvider""
                                 my-downing-provider {
                                     stable-after = 3s
                                     example-config-entry = 10
                                 }
                                 auto-down-unreachable-after = off
                                 seed-nodes = [""akka.tcp://test-system@localhost:8001""]
                             }
                         }
                     ")
                    .WithFallback(ConfigurationFactory.Default());

            var system = ActorSystem.Create("test-system", config);
            Console.Read();
        }
    }
}
