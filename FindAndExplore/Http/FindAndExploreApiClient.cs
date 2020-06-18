using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Appmilla.RestApiClient;
using Appmilla.RestApiClient.Interfaces;
using FindAndExploreApi.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindAndExplore.Http
{
    public class FindAndExploreApiException : Exception
    {
    }

    public class FindAndExploreApiClient
    {
        readonly IApiService _apiService;

        public FindAndExploreApiClient(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ICollection<SupportedArea>> GetCurrentAreaAsync(double lat, double lon)
        {
            var result = await _apiService.GetUrl<ICollection<SupportedArea>>($"FindAndExploreApi-dev/v1/CurrentArea?lat={lat}&lon={lon}").ConfigureAwait(false);

            if (result.ResponseType != ResponseTypes.Success)
                throw new FindAndExploreApiException();

            return result.Result;
        }

        public async Task<ICollection<PointOfInterest>> GetPointsOfInterestAsync(int locationId)
        {
            var result = await _apiService.GetUrl<ICollection<PointOfInterest>>($"FindAndExploreApi-dev/v1/PointsOfInterest?locationId={locationId}").ConfigureAwait(false);

            if (result.ResponseType != ResponseTypes.Success)
                throw new FindAndExploreApiException();

            return result.Result;
        }

        public async Task<HealthCheckResult> HealthCheck()
        {
            var result = await _apiService.GetUrl<HealthCheckResult>($"FindAndExploreApi-dev/v1/Health").ConfigureAwait(false);

            if (result.ResponseType != ResponseTypes.Success)
                throw new FindAndExploreApiException();

            return result.Result;
        }

    }
}
