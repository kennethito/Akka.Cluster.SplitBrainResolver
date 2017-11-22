using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Cluster;
using Akka.Cluster.SplitBrainResolver;
using Akka.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterManager
{
    class Program
    {
        async static Task Main(string[] args)
        {
            int lastPort = 9000;
            Func<ActorSystem> CreateSystemFactory()
            {
                int port = lastPort++;
                return () => ActorSystem.Create("test-system", GetConfig(port));
            }

            void CreateHeartbeat(ActorSystem system)
            {
                system.ActorOf((dsl, context) =>
                {
                    dsl.Receive<string>((msg, ctx) => Console.WriteLine($"[{Cluster.Get(context.System).SelfAddress}]: {msg}"));

                    context.System.Scheduler.ScheduleTellRepeatedly(
                        initialDelay: TimeSpan.Zero,
                        interval: TimeSpan.FromSeconds(5),
                        receiver: context.Self,
                        message: "heartbeat",
                        sender: context.Self);
                });
            }

            var clusters = new Dictionary<Address, Cluster>();

            Action<ActorSystem> CreateSystemBootstraper()
            {
                return system =>
                {
                    var cluster = Cluster.Get(system);
                    system.Log.Info($"Bootstrapping {cluster.SelfAddress}");

                    clusters[cluster.SelfAddress] = cluster;

                    CreateHeartbeat(system);
                };
            };

            var memberStore = new MemoryClusterMemberStore();
            var unreachable = Address.Parse("akka.tcp://test-system@unreachable-node:9999");

            var sb = new StringBuilder();
            sb.AppendLine("add system: adds an actor system");
            sb.AppendLine("add unreachable: adds an unreachable node");
            sb.AppendLine("remove unreachable: removes an unreachable node");
            sb.AppendLine("get members: lists all members");
            sb.AppendLine("down: downs all members");
            sb.AppendLine("exit");
            sb.AppendLine("?");
            var help = sb.ToString();

            Console.WriteLine(help);

            while (true)
            {
                string command = Console.ReadLine();

                switch(command)
                {
                    case "add system":
                        var settings = new ClusterManagerSettings(
                            memberStore,
                            CreateSystemFactory(),
                            CreateSystemBootstraper(),
                            ClusterManagerRestartMode.Always);

                        await Akka.Cluster.SplitBrainResolver.ClusterManager.Start(settings);

                        break;
                    case "add unreachable":
                        await memberStore.Add(unreachable);
                        break;
                    case "remove unreachable":
                        await memberStore.Remove(unreachable);
                        break;
                    case "get members":
                        var seeds = await memberStore.GetMembers();
                        Console.WriteLine(string.Join(',', seeds.Select(s => s.ToString())));
                        break;
                    case "down":
                        clusters.Values.ToList().ForEach(cluster => cluster.Down(cluster.SelfAddress));
                        break;
                    case "exit":
                        return;
                    case "?":
                        Console.WriteLine(help);
                        break;
                    default:
                        Console.WriteLine("Unknown command");
                        break;
                }
            }
        }

        private static Config GetConfig(int port)
        {
            return
                ConfigurationFactory
                    .ParseString(@"
                         akka {
                             actor.provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                             remote {
                                 dot-netty.tcp {
                                     hostname = localhost
                                 }
                             }
                             cluster {
                                 downing-provider-class = 
                                    ""Akka.Cluster.SplitBrainResolver.SplitBrainResolverDowningProvider, 
                                    Akka.Cluster.SplitBrainResolver""
                                 split-brain-resolver {
                                     stable-after = 10s
                                     active-strategy = keep-oldest
                                 }
                                 auto-down-unreachable-after = off
                             }
                         }
                     ")
                     .WithFallback(ConfigurationFactory.ParseString($"akka.remote.dot-netty.tcp.port = {port}"))
                     .WithFallback(ConfigurationFactory.Default());
        }
    }
}
