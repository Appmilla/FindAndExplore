using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using FindAndExplore.Http;
using FindAndExplore.Reactive;
using FindAndExploreApi.Client;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.Queries
{
    public interface IFindAndExploreQuery
    {
        bool IsBusy { get; }

        Task<ICollection<SupportedArea>> GetCurrentArea(double lat, double lon, string cacheKey);

        IObservable<ICollection<PointOfInterest>> GetPointsOfInterest(int locationId, string cacheKey);

        IObservable<ICollection<PointOfInterest>> RefreshPointsOfInterest(int locationId, string cacheKey);
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

        public async Task<ICollection<SupportedArea>> GetCurrentArea(double lat, double lon, string cacheKey)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var areaResponse = await _findAndExploreApiClient.GetCurrentAreaAsync(lat, lon);

            return areaResponse.Result;
        }

        public IObservable<ICollection<PointOfInterest>> GetPointsOfInterest(int locationId, string cacheKey)
        {
            DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;

            return _blobCache.GetOrFetchObject(cacheKey,
                    () => FetchPointsOfInterest(locationId),
                    expiration);
        }
        
        public IObservable<ICollection<PointOfInterest>> RefreshPointsOfInterest(int locationId, string cacheKey)
        {
            return Observable.Create<ICollection<PointOfInterest>>(async observer =>
            {
                DateTimeOffset? expiration = DateTimeOffset.Now + _cacheLifetime;

                var points = await FetchPointsOfInterest(locationId).ConfigureAwait(false);

                await _blobCache.InsertObject(cacheKey, points, expiration);

                observer.OnNext(points);

            }).SubscribeOn(_schedulerProvider.ThreadPool);
        }

        async Task<ICollection<PointOfInterest>> FetchPointsOfInterest(int locationId)
        {
            IsBusy = true;

            try
            {                
                var httpClient = _httpClientFactory.CreateClient();
                
                var points = await _findAndExploreApiClient.GetPointsOfInterestAsync(locationId);

                return points.Result;
            }
            finally
            {
                IsBusy = false;
            }
        }        
    }
}
