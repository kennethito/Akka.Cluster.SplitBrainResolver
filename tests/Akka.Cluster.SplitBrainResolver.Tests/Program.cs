//using Akka.Actor;
//using Akka.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static Akka.Cluster.ClusterEvent;

//namespace Akka.Cluster.SplitBrainResolver.Tests
//{
//    //Just for some manual testing convenience
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            // var config =
//            //     ConfigurationFactory
//            //         .ParseString(@"
//            //             akka {
//            //                 actor.provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
//            //                 remote {
//            //                     dot-netty.tcp {
//            //                         hostname = localhost
//            //                         port = 8001
//            //                     }
//            //                 }
//            //                 cluster {
//            //                     downing-provider-class = ""Akka.Cluster.SplitBrainResolver.SplitBrainResolverDowningProvider, Akka.Cluster.SplitBrainResolver""
//            //                     split-brain-resolver {
//            //                         # off, static-quorum, keep-majority, keep-oldest, keep-referee 
//            //                         active-strategy = keep-referee
//            //                         stable-after = 20s

//            //                         keep-referee.address = ""akka.tcp://test-system@localhost:8001""
//            //                     }
//            //                     auto-down-unreachable-after = off
//            //                     seed-nodes = [""akka.tcp://test-system@localhost:8001""]
//            //                 }
//            //             }
//            //         ")
//            //         .WithFallback(ConfigurationFactory.Default());

//            // var system = ActorSystem.Create("test-system", config);
//            // Console.Read();
//        }
//    }
//}
