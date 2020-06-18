using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Appmilla.RestApiClient;
using Appmilla.RestApiClient.Interfaces;
using FoursquareApi.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindAndExplore.Http
{
    public class FoursquareApiException : Exception
    {
    }

    public class FoursquareApiClient
    {
        readonly IApiService _apiService;

        public FoursquareApiClient(IApiService apiService)
        {
            _apiService = apiService;
        }
        
        public async Task<ICollection<Venue>> GetVenuesAsync(double lat, double lon, int radius)
        {
            var result = await _apiService.GetUrl<VenuesResponse>($"FoursquareApi-dev/Venues?lat={lat}&lon={lon}&radius={radius}").ConfigureAwait(false);

            if (result.ResponseType != ResponseTypes.Success)
                throw new FoursquareApiException();

            return result.Result.Response.Venues;
        }

        public async Task<HealthCheckResult> HealthCheck()
        {
            var result = await _apiService.GetUrl<HealthCheckResult>($"FoursquareApi-dev/Health").ConfigureAwait(false);

            if (result.ResponseType != ResponseTypes.Success)
                throw new FoursquareApiException();

            return result.Result;
        }

    }
}
