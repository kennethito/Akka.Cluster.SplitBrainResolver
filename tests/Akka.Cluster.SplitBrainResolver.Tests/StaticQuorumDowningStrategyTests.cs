using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Xunit;
using FluentAssertions;
using System.Reflection;
using static Akka.Cluster.ClusterEvent;
using Akka.Configuration;

namespace Akka.Cluster.SplitBrainResolver.Tests
{
    public class StaticQuorumDowningStrategyTests
    {
        [Fact]
        public void ShouldParseConfig()
        {
            var config = ConfigurationFactory.ParseString(@"
                akka.cluster.split-brain-resolver.static-quorum.role = ""test""
                akka.cluster.split-brain-resolver.static-quorum.quorum-size = 3");
            var strategy = new StaticQuorumDowningStrategy(config);

            strategy.Role.Should().Be("test");
            strategy.QuorumSize.Should().Be(3);
        }

        [Fact]
        public void ShouldDownPartitionsWithTooFewMembers()
        {
            var strategy = new StaticQuorumDowningStrategy(quorumSize: 3);

            var members = TestUtils.CreateMembers(MemberStatus.Up).Take(2).ToImmutableSortedSet();

            var clusterState = 
                new CurrentClusterState()
                .Copy(members: members);

            var victims = strategy.GetVictims(clusterState);

            victims.ToImmutableHashSet().SetEquals(members)
                .Should().BeTrue("Partitions with fewer than the quorum size should be marked for downing");
        }

        [Fact]
        public void ShouldDownPartitionsWithTooFewMembersConsideringRole()
        {
            string roleName = "SomeRole";
            var roles = new string[] { roleName }.ToImmutableHashSet();

            var membersInRole = TestUtils.CreateMembers(MemberStatus.Up, roles).Take(2).ToList();
            var members =
                TestUtils.CreateMembers(MemberStatus.Up)
                    .Take(3)
                    .Concat(membersInRole)
                    .ToImmutableSortedSet();

            var clusterState = new CurrentClusterState().Copy(members);

            var strategy = new StaticQuorumDowningStrategy(quorumSize: 3, role: roleName);
            var victims = strategy.GetVictims(clusterState);

            victims.ToImmutableHashSet().SetEquals(members)
                .Should().BeTrue("Partitions with fewer than the quorum size should be marked for downing");
        }

        [Fact]
        public void ShouldDownPartitionsWithTooFewAvailableMembers()
        {
            var members = TestUtils.CreateMembers(MemberStatus.Up).Take(3).ToImmutableSortedSet();
            var unreachableMembers = members.Take(1).ToImmutableHashSet();

            var clusterState =
                new CurrentClusterState()
                    .Copy(members, unreachableMembers);

            var strategy = new StaticQuorumDowningStrategy(quorumSize: 3);
            var victims = strategy.GetVictims(clusterState);

            victims.ToImmutableHashSet().SetEquals(members)
                .Should().BeTrue("Partitions with fewer available members than the quorum size should be marked for downing");
        }

        [Fact]
        public void ShouldNotDownPartitionsWithEnoughMembers()
        {
            var members = TestUtils.CreateMembers(MemberStatus.Up).Take(3).ToImmutableSortedSet();

            var clusterState = 
                new CurrentClusterState()
                    .Copy(members);

            var strategy = new StaticQuorumDowningStrategy(quorumSize: 3);
            var victims = strategy.GetVictims(clusterState);

            victims.Count().Should().Be(0, "Partitions with quorum size or greater should not be marked for downing");
        }

        [Fact]
        public void ShouldNotDownPartitionsWithEnoughMembersInRole()
        {
            string roleName = "SomeRole";
            var roles = new string[] { roleName }.ToImmutableHashSet();

            var membersInRole = TestUtils.CreateMembers(MemberStatus.Up, roles).Take(3).ToList();
            var members =
                TestUtils.CreateMembers(MemberStatus.Up)
                    .Take(2)
                    .Concat(membersInRole)
                    .ToImmutableSortedSet();

            var clusterState = new CurrentClusterState().Copy(members);

            var strategy = new StaticQuorumDowningStrategy(quorumSize: 3, role: roleName);
            var victims = strategy.GetVictims(clusterState);

            victims.Count().Should().Be(0, "Partitions with fewer than the quorum size should be marked for downing");
        }

        [Fact]
        public void ShouldDownUnreachablesWhenEnoughAvailableMembers()
        {
            var members = TestUtils.CreateMembers(MemberStatus.Up).Take(5).ToImmutableSortedSet();
            var unreachableMembers = members.Take(2).ToImmutableHashSet();

            var clusterState =
                new CurrentClusterState()
                    .Copy(members: members, unreachable: unreachableMembers);

            var strategy = new StaticQuorumDowningStrategy(quorumSize: 3);
            var victims = strategy.GetVictims(clusterState).ToList();

            victims.ToImmutableHashSet().SetEquals(unreachableMembers)
                .Should().BeTrue("Partitions with available members >= quorum size should down unreachable members");
        }
    }
}
