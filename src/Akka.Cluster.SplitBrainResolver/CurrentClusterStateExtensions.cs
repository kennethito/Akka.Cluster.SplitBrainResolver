using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using static Akka.Cluster.ClusterEvent;

namespace Akka.Cluster.SplitBrainResolver
{
    internal static class CurrentClusterStateExtensions
    {
        //For most things including availability, we only want to consider up members as available
        private static bool ShouldConsider(Member member, string role = null) => 
            string.IsNullOrWhiteSpace(role) 
                ? member.Status == MemberStatus.Up
                : member.Status == MemberStatus.Up && member.HasRole(role);

        //We need to be able to down everything including joining nodes
        private static bool ShouldConsiderForUnreachable(Member member, string role = null) =>
            string.IsNullOrWhiteSpace(role)
                ? true
                : member.HasRole(role);

        public static bool HasAvailableMember(this CurrentClusterState state, Address address)
        {
            var member = state.Members.FirstOrDefault(m => m.Address.Equals(address));  //cannot use ==
            var unavilable = state.Unreachable.FirstOrDefault(m => m.Address.Equals(address));

            return member != null && unavilable == null
                ? member.Status == MemberStatus.Up
                : false;
        }

        public static ImmutableHashSet<Member> GetAvailableMembers(this CurrentClusterState state, string role = null)
        {
            bool ShouldConsider(Member member) => CurrentClusterStateExtensions.ShouldConsider(member, role);

            return state.Members.Where(ShouldConsider)
                .Except(state.Unreachable.Where(ShouldConsider))
                .ToImmutableHashSet();
        }

        public static ImmutableHashSet<Member> GetUnreachableMembers(this CurrentClusterState state, string role = null)
        {
            bool ShouldConsider(Member member) => CurrentClusterStateExtensions.ShouldConsiderForUnreachable(member, role);

            return state.Unreachable.Where(ShouldConsider)
                .ToImmutableHashSet();
        }

        public static ImmutableHashSet<Member> GetMembers(this CurrentClusterState state, string role = null)
        {
            bool ShouldConsider(Member member) => CurrentClusterStateExtensions.ShouldConsider(member, role);

            return state.Members.Where(ShouldConsider)
                .ToImmutableHashSet();
        }

        public static ImmutableSortedSet<Member> SortByAge(this IEnumerable<Member> members)
        {
            return members.ToImmutableSortedSet(new AgeComparer());
        }

        private class AgeComparer : IComparer<Member>
        {
            public int Compare(Member a, Member b)
            {
                if (a.Equals(b)) return 0;
                if (a.IsOlderThan(b)) return -1;
                return 1;
            }
        }
    }
}