using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Cluster.SplitBrainResolver
{
    public abstract class StrategizedDowningProvider : IDowningProvider
    {
        protected ActorSystem System { get; private set; }

        public StrategizedDowningProvider(ActorSystem system)
        {
            System = system;
        }

        /// <summary>
        /// Time margin after which shards or singletons that belonged to a 
        /// downed/removed partition are created in surviving partition. The
        /// purpose of this margin is that in case of a network partition the 
        /// persistent actors in the non-surviving partitions must be stopped 
        /// before corresponding persistent actors are started somewhere else. 
        /// This is useful if you implement downing strategies that handle 
        /// network partitions, e.g. by keeping the larger side of the partition 
        /// and shutting down the smaller side.
        /// </summary>
        public virtual TimeSpan DownRemovalMargin =>
            System.Settings.Config.GetTimeSpan(
                "akka.cluster.split-brain-resolver.down-removal-margin",
                @default: TimeSpan.FromSeconds(10));

        /// <summary>
        /// If a props is returned it is created as a child of the core cluster 
        /// daemon on cluster startup. It should then handle downing using the regular 
        /// Cluster APIs. The actor will run on the same dispatcher as the cluster actor 
        /// if dispatcher not configured. May throw an exception which will then 
        /// immediately lead to Cluster stopping, as the downing provider is vital 
        /// to a working cluster.
        /// </summary>
        public Props DowningActorProps =>
            Props.Create(() =>
                new ClusterListener(
                    StableAfter,
                    GetDowningStrategy()));

        protected abstract IDowningStrategy GetDowningStrategy();

        /// <summary>
        /// All strategies are inactive until the cluster membership and the information 
        /// about unreachable nodes have been stable for a certain time period. Continuously 
        /// adding more nodes while there is a network partition does not influence this 
        /// timeout, since the status of those nodes will not be changed to Up while there 
        /// are unreachable nodes. Joining nodes are not counted in the logic of the strategies.
        /// </summary>
        protected virtual TimeSpan StableAfter => 
            System.Settings.Config.GetTimeSpan(
                "akka.cluster.split-brain-resolver.stable-after",
                @default: TimeSpan.FromSeconds(10));
    }
}
