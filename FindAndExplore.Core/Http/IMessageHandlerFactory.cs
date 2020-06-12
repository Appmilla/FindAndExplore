using System.Net.Http;

namespace FindAndExplore.Core.Http
{
    public interface IMessageHandlerFactory
    {
        HttpMessageHandler Create();
    }
}
