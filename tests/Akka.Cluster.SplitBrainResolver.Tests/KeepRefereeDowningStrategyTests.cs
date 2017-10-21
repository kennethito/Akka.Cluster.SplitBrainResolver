using System.Linq;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.SplitBrainResolver.Tests;
using System.Collections.Generic;
using static Akka.Cluster.ClusterEvent;
using FluentAssertions;
using Xunit;

namespace Akka.Cluster.SplitBrainResolver
{
    public class KeepRefereeDowningStrategyTests
    {
        [Fact]
        public void ShouldDownUnreachableInPartitionsWithReferee()
        {
            string refereeAddress = "akka.tcp://system@localhost:33333";

            var referee = TestUtils.CreateMember(MemberStatus.Up, Address.Parse(refereeAddress));

            var members = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(4)
                .Concat(new List<Member> { referee })
                .ToImmutableSortedSet();

            var unreachable = members.Where(m => m != referee).Take(2).ToImmutableHashSet();

            var state = new CurrentClusterState().Copy(members, unreachable);
            var strategy = new KeepRefereeDowningStrategy(refereeAddress, downAllIfLessThanNodes:0);

            var victims = strategy.GetVictims(state);

            victims.ToImmutableHashSet().SetEquals(unreachable)
                .Should().BeTrue("When we have the referee, the unreachable nodes should be downed");
        }

        [Fact]
        public void ShouldDownMembersInPartitionsWithoutReferee()
        {
            string refereeAddress = "akka.tcp://system@localhost:33333";

            var referee = TestUtils.CreateMember(MemberStatus.Up, Address.Parse(refereeAddress));

            var members = 
                TestUtils.CreateMembers(MemberStatus.Up)
                    .Take(4)
                    .Concat(new List<Member> { referee })
                    .ToImmutableSortedSet();

            var unreachable = 
                members.Where(m => m != referee)
                .Take(2)
                .Concat(new List<Member>{ referee })
                .ToImmutableHashSet();

            var state = new CurrentClusterState().Copy(members, unreachable);
            var strategy = new KeepRefereeDowningStrategy(refereeAddress, downAllIfLessThanNodes:0);

            var victims = strategy.GetVictims(state);

            victims.ToImmutableHashSet().SetEquals(members)
                .Should().BeTrue("When we don't have the referee, we down all members");
        }

        [Fact]
        public void ShouldDownAllMembersWhenNotEnoughNodes()
        {
            string refereeAddress = "akka.tcp://system@localhost:33333";

            var referee = TestUtils.CreateMember(MemberStatus.Up, Address.Parse(refereeAddress));

            var members =
                TestUtils.CreateMembers(MemberStatus.Up)
                    .Take(4)
                    .Concat(new List<Member> { referee })
                    .ToImmutableSortedSet();

            var unreachable =
                members.Where(m => m != referee)
                .Take(2)
                .ToImmutableHashSet();

            var state = new CurrentClusterState().Copy(members, unreachable);
            var strategy = new KeepRefereeDowningStrategy(refereeAddress, downAllIfLessThanNodes: 4);

            var victims = strategy.GetVictims(state);

            victims.ToImmutableHashSet().SetEquals(members)
                .Should().BeTrue("We have 3 available members, which is less than downAllIfLessThanNodes 4, so down all members");
        }

        [Fact]
        public void ShouldNotDownAllMembersWhenEnoughNodes()
        {
            string refereeAddress = "akka.tcp://system@localhost:33333";

            var referee = TestUtils.CreateMember(MemberStatus.Up, Address.Parse(refereeAddress));

            var members =
                TestUtils.CreateMembers(MemberStatus.Up)
                    .Take(4)
                    .Concat(new List<Member> { referee })
                    .ToImmutableSortedSet();

            var unreachable =
                members.Where(m => m != referee)
                .Take(2)
                .ToImmutableHashSet();

            var state = new CurrentClusterState().Copy(members, unreachable);
            var strategy = new KeepRefereeDowningStrategy(refereeAddress, downAllIfLessThanNodes: 3);

            var victims = strategy.GetVictims(state);

            victims.ToImmutableHashSet().SetEquals(unreachable)
                .Should().BeTrue("We have 3 available members, which satisfies downAllIfLessThanNodes 3, so down unreachables");
        }
    }
}