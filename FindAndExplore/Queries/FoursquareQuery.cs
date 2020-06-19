using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using FindAndExplore.Http;
using FindAndExplore.Reactive;
using FoursquareApi.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.Queries
{   
    public interface IFoursquareQuery
    {
        bool IsBusy { get; }
        
        IObservable<ICollection<Venue>> GetVenues(double lat, double lon, int radius, string cacheKey);

        IObservable<ICollection<Venue>> RefreshVenues(double lat, double lon, int radius, string cacheKey);
    }

    public class FoursquareQuery : ReactiveObject, IFoursquareQuery
    {
        readonly IBlobCache _blobCache;
        readonly IFindAndExploreHttpClientFactory _httpClientFactory;
        readonly FoursquareApiClient _foursquareApiClient;
        readonly ISchedulerProvider _schedulerProvider;

        readonly TimeSpan _cacheLifetime = TimeSpan.FromHours(1);

        [Reactive]
        public bool IsBusy { get; set; }

        public FoursquareQuery(IBlobCache blobCache,
            IFindAndExploreHttpClientFactory httpClientFactory,
            FoursquareApiClient foursquareApiClient,
            ISchedulerProvider schedulerProvider)
        {
            _blobCache = blobCache;
            _httpClientFactory = httpClientFactory;
            _foursquareApiClient = foursquareApiClient;
            _schedulerProvider = schedulerProvider;
        }

        public async Task<HealthCheckResult> HealthCheck()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var healthCheck = await _foursquareApiClient.HealthCheck();

            return healthCheck;
        }

        public IObservable<ICollection<Venue>> GetVenues(double lat, double lon, int radius, string cacheKey)
        {
            DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;

            return _blobCache.GetOrFetchObject(cacheKey,
                    () => FetchVenues(lat, lon, radius),
                    expiration);
        }

        public IObservable<ICollection<Venue>> RefreshVenues(double lat, double lon, int radius, string cacheKey)
        {
            return Observable.Create<ICollection<Venue>>(async observer =>
            {
                DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;

                var venues = await FetchVenues(lat, lon, radius).ConfigureAwait(false);

                await _blobCache.InsertObject(cacheKey, venues, expiration);

                observer.OnNext(venues);

            }).SubscribeOn(_schedulerProvider.ThreadPool);
        }

        async Task<ICollection<Venue>> FetchVenues(double lat, double lon, int radius)
        {
            IsBusy = true;

            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                var points = await _foursquareApiClient.GetVenuesAsync(lat, lon, radius);

                return points;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
