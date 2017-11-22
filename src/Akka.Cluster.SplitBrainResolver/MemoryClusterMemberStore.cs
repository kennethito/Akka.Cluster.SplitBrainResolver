using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using System.Collections.Concurrent;

namespace Akka.Cluster.SplitBrainResolver
{
    public class MemoryClusterMemberStore : IClusterMemberStore
    {
        private readonly object filler = new object();

        private readonly ConcurrentDictionary<Address, object> store 
            = new ConcurrentDictionary<Address, object>();

        public Task<bool> Add(Address address)
        {
            return Task.FromResult(store.TryAdd(address, filler));
        }

        public Task<IEnumerable<Address>> GetMembers()
        {
            return Task.FromResult(store.Keys.AsEnumerable());
        }

        public Task<bool> Remove(Address address)
        {
            return Task.FromResult(store.TryRemove(address, out var unused));
        }
    }
}
