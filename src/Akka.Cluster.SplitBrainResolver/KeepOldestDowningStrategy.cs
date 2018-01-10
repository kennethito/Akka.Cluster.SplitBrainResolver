using System.Collections.Generic;
using System.Linq;
using Akka.Configuration;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver
{
    public sealed class KeepOldestDowningStrategy : IDowningStrategy
    {
        public string Role { get; }
        public bool DownIfAlone { get; }

        public KeepOldestDowningStrategy(string role = null, bool downIfAlone = false)
        {
            Role = role;
            DownIfAlone = downIfAlone;
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
            var oldest = clusterState.GetMembers(Role).SortByAge().FirstOrDefault();
            var available = clusterState.GetAvailableMembers(Role);
            var haveOldest = oldest != null && available.Contains(oldest);
            var oldestIsAlone =
                (haveOldest && available.Count == 1)
                || (!haveOldest && available.Count == clusterState.GetMembers(Role).Count - 1);

            if(oldest == null)
                return Enumerable.Empty<Member>();

            if(oldestIsAlone && DownIfAlone)
                return new List<Member> { oldest };

            return haveOldest
                ? clusterState.GetUnreachableMembers()
                : clusterState.GetMembers();
        }
    }
}