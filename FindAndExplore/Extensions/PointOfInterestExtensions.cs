using System;
using System.Collections.Generic;
using System.Linq;
using FindAndExploreApi.Client;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;

namespace FindAndExplore.Extensions
{
    public static class PointOfInterestExtensions
    {
        public static Feature ToFeature(this PointOfInterest value)
        {
            var properties = new Dictionary<string, object>()
            {
                { "name", value.Name },
                { "category", value.Category },
            };
            var feature = new Feature(value.Location, properties, value.Id);

            return feature;
        }

        public static FeatureCollection ToFeatureCollection(this IEnumerable<PointOfInterest> value)
        {
            var features = value.Select(poi => poi.ToFeature()).ToList();
            return new FeatureCollection(features);
        }

        public static string ToGeoJsonFeatureSource(this IEnumerable<PointOfInterest> value)
            => JsonConvert.SerializeObject(value.ToFeatureCollection(), QuickTypeConverter.Settings);
    }
}
