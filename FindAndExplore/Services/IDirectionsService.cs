using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FindAndExplore.Http;
using GeoJSON.Net.Geometry;
using MapboxApi.Client;

namespace FindAndExplore.Services
{
    public interface IDirectionsService
    {
        Task<ICollection<Position>> GetDirectionsAsync(DirectionsType routeType, Position current, Position destination);
    }
}