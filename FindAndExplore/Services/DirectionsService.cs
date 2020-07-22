using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FindAndExplore.Http;
using GeoJSON.Net.Geometry;
using MapboxApi.Client;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.Services
{
    public class DirectionsService : IDirectionsService
    {
        [Reactive]
        public bool IsBusy { get; set; }

        readonly MapboxApiClient _mapboxApiClient;
        readonly IFindAndExploreHttpClientFactory _httpClientFactory;

        public DirectionsService(IFindAndExploreHttpClientFactory httpClientFactory,
                            MapboxApiClient mapboxApiClient)
        {
            _httpClientFactory = httpClientFactory;
            _mapboxApiClient = mapboxApiClient;
        }

        public async Task<ICollection<Position>> GetDirectionsAsync(DirectionsType routeType, Position current, Position destination)
        {
            IsBusy = true;

            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                var positions = await _mapboxApiClient.GetDirectionsAsync(routeType, current, destination);

                return positions;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
