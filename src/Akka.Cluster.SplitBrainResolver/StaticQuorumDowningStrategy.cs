using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using System.Linq;
using static Akka.Cluster.ClusterEvent;
using Akka.Configuration;

namespace Akka.Cluster.SplitBrainResolver
{
    /// <summary>
    /// The strategy named static-quorum will down the unreachable nodes if the 
    /// number of remaining nodes are greater than or equal to a configured quorum-size. 
    /// Otherwise it will down the reachable nodes, i.e. it will shut down that side of 
    /// the partition. In other words, the quorum-size defines the minimum number of 
    /// nodes that the cluster must have to be operational.
    /// </summary>
    public sealed class StaticQuorumDowningStrategy : IDowningStrategy
    {
        public int QuorumSize { get; }
        public string Role { get; }

        /// <summary>
        /// Creates a StaticQuorumDowningStrategy
        /// </summary>
        /// <param name="quorumSize">Partitions available nodes less than configured quorum-size will be downed</param>
        /// <param name="role">Only consider nodes in this role. Will be ignored if null</param>
        public StaticQuorumDowningStrategy(int quorumSize, string role = null)
        {
            QuorumSize = quorumSize;
            Role = role;
        }

        /// <summary>
        /// Creates a StaticQuorumDowningStrategy getting quorumSize and role from config.
        /// Uses
        ///     akka.cluster.split-brain-resolver.static-quorum.quorum-size
        ///     akka.cluster.split-brain-resolver.static-quorum.role
        /// </summary>
        /// <param name="config"></param>
        public StaticQuorumDowningStrategy(Config config)
            : this(
                quorumSize: config.GetInt("akka.cluster.split-brain-resolver.static-quorum.quorum-size"), 
                role: config.GetString("akka.cluster.split-brain-resolver.static-quorum.role"))
        {

        }

        public IEnumerable<Member> GetVictims(CurrentClusterState clusterState)
        {
            var members = clusterState.GetMembers(Role);
            var unreachable = clusterState.GetUnreachableMembers(Role);
            int availableCount = members.Count - unreachable.Count;

            return availableCount < QuorumSize
                //too few available, down our partition
                ? clusterState.GetMembers()
                //enough available, down unreachable
                : clusterState.GetUnreachableMembers();
        }
    }
}
