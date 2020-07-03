using System.Collections.Generic;
using System.Linq;
using FindAndExplore.ViewModels;
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
        
        
        public static PlaceViewModel ToPlaceViewModel(this Venue value)
        {
            var locationPosition = new Position(value.Location.Lat, value.Location.Lng);

            var place = new PlaceViewModel
            {
                Id = value.Id,
                Name =  value.Name,
                Location = new LocationViewModel
                {
                    Position = locationPosition,
                    Address = value.Location.Address,
                    City = value.Location.City,
                    State = value.Location.State,
                    PostalCode = value.Location.PostalCode,
                    Country = value.Location.Country
                },
            };

            foreach (var mappedCategory in value.Categories.Select(category => new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                PluralName = category.PluralName,
                ShortName = category.ShortName,
                Icon = new IconViewModel
                {
                    Prefix = category.Icon.Prefix,
                    Suffix = category.Icon.Suffix
                },
                Primary = category.Primary
            }))
            {
                place.Categories.Add(mappedCategory);
            }
            return place;
        }
        
        public static ICollection<PlaceViewModel> ToPlaceCollection(this IEnumerable<Venue> value)
        {
            var places = value.Select(venue => venue.ToPlaceViewModel()).ToList();
            return new List<PlaceViewModel>(places);
        }
    }    
}
