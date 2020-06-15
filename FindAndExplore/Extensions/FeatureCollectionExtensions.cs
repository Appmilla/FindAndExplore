using GeoJSON.Net.Feature;
using Newtonsoft.Json;

namespace FindAndExplore.Extensions
{
    public static class FeatureCollectionExtensions
    {
        public static string ToGeoJsonFeatureSource(this FeatureCollection value)
            => JsonConvert.SerializeObject(value, QuickTypeConverter.Settings);  
    }
}