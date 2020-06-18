using System.Collections.Generic;
using System.Linq;
using FoursquareApi.Client;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;
using Feature = GeoJSON.Net.Feature.Feature;

namespace FindAndExplore.Extensions
{
    public static class VenueExtensions
    {
        public static Feature ToFeature(this Venue value)
        {           
            var properties = new Dictionary<string, object>()
            {
                { "name", value.Name },
                { "id", value.Id },
                { "referralid", value.ReferralId },
                { "categories", value.Categories }               
            };

            var location = new Point(new Position(value.Location.Lat, value.Location.Lng));

            var feature = new Feature(location, properties, value.Id);

            return feature;
        }

        public static FeatureCollection ToFeatureCollection(this IEnumerable<Venue> value)
        {
            var features = value.Select(venue => venue.ToFeature()).ToList();
            return new FeatureCollection(features);
        }

        public static string ToGeoJsonFeatureSource(this IEnumerable<Venue> value)
            => JsonConvert.SerializeObject(value.ToFeatureCollection(), QuickTypeConverter.Settings);
    }    
}
