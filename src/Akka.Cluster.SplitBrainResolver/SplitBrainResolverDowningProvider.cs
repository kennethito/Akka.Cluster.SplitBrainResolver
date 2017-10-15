using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.Cluster.SplitBrainResolver
{
    public class SplitBrainResolverDowningProvider : StrategizedDowningProvider
    {
        public SplitBrainResolverDowningProvider(ActorSystem system)
            : base(system)
        {

        }

        protected override IDowningStrategy GetDowningStrategy()
        {
            var config = System.Settings.Config;

            var requestedStrategy = 
                config.GetString("akka.cluster.split-brain-resolver.active-strategy");

            IDowningStrategy strategy = null;

            switch(requestedStrategy)
            {
                case "static-quorum":
                    strategy = new StaticQuorumDowningStrategy(config);
                    break;
                case "off":
                    strategy = new NoopDowningStrategy();
                    break;
                default:
                    throw new NotSupportedException($"Currently only 'static-quorum' and 'off' are supported");
            }

            return strategy;
        }
    }
}
