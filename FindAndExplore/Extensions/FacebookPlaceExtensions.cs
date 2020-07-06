using System;
using System.Collections.Generic;
using System.Linq;
using FacebookApi.Client;
using FindAndExplore.ViewModels;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;

namespace FindAndExplore.Extensions
{
    public static class FacebookPlaceExtensions
    {
        public static Feature ToFeature(this Place value)
        {
            var properties = new Dictionary<string, object>()
            {
                { "name", value.Name },
                { "id", value.Id },
                { "categories", value.CategoryList }
            };

            var location = new Point(new Position(value.Location.Latitude, value.Location.Longitude));

            var feature = new Feature(location, properties, value.Id);

            return feature;
        }

        public static FeatureCollection ToFeatureCollection(this IEnumerable<Place> value)
        {
            var features = value.Select(venue => venue.ToFeature()).ToList();
            return new FeatureCollection(features);
        }

        public static string ToGeoJsonFeatureSource(this IEnumerable<Place> value)
            => JsonConvert.SerializeObject(value.ToFeatureCollection(), QuickTypeConverter.Settings);


        public static PlaceViewModel ToPlaceViewModel(this Place value)
        {
            var locationPosition = new Position(value.Location.Latitude, value.Location.Longitude);

            var place = new PlaceViewModel
            {
                Id = value.Id,
                Name = value.Name,
                Location = new LocationViewModel
                {
                    Position = new Point(locationPosition),
                    Address = value.Location.Street,
                    City = value.Location.City,
                    PostalCode = value.Location.Zip,
                    Country = value.Location.Country
                },
                Source = "Facebook"
            };

            foreach (var mappedCategory in value.CategoryList.Select(category => new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name
            }))
            {
                place.Categories.Add(mappedCategory);
            }
            return place;
        }

        public static ICollection<PlaceViewModel> ToPlaceCollection(this IEnumerable<Place> value)
        {
            var places = value.Select(place => place.ToPlaceViewModel()).ToList();
            return new List<PlaceViewModel>(places);
        }
    }
}
