using System;
using System.Collections.Generic;
using GeoJSON.Net.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.ViewModels
{
    public class LocationViewModel : ReactiveObject
    {
        [Reactive]
        public Point Position { get; set; }
        
        [Reactive]
        public string Address { get; set; }
        
        [Reactive]
        public string City { get; set; }
        
        [Reactive]
        public string State { get; set; }
        
        [Reactive]
        public string PostalCode { get; set; }
        
        [Reactive]
        public string Country { get; set; }
    }
    
    public class IconViewModel
    {
        [Reactive]
        public Uri Prefix { get; set; }

        [Reactive]
        public string Suffix { get; set; }
    }
    
    public class CategoryViewModel
    {
        [Reactive]
        public string Id { get; set; }

        [Reactive]
        public string Name { get; set; }

        [Reactive]
        public string PluralName { get; set; }

        [Reactive]
        public string ShortName { get; set; }

        [Reactive]
        public IconViewModel Icon { get; set; }

        [Reactive]
        public bool Primary { get; set; }
    }
    
    public class PlaceViewModel : ReactiveObject
    {
        [Reactive]
        public string Id { get; set; }
        
        [Reactive]
        public string Name { get; set; }
        
        [Reactive]
        public LocationViewModel Location { get; set; }
        
        [Reactive]
        public ICollection<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
        
        [Reactive]
        public string Source { get; set; }
    }
}