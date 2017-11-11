using Akka.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver
{
    public sealed class KeepMajorityDowningStrategy : IDowningStrategy
    {
        public string Role { get; }

        public KeepMajorityDowningStrategy(string role = null)
        {
            Role = role;
        }

        public KeepMajorityDowningStrategy(Config config)
            : this(
                role: config.GetString("akka.cluster.split-brain-resolver.keep-majority.role"))
        {
        }

        /// <summary>
        /// The strategy named keep-majority will down the unreachable nodes if the 
        /// current node is in the majority part based on the last known membership information.
        /// Otherwise down the reachable nodes, i.e. the own part. If the parts are of equal size
        /// the part containing the node with the lowest address is kept.
        /// </summary>
        public IEnumerable<Member> GetVictims(CurrentClusterState clusterState)
        {
            var members = clusterState.GetMembers(Role);
            var unreachableMembers = clusterState.GetUnreachableMembers(Role);
            var availableMembers = clusterState.GetAvailableMembers(Role);

            int unreachableCount = unreachableMembers.Count;
            int availableCount = availableMembers.Count;

            if (availableCount == unreachableCount)
            {
                var oldest = clusterState.GetMembers(Role).SortByAge().FirstOrDefault();
                if (availableMembers.Contains(oldest))
                {
                    return unreachableMembers;
                }
                else
                {
                    return members;
                }
            }

            return availableCount < unreachableCount
                //too few available, down our partition (entire members)
                ? members
                //enough available, down unreachable
                : unreachableMembers;
        }
    }
}
