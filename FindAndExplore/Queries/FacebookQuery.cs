using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using FacebookApi.Client;
using FindAndExplore.Http;
using FindAndExplore.Reactive;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.Queries
{
    public interface IFacebookQuery
    {
        bool IsBusy { get; }

        IObservable<ICollection<Place>> GetPlaces(double lat, double lon, int radius, string cacheKey);

        IObservable<ICollection<Place>> RefreshPlaces(double lat, double lon, int radius, string cacheKey);
    }

    public class FacebookQuery : ReactiveObject, IFacebookQuery
    {
        readonly IBlobCache _blobCache;
        readonly IFindAndExploreHttpClientFactory _httpClientFactory;
        readonly FacebookApiClient _facebookApiClient;
        readonly ISchedulerProvider _schedulerProvider;

        readonly TimeSpan _cacheLifetime = TimeSpan.FromHours(1);

        [Reactive]
        public bool IsBusy { get; set; }

        public FacebookQuery(IBlobCache blobCache,
            IFindAndExploreHttpClientFactory httpClientFactory,
            FacebookApiClient facebookApiClient,
            ISchedulerProvider schedulerProvider)
        {
            _blobCache = blobCache;
            _httpClientFactory = httpClientFactory;
            _facebookApiClient = facebookApiClient;
            _schedulerProvider = schedulerProvider;
        }

        public async Task<HealthCheckResult> HealthCheck()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var healthCheck = await _facebookApiClient.HealthCheck();

            return healthCheck;
        }

        public IObservable<ICollection<Place>> GetPlaces(double lat, double lon, int radius, string cacheKey)
        {
            DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;

            return _blobCache.GetOrFetchObject(cacheKey,
                    () => FetchPlaces(lat, lon, radius),
                    expiration);
        }

        public IObservable<ICollection<Place>> RefreshPlaces(double lat, double lon, int radius, string cacheKey)
        {
            return Observable.Create<ICollection<Place>>(async observer =>
            {
                DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;

                var places = await FetchPlaces(lat, lon, radius).ConfigureAwait(false);

                await _blobCache.InsertObject(cacheKey, places, expiration);

                observer.OnNext(places);

            }).SubscribeOn(_schedulerProvider.ThreadPool);
        }

        async Task<ICollection<Place>> FetchPlaces(double lat, double lon, int radius)
        {
            IsBusy = true;

            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                var points = await _facebookApiClient.GetPlacesAsync(lat, lon, radius);

                return points;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
