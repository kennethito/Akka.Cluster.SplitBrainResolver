using Akka.Cluster.SplitBrainResolver;
using System;
using System.Collections.Generic;
using System.Text;
using Akka.Cluster;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;

namespace CustomDowningProvider
{
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
}
