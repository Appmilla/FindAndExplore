using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Java.Net;

namespace FindAndExplore.Droid.Http
{
    public class AndroidAppmillaClientHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (UnknownHostException)
            {
                // this is the same message text thrown by iOS so we can filter this out of app center errors
                throw new HttpRequestException("The Internet connection appears to be offline.");
            }
        }
    }
}
