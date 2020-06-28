using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FindAndExplore.DatasetProviders
{
    public interface IDatasetProvider<TViewModel, TCollection, TKey> where TViewModel : class//, IReactiveObject
    {
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        bool IsBusy { get; }
        
        ReactiveCommand<Position, TCollection> Load { get; }
        ReactiveCommand<Position, TCollection> Refresh { get; }
        ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; }
        SourceCache<TViewModel, TKey> ViewModelCache { get; }
        
        FeatureCollection Features { get; }
    }

    public class DatasetProvider<TViewModel, TCollection, TKey> : DatasetProvider, IDatasetProvider<TViewModel, TCollection, TKey>
        where TViewModel : class//, IReactiveObject
        where TCollection : ICollection<TViewModel>//, INotifyCollectionChanged
    {
        [ObservableAsProperty]
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public bool IsBusy { get; }
        
        public ReactiveCommand<Position, TCollection> Load { get; protected set; }

        public ReactiveCommand<Position, TCollection> Refresh { get; protected set; }
        public ReactiveCommand<Unit, Unit> CancelInFlightQueries { get; protected set; }
        
        public SourceCache<TViewModel, TKey> ViewModelCache { get; protected set; }
        
        public FeatureCollection Features { get; protected set; }
        
        protected DatasetProvider()
        {
            
        }
    }
    
    public class DatasetProvider : ReactiveObject//, IDatasetProvider
    {
        
    }
}