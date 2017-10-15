using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver
{
    public interface IDowningStrategy
    {
        /// <summary>
        /// Gets victims to be downed according to the implementing strategy
        /// </summary>
        /// <returns>Addresses to be downed</returns>
        IEnumerable<Member> GetVictims(CurrentClusterState clusterState);
    }
}
