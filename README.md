# Akka.NET Split Brain Resolver
|Status||
|:--:|:--:| 
[![Build status](https://ci.appveyor.com/api/projects/status/ty8ftchtmfes58eu/branch/master?svg=true)](https://ci.appveyor.com/project/kennethito/akka-cluster-splitbrainresolver/branch/master) |master|
| [![Build status](https://ci.appveyor.com/api/projects/status/ty8ftchtmfes58eu/branch/dev?svg=true)](https://ci.appveyor.com/project/kennethito/akka-cluster-splitbrainresolver/branch/dev) |dev|
| [![Nuget stable version](https://img.shields.io/nuget/v/Akka.Cluster.SplitBrainResolver.svg)](https://www.nuget.org/packages/Akka.Cluster.SplitBrainResolver) |Stable|
| [![Nuget prerelease version](https://img.shields.io/nuget/vpre/Akka.Cluster.SplitBrainResolver.svg)](https://www.nuget.org/packages/Akka.Cluster.SplitBrainResolver) |Prerelease|

This project initially aims to reproduce functionality found in the [JVM Split Brain Resolver](https://doc.akka.io/docs/akka/rp-15v01p05/scala/split-brain-resolver.html).  
The JVM akka doc previously linked should be considered an accurate description for this project as well, including for hocon configuration.

This is a work in progress. 

Currently only the following are implemented.

* [Static Quorum](https://developer.lightbend.com/docs/akka-commercial-addons/current/split-brain-resolver.html#static-quorum)
* [Keep Referee](https://developer.lightbend.com/docs/akka-commercial-addons/current/split-brain-resolver.html#keep-referee)
* [Keep Majority](https://developer.lightbend.com/docs/akka-commercial-addons/current/split-brain-resolver.html#keep-majority)

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

                downing-provider-class = "Akka.Cluster.SplitBrainResolver.SplitBrainDowningProvider, Akka.Cluster.SplitBrainResolver"

                split-brain-resolver {
                    # Enable one of the available strategies (see descriptions below):
                    # static-quorum, keep-majority, keep-oldest, keep-referee 
                    active-strategy = off
                    
                    # Decision is taken by the strategy when there has been no membership or
                    # reachability changes for this duration, i.e. the cluster state is stable.
                    stable-after = 20s
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