using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Appmilla.RestApiClient;
using Appmilla.RestApiClient.Interfaces;
using FacebookApi.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindAndExplore.Http
{
    public class FacebookApiException : Exception
    {
    }

    public class FacebookApiClient
    {
        readonly IApiService _apiService;

        public FacebookApiClient(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ICollection<Place>> GetPlacesAsync(double lat, double lon, int radius)
        {
            var result = await _apiService.GetUrl<PlacesResponse>($"FacebookApi-Appmilla-dev/Places?lat={lat}&lon={lon}&radius={radius}").ConfigureAwait(false);

            if (result.ResponseType != ResponseTypes.Success)
                throw new FacebookApiException();

            return result.Result.Venues;
        }

        public async Task<HealthCheckResult> HealthCheck()
        {
            var result = await _apiService.GetUrl<HealthCheckResult>($"FacebookApi-Appmilla-dev/Health").ConfigureAwait(false);

            if (result.ResponseType != ResponseTypes.Success)
                throw new FacebookApiException();

            return result.Result;
        }

    }
}
