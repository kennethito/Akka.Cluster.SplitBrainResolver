using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;

namespace Akka.Cluster.SplitBrainResolver
{
    public interface IClusterMemberStore
    {
        Task<IEnumerable<Address>> GetMembers();
        Task<bool> Add(Address address);
        Task<bool> Remove(Address address);
    }
}