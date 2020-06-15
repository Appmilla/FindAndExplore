using System.Net.Http;

namespace FindAndExplore.Http
{
    public interface IFindAndExploreHttpClientFactory
    {
        HttpClient CreateClient();
        void Reset();
    }
}
