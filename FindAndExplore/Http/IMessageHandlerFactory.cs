using System.Net.Http;

namespace FindAndExplore.Http
{
    public interface IMessageHandlerFactory
    {
        HttpMessageHandler Create();
    }
}
