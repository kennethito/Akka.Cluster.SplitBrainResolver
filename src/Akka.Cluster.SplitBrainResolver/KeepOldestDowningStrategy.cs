using System.Collections.Generic;
using System.Linq;
using Akka.Configuration;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver
{
    public sealed class KeepOldestDowningStrategy : IDowningStrategy
    {
        private readonly string role;
        private readonly bool downIfAlone;

        public KeepOldestDowningStrategy(string role = null, bool downIfAlone = false)
        {
            this.role = role;
            this.downIfAlone = downIfAlone;
        }

        /// <summary>
        /// Creates a KeepOldestDowningStrategy getting downIfAlone from config.
        /// Uses
        ///     akka.cluster.split-brain-resolver.keep-oldest.role
        ///     akka.cluster.split-brain-resolver.keep-oldest.down-if-alone
        /// </summary>
        /// <param name="config"></param>
        public KeepOldestDowningStrategy(Config config)
            : this(
                config.GetString("akka.cluster.split-brain-resolver.keep-oldest.role"),
                config.GetBoolean("akka.cluster.split-brain-resolver.keep-oldest.down-if-alone")
            )
        {
        }

        public IEnumerable<Member> GetVictims(CurrentClusterState clusterState)
        {
            var oldest = clusterState.GetMembers(this.role).SortByAge().FirstOrDefault();
            var available = clusterState.GetAvailableMembers(this.role);
            var haveOldest = available.Contains(oldest);
            var oldestIsAlone =
                (haveOldest && available.Count == 1)
                || (!haveOldest && available.Count == clusterState.GetMembers(this.role).Count - 1);

            if(oldest == null)
                return Enumerable.Empty<Member>();

            if(oldestIsAlone && this.downIfAlone)
                return new List<Member> { oldest };

            return haveOldest
                ? clusterState.GetUnreachableMembers(this.role)
                : clusterState.GetMembers(this.role);
        }
    }
}