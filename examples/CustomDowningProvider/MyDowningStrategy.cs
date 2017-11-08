using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.SplitBrainResolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomDowningProvider
{
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
            var config = this.system.Settings.Config.GetConfig($"akka.cluster.{MyDowningProvider.RootConfigElement}");
            var exampleSetting = config.GetInt("example-config-entry");

            this.system.Log.Info($"Example logging of example setting: {exampleSetting}");

            //Decide what do based off the the passed in current cluster state.
            //Whatever is returned, will be downed
            return Enumerable.Empty<Member>();
        }
    }
}
