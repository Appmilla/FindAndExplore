namespace FindAndExplore.Configuration
{
    public class AppConfiguration : IAppConfiguration    
    {
        //public string FindAndExploreBaseUrl => "https://apim-find-and-explore.azure-api.net/FindAndExploreApi-dev/";
        //public string FindAndExploreBaseUrl => "https://apim-find-and-explore.azure-api.net/FindAndExploreApi-dev/v1/";
        public string FindAndExploreBaseUrl => "https://apim-find-and-explore.azure-api.net/";
        public string FindAndExploreSubscriptionKey => "12e7dd82e8e94a67a4cc70663f9cf46d";

        public AppConfiguration()
        {
        }
    }
}
