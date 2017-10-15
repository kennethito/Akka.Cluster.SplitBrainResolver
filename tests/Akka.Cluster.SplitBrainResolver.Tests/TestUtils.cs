using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Cluster.SplitBrainResolver.Tests
{
    static class TestUtils
    {
        private static int upNumber = 1;

        static IEnumerable<Address> CreateAddresses()
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());

            while (true)
            {
                yield return new Address(
                    protocol: "tcp",
                    system: "Testing",
                    host: Guid.NewGuid().ToString(),
                    port: rand.Next(10000, 20000));
            }
        }

        public static IEnumerable<Member> CreateMembers(MemberStatus status, ImmutableHashSet<string> roles = null)
        {
            while (true)
            {
                yield return CreateMember(
                    status,
                    roles ?? ImmutableHashSet<string>.Empty);
            }
        }

        public static Member CreateMember(MemberStatus status, ImmutableHashSet<string> roles = null)
        {
            var rand = new Random(Guid.NewGuid().GetHashCode());

            //internal static Member Create(UniqueAddress uniqueAddress, int upNumber, MemberStatus status, ImmutableHashSet<string> roles)
            var methodInfo =
                typeof(Member).GetMethod(
                    name: "Create",
                    bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
                    binder: null,
                    types: new Type[] { typeof(UniqueAddress), typeof(int), typeof(MemberStatus), typeof(ImmutableHashSet<string>) },
                    modifiers: null);

            var address = new UniqueAddress(CreateAddresses().First(), rand.Next(1, int.MaxValue));

            return methodInfo.Invoke(
                obj: null,
                parameters: new object[] 
                {
                    address,
                    upNumber++,
                    status,
                    roles ?? ImmutableHashSet<string>.Empty
                }) as Member;
        }
    }
}
