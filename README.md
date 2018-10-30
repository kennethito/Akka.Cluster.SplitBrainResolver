# Akka.NET Split Brain Resolver
|Status||
|:--:|:--:| 
[![Build status](https://ci.appveyor.com/api/projects/status/ty8ftchtmfes58eu/branch/master?svg=true)](https://ci.appveyor.com/project/kennethito/akka-cluster-splitbrainresolver/branch/master) |master|
| [![Build status](https://ci.appveyor.com/api/projects/status/ty8ftchtmfes58eu/branch/dev?svg=true)](https://ci.appveyor.com/project/kennethito/akka-cluster-splitbrainresolver/branch/dev) |dev|
| [![Nuget stable version](https://img.shields.io/nuget/v/Akka.Cluster.SplitBrainResolver.svg)](https://www.nuget.org/packages/Akka.Cluster.SplitBrainResolver) |Stable|
| [![Nuget prerelease version](https://img.shields.io/nuget/vpre/Akka.Cluster.SplitBrainResolver.svg)](https://www.nuget.org/packages/Akka.Cluster.SplitBrainResolver) |Prerelease|

Currently available downing strategies

* [Static Quorum](https://developer.lightbend.com/docs/akka-commercial-addons/current/split-brain-resolver.html#static-quorum)
* [Keep Referee](https://developer.lightbend.com/docs/akka-commercial-addons/current/split-brain-resolver.html#keep-referee)
* [Keep Majority](https://developer.lightbend.com/docs/akka-commercial-addons/current/split-brain-resolver.html#keep-majority)
* [Keep Oldest](https://developer.lightbend.com/docs/akka-commercial-addons/current/split-brain-resolver.html#keep-oldest)

You can also create your own downing providers.
* [Custom Downing Providers Documentation](https://github.com/kennethito/Akka.Cluster.SplitBrainResolver/blob/dev/docs/custom-downing-providers.md)

## Configuration

Configure the downing provider class and split-brain-resolver section similar to below

        akka {
            actor.provider = "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster"
            remote {
                dot-netty.tcp {
                    hostname = localhost
                    port = 8001
                }
            }
            cluster {
                auto-down-unreachable-after = off

                downing-provider-class = "Akka.Cluster.SplitBrainResolver.SplitBrainResolverDowningProvider, Akka.Cluster.SplitBrainResolver"

                split-brain-resolver {
                    # Enable one of the available strategies (see descriptions below):
                    # static-quorum, keep-majority, keep-oldest, keep-referee 
                    active-strategy = off
                    
                    # Decision is taken by the strategy when there has been no membership or
                    # reachability changes for this duration, i.e. the cluster state is stable.
                    stable-after = 20s

                    # The strategy named static-quorum will down the unreachable nodes if the number 
                    # of remaining nodes are greater than or equal to a configured quorum-size. 
                    # Otherwise, it will down the reachable nodes
                    static-quorum {
                        # if the 'role' is defined the decision is based only on members with that 'role'
                        role = ""
                        # Minimum number of nodes that the cluster must have (not the total size)
                        # Note that you must not add more members to the cluster than quorum-size * 2 - 1, 
                        # because then both sides may down each other and thereby form two separate clusters
                        quorum-size = 3
                    }

                    # The strategy named keep-majority will down the unreachable nodes if the current node is 
                    # in the majority part based on the last known membership information. Otherwise down the 
                    # reachable nodes, i.e. the own part. If the parts are of equal size the part containing 
                    # the node with the lowest address is kept.
                    keep-majority {
                        # if the 'role' is defined the decision is based only on members with that 'role'
                        role = ""
                    }

                    # The strategy named keep-oldest will down the part that does not contain the oldest member. 
                    # The oldest member is interesting because the active Cluster Singleton instance is 
                    # running on the oldest member.
                    keep-oldest {
                        # if the 'role' is defined the decision is based only on members with that 'role'
                        role = ""
                        # Enable downing of the oldest node when it is partitioned from all other nodes
                        down-if-alone = off
                    }

                    # The strategy named keep-referee will down the part that does not contain the given referee node.
                    keep-referee {
                        # referee address on the form of "akka.tcp://system@hostname:port"
                        address = ""
                        # If the remaining number of nodes are less than the configured down-all-if-less-than-nodes 
                        # all nodes will be downed. If the referee node itself is removed all nodes will be downed.
                        down-all-if-less-than-nodes = 1
                    }
                }

                seed-nodes = ["akka.tcp://test-system@localhost:8001"]
            }
        }

## Building

Pre-requsites

1. [Nuget](https://docs.microsoft.com/en-us/nuget/guides/install-nuget) on your path
2. [Dotnet core sdk 2.0+](https://www.microsoft.com/net/core#windowscmd)
3. Visual studio 2017.3+ (Potentially optional, but untested)

Building via dotnet has several alternatives

* dotnet build from the repository root
* build.ps1 from the repository root (used via CI) 