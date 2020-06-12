﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Appmilla.RestApiClient;
using Appmilla.RestApiClient.Interfaces;
using FindAndExploreApi.Client;

namespace FindAndExplore.Core.Http
{
    public class FindAndExploreApiClient
    {
        readonly IApiService _apiService;

        public FindAndExploreApiClient(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<ApiServiceResponse<ICollection<SupportedArea>>> GetCurrentAreaAsync(double lat, double lon)
        {
            var result = await _apiService.GetUrl<ICollection<SupportedArea>>($"GetCurrentArea?lat={lat}&lon={lon}").ConfigureAwait(false);

            return result;
        }

        public async Task<ApiServiceResponse<ICollection<PointOfInterest>>> GetPointsOfInterestAsync(int locationId)
        {
            var result = await _apiService.GetUrl<ICollection<PointOfInterest>>($"GetAreasPointsOfInterest?locationId={locationId}").ConfigureAwait(false);

            return result;
        }

    }
}