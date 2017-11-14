using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Akka.Cluster.ClusterEvent;
using Akka.Configuration;

namespace Akka.Cluster.SplitBrainResolver.Tests
{
    public class KeepMajorityDowningStrategyTests
    {
        [Fact]
        public void ShouldParseConfig()
        {
            var config = ConfigurationFactory.ParseString("akka.cluster.split-brain-resolver.keep-majority.role = \"test\"");
            var strategy = new KeepMajorityDowningStrategy(config);

            strategy.Role.Should().Be("test");
        }

        [Fact]
        public void ShouldDownMinorityPartitions()
        {
            var strategy = new KeepMajorityDowningStrategy();

            var members = TestUtils.CreateMembers(MemberStatus.Up).Take(5).ToImmutableSortedSet();
            var unreachableMembers = members.Take(3).ToImmutableHashSet();

            var clusterState =
                new CurrentClusterState()
                    .Copy(members: members, unreachable: unreachableMembers);

            var victims = strategy.GetVictims(clusterState).ToList();

            victims.ToImmutableHashSet().SetEquals(members)
                .Should().BeTrue("Minority partitions should be marked for downing");
        }

        [Fact]
        public void ShouldDownUnreachableMembersInMajorityPartitions()
        {
            var strategy = new KeepMajorityDowningStrategy();

            var members = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(5)
                .ToImmutableSortedSet();
            var unreachableMembers = members.Take(1).ToImmutableHashSet();

            var clusterState =
                new CurrentClusterState()
                    .Copy(members: members, unreachable: unreachableMembers);

            var victims = strategy.GetVictims(clusterState).ToList();

            victims.ToImmutableHashSet().SetEquals(unreachableMembers)
                .Should().BeTrue("Unreachable members in majority should be marked for downing");
        }

        [Fact]
        public void ShouldDownMinorityPartitionsConsideringRole()
        {
            string roleName = "SomeRole";
            var roles = new string[] { roleName }.ToImmutableHashSet();

            var membersInRole = TestUtils.CreateMembers(MemberStatus.Up, roles).Take(3).ToList();
            var unreachableMembersInRole = membersInRole.Take(2).ToImmutableHashSet();

            var members = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(4)
                .Concat(membersInRole)
                .ToImmutableSortedSet();

            var clusterState =
                new CurrentClusterState().Copy(members: members, unreachable: unreachableMembersInRole);

            var strategy = new KeepMajorityDowningStrategy(roleName);

            var victims = strategy.GetVictims(clusterState).ToList();

            victims.ToImmutableHashSet().SetEquals(membersInRole)
                .Should().BeTrue("Minority partitions should be marked for downing");

            victims.All(v => v.HasRole(roleName))
                .Should().BeTrue("We should only down members in the specified role");
        }

        [Fact]
        public void ShouldDownUnreachableMembersInMajorityPartitionsConsideringRole()
        {
            string roleName = "SomeRole";
            var roles = new string[] { roleName }.ToImmutableHashSet();

            var membersInRole = TestUtils.CreateMembers(MemberStatus.Up, roles).Take(3).ToList();
            var unreachableMembersInRole = membersInRole.Take(1).ToImmutableHashSet();

            var members = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(4)
                .Concat(membersInRole)
                .ToImmutableSortedSet();

            var clusterState =
                new CurrentClusterState().Copy(members: members, unreachable: unreachableMembersInRole);

            var strategy = new KeepMajorityDowningStrategy(roleName);

            var victims = strategy.GetVictims(clusterState).ToList();

            victims.ToImmutableHashSet().SetEquals(unreachableMembersInRole)
                .Should().BeTrue("Unreachable members in majority should be marked for downing");

            victims.All(v => v.HasRole(roleName))
                .Should().BeTrue("We should only down members in the specified role");
        }

        [Fact]
        public void ShouldDownPartitionWithoutOldestWhenEqualMembers()
        {
            var oldest = TestUtils.CreateMembers(MemberStatus.Up).First();
            
            var members = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(5)
                .ToImmutableSortedSet()
                .Add(oldest);

            //Oldest is in unreachableMembers
            var unreachableMembers = members
                .Where(m => !m.Equals(oldest))
                .Take(2)
                .ToImmutableHashSet()
                .Add(oldest);

            var clusterState =
                new CurrentClusterState().Copy(members: members, unreachable: unreachableMembers);

            var strategy = new KeepMajorityDowningStrategy();

            var victims = strategy.GetVictims(clusterState).ToList();

            victims.ToImmutableSortedSet().SetEquals(members)
                .Should().BeTrue("Partition without oldest should be marked for downing");
        }

        [Fact]
        public void ShouldNotDownPartitionWithOldestWhenEqualMembers()
        {
            var oldest = TestUtils.CreateMembers(MemberStatus.Up).First();

            //Oldest is in the reachable members
            var members = TestUtils.CreateMembers(MemberStatus.Up)
                .Take(5)
                .ToImmutableSortedSet()
                .Add(oldest);

            var unreachableMembers = members
                .Where(m => !m.Equals(oldest))
                .Take(3)
                .ToImmutableHashSet();

            var clusterState =
                new CurrentClusterState().Copy(members: members, unreachable: unreachableMembers);

            var strategy = new KeepMajorityDowningStrategy();

            var victims = strategy.GetVictims(clusterState).ToList();

            victims.ToImmutableSortedSet().SetEquals(unreachableMembers)
                .Should().BeTrue("Partition with oldest should not be marked for downing");
        }
    }
}
