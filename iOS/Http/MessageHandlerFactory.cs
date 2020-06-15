using System.Net.Http;
using FindAndExplore.Http;

namespace FindAndExplore.iOS.Http
{
    public class MessageHandlerFactory : IMessageHandlerFactory
    {
        public HttpMessageHandler Create()
        {
            return new NSUrlSessionHandler
            {
                DisableCaching = true,
                AllowAutoRedirect = false,
            };
        }
    }
}
