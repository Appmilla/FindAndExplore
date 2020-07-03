using System.Linq;
using Android.App;
using Android.Graphics;
using Com.Mapbox.Geojson;
using Com.Mapbox.Mapboxsdk.Maps;
using Com.Mapbox.Mapboxsdk.Style.Sources;
using FindAndExplore.Mapping;
using GeoJSON.Net;
//using Naxam.Mapbox.Platform.Droid.Extensions;
using Newtonsoft.Json;
using Com.Mapbox.Mapboxsdk.Style.Light;
using Com.Mapbox.Mapboxsdk.Utils;
using FindAndExplore.Mapping.Layers;

using Light = FindAndExplore.Mapping.Light;

using NxSource = FindAndExplore.Mapping.Sources.Source;

namespace FindAndExplore.Droid.Mapping
{
    public class MapLayerController : IMapLayerController
    {
        public Style MapStyle { get; set; }
        
        public bool AddSource(params NxSource[] sources)
        {
            for (int i = 0; i < sources.Length; i++)
            { 
                if (string.IsNullOrWhiteSpace(sources[i].Id)) continue;

                MapStyle.AddSource(sources[i].ToSource(/*Context*/));
            }

            return true;
        }

        public bool UpdateSource(string sourceId, IGeoJSONObject geoJsonObject)
        {
            var source = MapStyle.GetSource(sourceId) as Com.Mapbox.Mapboxsdk.Style.Sources.GeoJsonSource;

            if (source == null) return false;

            var json = JsonConvert.SerializeObject(geoJsonObject);

            switch (geoJsonObject)
            {
                case GeoJSON.Net.Feature.Feature feature:
                    source.SetGeoJson(Feature.FromJson(json));
                    break;
                default:
                    source.SetGeoJson(FeatureCollection.FromJson(json));
                    break;
            }

            return true;
        }

        /*
        public bool UpdateSource(string sourceId, ImageSource imageSource)
        {
            var source = mapStyle.GetSource(sourceId) as Com.Mapbox.Mapboxsdk.Style.Sources.ImageSource;

            if (source == null) return false;

            // TODO Cache image
            source.SetImage(imageSource.GetBitmap(Context));
            
            return true;
        }
        */
        
        public void RemoveSource(params string[] sourceIds)
        {
            for (int i = 0; i < sourceIds.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(sourceIds[i])) continue;

                MapStyle.RemoveSource(sourceIds[i]);
            }
        }
        
        public void RemoveLayer(params string[] layerIds)
        {
            for (int i = 0; i < layerIds.Length; i++)
            {
                var native = MapStyle.GetLayer(layerIds[i]);

                if (native == null) continue;

                MapStyle.RemoveLayer(native);
            }
        }

        public bool AddLayer(params Layer[] layers)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(layers[i].Id)) continue;

                var styleLayer = layers[i] as StyleLayer;

                if (styleLayer == null) continue;

                var source = MapStyle.GetSource(styleLayer.SourceId);

                if (source == null) continue;

                var layer = layers[i].ToNative();

                MapStyle.AddLayer(layer);
            }

            return true;
        }

        public bool AddLayerBelow(Layer layer, string layerId)
        {
            MapStyle.AddLayerBelow(layer.ToNative(), layerId);

            return true;
        }

        public bool AddLayerAbove(Layer layer, string layerId)
        {
            MapStyle.AddLayerAbove(layer.ToNative(), layerId);

            return true;
        }

        public bool UpdateLayer(Layer layer)
        {
            var nativeLayer = MapStyle.GetLayer(layer.Id);

            if (nativeLayer == null) return false;

            layer.UpdateLayer(nativeLayer);

            return true;
        }

        public bool AddLayerAt(Layer layer, int index)
        {
            MapStyle.AddLayerAt(layer.ToNative(), index);

            return true;
        }

        public StyleLayer[] GetLayers()
        {
            return MapStyle.Layers.Select(x => x.ToForms()).Where(x => x != null).ToArray();
        }

        public void AddImage(string imageId, string resourceId)
        {
            int resId = Application.Context.Resources.GetIdentifier(resourceId, "drawable", Application.Context.PackageName);
            
            var bitmap = BitmapFactory.DecodeResource(Application.Context.Resources, resId);
            MapStyle.AddImage(imageId, bitmap);
        }
        
        /*
        public void UpdateLight(Light light)
        {
            var native = MapStyle.Light;
            if (!string.IsNullOrWhiteSpace(light.Anchor))
            {
                native.Anchor = light.Anchor;
            }

            if (light.Color != null)
            {
                native.Color = ColorUtils.ColorToRgbaString(light.Color.Value.ToAndroid());
            }

            if (light.ColorTransition != null)
            {
                native.ColorTransition = light.ColorTransition.ToNative();
            }

            if (light.Intensity.HasValue)
            {
                native.Intensity = light.Intensity.Value;
            }

            if (light.IntensityTransition != null)
            {
                native.IntensityTransition = light.IntensityTransition.ToNative();
            }

            if (light.Position.HasValue)
            {
                native.Position = new Position(
                    light.Position.Value.Radial, 
                    light.Position.Value.Azimuthal, 
                    light.Position.Value.Polar);
            }

            if (light.PositionTransition != null)
            {
                native.PositionTransition = light.PositionTransition.ToNative();
            }
        }
        */

    }
}