using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using FindAndExplore.Http;
using FindAndExplore.Reactive;
using FindAndExploreApi.Client;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.Queries
{
    public interface IFindAndExploreQuery
    {
        bool IsBusy { get; }

        Task<HealthCheckResult> HealthCheck();

        IObservable<ICollection<PointOfInterest>> GetPointsOfInterest(Position position, string cacheKey);

        IObservable<ICollection<PointOfInterest>> RefreshPointsOfInterest(Position position, string cacheKey);
    }

    public class FindAndExploreQuery : ReactiveObject, IFindAndExploreQuery
    {
        readonly IBlobCache _blobCache;
        readonly IFindAndExploreHttpClientFactory _httpClientFactory;
        readonly FindAndExploreApiClient _findAndExploreApiClient;
        readonly ISchedulerProvider _schedulerProvider;

        readonly TimeSpan _cacheLifetime = TimeSpan.FromHours(1);

        [Reactive]
        public bool IsBusy { get; set; }

        SupportedArea CurrentArea { get; set; }
        
        public FindAndExploreQuery(IBlobCache blobCache,
            IFindAndExploreHttpClientFactory httpClientFactory,
            FindAndExploreApiClient findAndExploreApiClient,
            ISchedulerProvider schedulerProvider)
        {
            _blobCache = blobCache;
            _httpClientFactory = httpClientFactory;
            _findAndExploreApiClient = findAndExploreApiClient;
            _schedulerProvider = schedulerProvider;
        }

        public async Task<HealthCheckResult> HealthCheck()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var healthCheck = await _findAndExploreApiClient.HealthCheck();

            return healthCheck;
        }

        async Task<ICollection<SupportedArea>> GetCurrentArea(double lat, double lon)
        {
            var areaResponse = await _findAndExploreApiClient.GetCurrentAreaAsync(lat, lon);

            return areaResponse;
        }

        public IObservable<ICollection<PointOfInterest>> GetPointsOfInterest(Position position, string cacheKey)
        {
            DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;

            return _blobCache.GetOrFetchObject(cacheKey,
                    () => FetchPointsOfInterest(position),
                    expiration);
        }
        
        public IObservable<ICollection<PointOfInterest>> RefreshPointsOfInterest(Position position, string cacheKey)
        {
            return Observable.Create<ICollection<PointOfInterest>>(async observer =>
            {
                DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;

                var points = await FetchPointsOfInterest(position).ConfigureAwait(false);

                await _blobCache.InsertObject(cacheKey, points, expiration);

                observer.OnNext(points);

            }).SubscribeOn(_schedulerProvider.ThreadPool);
        }

        async Task<ICollection<PointOfInterest>> FetchPointsOfInterest(Position position)
        {
            IsBusy = true;

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                
                //TODO maybe cache current area as we did previously
                var points = new List<PointOfInterest>();
                var supportedAreas = await GetCurrentArea(position.Latitude, position.Longitude);

                if (supportedAreas.Any())
                {
                    foreach (var area in supportedAreas)
                    {
                        var areaPoints = await _findAndExploreApiClient.GetPointsOfInterestAsync(area.LocationId);
                        points.AddRange(areaPoints);
                    }
                }
                return points;
            }
            finally
            {
                IsBusy = false;
            }
        }        
    }
}
