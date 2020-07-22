using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Appmilla.RestApiClient;
using Appmilla.RestApiClient.Interfaces;
using GeoJSON.Net.Geometry;
using MapboxApi.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindAndExplore.Http
{
    public enum DirectionsType
    {
        Driving,
        Cycling,
        Walking
    }

    public class MapboxApiException : Exception
    {
    }

    public class MapboxApiClient
    {
        readonly IApiService _apiService;

        public MapboxApiClient(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ICollection<Position>> GetDirectionsAsync(DirectionsType directionsType, Position current, Position destination)
        {
            var t = $"MapboxApi-dev/Route/?routetype={directionsType.ToString().ToLower()}&startlat={current.Latitude}&startlon={current.Longitude}&endlat={destination.Latitude}&endlon={destination.Longitude}";

            var result = await _apiService.GetUrl<DirectionsResponse>($"MapboxApi-dev/Route/?routetype={directionsType.ToString().ToLower()}&startlat={current.Latitude}&startlon={current.Longitude}&endlat={destination.Latitude}&endlon={destination.Longitude}").ConfigureAwait(false);

            if (result.ResponseType != ResponseTypes.Success)
                throw new FoursquareApiException();

            var positions = new List<Position>();

            foreach (var coordinate in result.Result.Routes[0].Geometry.Coordinates)
            {
                positions.Add(new Position(coordinate[1], coordinate[0]));
            }

            return positions;
        }

        public async Task<HealthCheckResult> HealthCheck()
        {
            var result = await _apiService.GetUrl<HealthCheckResult>($"MapboxApi-dev/Route").ConfigureAwait(false);

            if (result.ResponseType != ResponseTypes.Success)
                throw new FoursquareApiException();

            return result.Result;
        }

    }
}
