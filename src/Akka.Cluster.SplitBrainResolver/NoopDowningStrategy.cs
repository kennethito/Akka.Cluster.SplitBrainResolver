using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver
{
    public sealed class NoopDowningStrategy : IDowningStrategy
    {
        public IEnumerable<Member> GetVictims(CurrentClusterState clusterState)
        {
            return Enumerable.Empty<Member>();
        }
    }
}
