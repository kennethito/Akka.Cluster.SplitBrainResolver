using System.Linq;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Cluster.SplitBrainResolver;
using System.Collections.Generic;
using static Akka.Cluster.ClusterEvent;
using FluentAssertions;
using Xunit;
using Akka.Configuration;

namespace Akka.Cluster.SplitBrainResolver.Tests
{
    public class KeepOldestDowningStrategyTests
    {
        [Fact]
        public void ShouldParseConfig()
        {
            var config = ConfigurationFactory.ParseString(@"
                akka.cluster.split-brain-resolver.keep-oldest.role = ""test""
                akka.cluster.split-brain-resolver.keep-oldest.down-if-alone = on");
            var strategy = new KeepOldestDowningStrategy(config);

            strategy.Role.Should().Be("test");
            strategy.DownIfAlone.Should().BeTrue();
        }

        [Fact]
        public void ShouldDownUnreachablesInOldestPartition()
        {
            var oldest = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(3)
                .ToImmutableSortedSet();

            var newest = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(2)
                .ToImmutableSortedSet();

            var members = oldest.Union(newest);
            var unreachable = newest.ToImmutableHashSet();

            var state = new CurrentClusterState().Copy(members, unreachable);
            var strategy = new KeepOldestDowningStrategy();

            var victims = strategy.GetVictims(state);

            victims.ToImmutableHashSet().SetEquals(unreachable)
                .Should().BeTrue("When we have the oldest node, the unreachable nodes should be downed");
        }

        [Fact]
        public void ShouldDownSelfWhenNotInOldestPartition()
        {
            var oldest = TestUtils.CreateMembers(MemberStatus.Up).First();

            var members = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(4)
                .ToImmutableSortedSet()
                .Add(oldest);

            var unreachable = ImmutableHashSet<Member>.Empty.Add(oldest);

            var state = new CurrentClusterState().Copy(members, unreachable);
            var strategy = new KeepOldestDowningStrategy();

            var victims = strategy.GetVictims(state);

            victims.ToImmutableHashSet().SetEquals(members)
                .Should().BeTrue("When we don't have the oldest node, down ourselves");
        }

        [Fact]
        public void ShouldDownLocalSelfIfOldestAndAlone()
        {
            var oldest = TestUtils.CreateMembers(MemberStatus.Up).First();

            var rest = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(4)
                .ToImmutableSortedSet();

            var members = rest.Add(oldest);
            var unreachable = rest.ToImmutableHashSet();

            var state = new CurrentClusterState().Copy(members, unreachable);
            var strategy = new KeepOldestDowningStrategy(downIfAlone:true);

            var victims = strategy.GetVictims(state);

            victims.ToImmutableHashSet().SetEquals(ImmutableHashSet<Member>.Empty.Add(oldest))
                .Should().BeTrue("When downIfAlone=true, we should down the oldest member if its clustered by itself");
        }

        [Fact]
        public void ShouldDownRemoteOldestWhenOldestAndAlone()
        {
            var oldest = TestUtils.CreateMembers(MemberStatus.Up).First();

            var rest = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(4)
                .ToImmutableSortedSet();

            var members = rest.Add(oldest);
            var unreachable = ImmutableHashSet<Member>.Empty.Add(oldest);

            var state = new CurrentClusterState().Copy(members, unreachable);
            var strategy = new KeepOldestDowningStrategy(downIfAlone: true);

            var victims = strategy.GetVictims(state);

            victims.ToImmutableHashSet().SetEquals(ImmutableHashSet<Member>.Empty.Add(oldest))
                .Should().BeTrue("When downIfAlone=true, we should down the oldest member if its clustered by itself");
        }

        [Fact]
        public void ShouldDownUnreachableWhenNoOldest()
        {
            var oldest = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(3)
                .ToImmutableSortedSet();

            var members = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(5)
                .ToImmutableSortedSet();

            var unreachable = members.Take(2).ToImmutableHashSet();

            var state = new CurrentClusterState().Copy(members, unreachable);

            //There will be no oldest when constrained to this role
            var strategy = new KeepOldestDowningStrategy(role: "SomeRole");

            var victims = strategy.GetVictims(state);

            victims.ToImmutableHashSet().SetEquals(unreachable)
                .Should().BeTrue("When there isn't an oldest node, the unreachable nodes should be downed");
        }
    }
}