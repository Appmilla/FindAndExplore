using System;

namespace FindAndExplore.Core.Configuration
{
    public class AppConfiguration : IAppConfiguration    
    {
        /*
        public string FindAndExploreBaseUrl => ConfigCollection.GetConfigValue("FindAndExploreBaseUrl");
        public string FindAndExploreSubscriptionKey => ConfigCollection.GetConfigValue("FindAndExploreSubscriptionKey");
        */

        public string FindAndExploreBaseUrl => "https://apim-find-and-explore.azure-api.net/FindAndExploreApi-dev/";
        public string FindAndExploreSubscriptionKey => "12e7dd82e8e94a67a4cc70663f9cf46d";

        public AppConfiguration()
        {
        }
    }
}
