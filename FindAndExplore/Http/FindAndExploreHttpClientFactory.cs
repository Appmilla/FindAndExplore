using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FindAndExplore.Configuration;

namespace FindAndExplore.Http
{
    public class FindAndExploreHttpClientFactory : IFindAndExploreHttpClientFactory
    {
        private readonly IMessageHandlerFactory _messageHandlerFactory;

        readonly IAppConfiguration _appConfiguration;
        
        HttpClient _httpClient;

        static readonly object SyncObject = new object();

        public FindAndExploreHttpClientFactory(IMessageHandlerFactory messageHandlerFactory,
            IAppConfiguration appConfiguration)
        {
            _messageHandlerFactory = messageHandlerFactory;
            _appConfiguration = appConfiguration;            
        }

        public HttpClient CreateClient()
        {
            lock (SyncObject)
            {
                if (_httpClient != null)
                    return _httpClient;

                var innerHandler = _messageHandlerFactory.Create();
                var handler = new ApiHttpHandler(innerHandler, _appConfiguration);

                _httpClient = new HttpClient(handler) { BaseAddress = new Uri(_appConfiguration.FindAndExploreBaseUrl) };

                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                return _httpClient;
            }
        }

        public void Reset()
        {
            lock (SyncObject)
            {
                if (_httpClient != null)
                {
                    _httpClient.Dispose();
                    _httpClient = null;
                }
            }
        }

        class ApiHttpHandler : DelegatingHandler
        {
            readonly IAppConfiguration _appConfiguration;
            
            public ApiHttpHandler(HttpMessageHandler innerHandler,
                IAppConfiguration appConfiguration)
                : base(innerHandler)
            {
                _appConfiguration = appConfiguration;                
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {               
                request.Headers.Add("Api-Version", "v1");

                if (!string.IsNullOrEmpty(_appConfiguration.FindAndExploreSubscriptionKey))
                {
                    request.Headers.Add("Ocp-Apim-Subscription-Key", _appConfiguration.FindAndExploreSubscriptionKey);
                    request.Headers.Add("Ocp-Apim-Trace", "true");
                }

                /*
                IEnumerable<string> headerValues = request.Headers.GetValues("Request-Id");
                var requestId = headerValues.FirstOrDefault();
                
                var requestLog = Log.ForContext<ApiHttpHandler>().ForContext("Request-Id", requestId);
                requestLog.Debug("Request {Url} {AccessToken}", request.RequestUri, accessToken);
                */

                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var contentString = await response.Content.ReadAsStringAsync();
                    if (contentString.ToLower().Contains("unsupported version".ToLower()))
                    {
                        throw new UnsupportedApiException();
                    }
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedApiException();
                }

                return response;
            }
        }
    }
}
