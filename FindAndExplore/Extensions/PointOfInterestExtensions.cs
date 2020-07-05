using System;
using System.Collections.Generic;
using System.Linq;
using FindAndExplore.ViewModels;
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
        
        public static PlaceViewModel ToPlaceViewModel(this PointOfInterest value)
        {
            var place = new PlaceViewModel
            {
                Id = value.Id,
                Name =  value.Name,
                Location = new LocationViewModel
                {
                    Position = value.Location/*,
                    Address = value.Location.Address,
                    City = value.Location.City,
                    State = value.Location.State,
                    PostalCode = value.Location.PostalCode,
                    Country = value.Location.Country*/
                },
                Source = "Find And Explore MongoDB"
            };

            place.Categories.Add(new CategoryViewModel
            {
                Name = value.Category
            });
            
            return place;
        }
        
        public static ICollection<PlaceViewModel> ToPlaceCollection(this IEnumerable<PointOfInterest> value)
        {
            var places = value.Select(PointOfInterest => PointOfInterest.ToPlaceViewModel()).ToList();
            return new List<PlaceViewModel>(places);
        }
    }
}
