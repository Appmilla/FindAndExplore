using System.Net;
using System.Net.Http;
using FindAndExplore.Http;
using Xamarin.Android.Net;

namespace FindAndExplore.Droid.Http
{
    public class MessageHandlerFactory : IMessageHandlerFactory
    {
        public HttpMessageHandler Create()
        {
            var handler = new AndroidAppmillaClientHandler
            {
                InnerHandler = new AndroidClientHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                }
            };

            return handler;
        }
    }
}
