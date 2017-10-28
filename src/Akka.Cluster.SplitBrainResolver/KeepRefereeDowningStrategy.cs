using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver
{
    /// <summary>
    /// The strategy named keep-referee will down the part that does not contain 
    /// the given referee node.  If the remaining number of nodes are less than the 
    /// configured down-all-if-less-than-nodes all nodes will be downed. If the 
    /// referee node itself is removed all nodes will be downed.  This strategy is 
    /// good if you have one node that hosts some critical resource and the system 
    /// cannot run without it. The drawback is that the referee node is a single point 
    /// of failure, by design. keep-referee will never result in two separate clusters.
    /// </summary>
    public class KeepRefereeDowningStrategy : IDowningStrategy
    {
        private readonly Address address;
        private readonly int downAllIfLessThanNodes;

        public KeepRefereeDowningStrategy(string address, int downAllIfLessThanNodes)
        {
            if(string.IsNullOrWhiteSpace(address))
                throw new ArgumentNullException(nameof(address));

            //akka.tcp://system@hostname:port
            this.address = Address.Parse(address);
            this.downAllIfLessThanNodes = downAllIfLessThanNodes;
        }

        /// <summary>
        /// Creates a KeepRefereeDowningStrategy getting address and downAllIfLessThanNodes from config.
        /// Uses
        ///     akka.cluster.split-brain-resolver.keep-referee.address
        ///     akka.cluster.split-brain-resolver.keep-referee.down-all-if-less-than-nodes
        /// </summary>
        /// <param name="config"></param>
        public KeepRefereeDowningStrategy(Config config)
            : this(
                config.GetString("akka.cluster.split-brain-resolver.keep-referee.address"),
                config.GetInt("akka.cluster.split-brain-resolver.keep-referee.down-all-if-less-than-nodes")
            )
        {
        }

        public IEnumerable<Member> GetVictims(CurrentClusterState clusterState)
        {
            bool haveReferee = clusterState.HasAvailableMember(this.address);

            return !haveReferee || clusterState.GetAvailableMembers().Count < this.downAllIfLessThanNodes
                ? clusterState.GetMembers()
                : clusterState.GetUnreachableMembers();
        }
    }
}