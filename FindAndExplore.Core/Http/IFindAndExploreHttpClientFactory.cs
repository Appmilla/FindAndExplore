using System.Net.Http;

namespace FindAndExplore.Core.Http
{
    public interface IFindAndExploreHttpClientFactory
    {
        HttpClient CreateClient();
        void Reset();
    }
}
