# Custom Downing Providers

You can easily create your own downing providers by extending the provided
[StrategizedDowningProvider](https://github.com/kennethito/Akka.Cluster.SplitBrainResolver/blob/dev/src/Akka.Cluster.SplitBrainResolver/StrategizedDowningProvider.cs)
and implementing a corresponding [IDowningStrategy](https://github.com/kennethito/Akka.Cluster.SplitBrainResolver/blob/dev/src/Akka.Cluster.SplitBrainResolver/IDowningStrategy.cs)
for it.

The example below can be found [here](https://github.com/kennethito/Akka.Cluster.SplitBrainResolver/blob/dev/examples/CustomDowningProvider)

```C#
public class MyDowningStrategy : IDowningStrategy
{
    private readonly ActorSystem system;

    public MyDowningStrategy(ActorSystem system)
    {
        this.system = system;
    }

    public IEnumerable<Member> GetVictims(ClusterEvent.CurrentClusterState clusterState)
    {
        //Example configuration access
        var config = this.system.Settings.Config.GetConfig(
            $"akka.cluster.{MyDowningProvider.RootConfigElement}");

        var exampleSetting = config.GetInt("example-config-entry");

        this.system.Log.Info($"Example logging of example setting: {exampleSetting}");

        //Decide what do based off the the passed in current cluster state.
        //Whatever is returned, will be downed
        return Enumerable.Empty<Member>();
    }
}
```

```C#
public class MyDowningProvider : StrategizedDowningProvider
{
    //The parent StrategizedDowningProvider will look for configuration here
    internal const string RootConfigElement = "my-downing-provider";

    private readonly ActorSystem system;

    public MyDowningProvider(ActorSystem system) 
        : base(system, RootConfigElement)
    {
        this.system = system;
    }

    protected override IDowningStrategy GetDowningStrategy()
    {
        return new MyDowningStrategy(this.system);
    }
}
```

```
akka {
    actor.provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
    remote {
        dot-netty.tcp {
            hostname = localhost
            port = 8001
        }
    }
    cluster {
        downing-provider-class = 
            "CustomDowningProvider.MyDowningProvider, CustomDowningProvider"

        my-downing-provider {
            stable-after = 3s
            example-config-entry = 10
        }

        auto-down-unreachable-after = off
        seed-nodes = ["akka.tcp://test-system@localhost:8001"]
    }
}
```
