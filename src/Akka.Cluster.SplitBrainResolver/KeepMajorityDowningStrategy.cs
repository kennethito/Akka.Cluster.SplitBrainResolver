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
        private readonly string role;

        public KeepMajorityDowningStrategy(string role = null)
        {
            this.role = role;
        }

        public KeepMajorityDowningStrategy(Config config)
            : this(
                role: config.GetString("akka.cluster.split-brain-resolver.keep-majority"))
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
            var members = clusterState.GetMembers(role);
            var unreachableMembers = clusterState.GetUnreachableMembers(role);
            var availableMembers = clusterState.GetAvailableMembers(role);

            int unreachableCount = unreachableMembers.Count;
            int availableCount = availableMembers.Count;

            return availableCount < unreachableCount
                //too few available, down our partition (entire members)
                ? members
                //enough available, down unreachable
                : unreachableMembers;
        }
    }
}
