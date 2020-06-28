using System;
using System.Linq;
using System.Linq;
using Foundation;
using Mapbox;
using FindAndExplore.Mapping.Layers;
using FindAndExplore.Mapping;
using FindAndExplore.Mapping.Sources;
using GeoJSON.Net;
using Mapbox;


using Newtonsoft.Json;
using UIKit;

namespace FindAndExplore.iOS.Mapping
{
    public class MapLayerController : IMapLayerController
    {
        public MGLStyle MapStyle { get; set; }
        
        /*
        public void AddStyleImage(IconImageSource iconImageSource)
        {
            if (iconImageSource.Source == null) return;
            
            var cachedImage = mapStyle.ImageForName(iconImageSource.Id);
            if (cachedImage != null) return;
            
            var image = iconImageSource.Source.GetImage();

            if (image == null)
            {
                return;
            }

            if (iconImageSource.IsTemplate)
            {
                image = image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            }

            mapStyle.SetImage(image, iconImageSource.Id);
        }
        */
        
        public bool AddSource(params Source[] sources)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(sources[i].Id)) continue;

                var src = sources[i].ToSource();
                MapStyle.AddSource(src);
            }

            return true;
        }

        public bool UpdateSource(string sourceId, IGeoJSONObject featureCollection)
        {
            var source = MapStyle.SourceWithIdentifier(sourceId) as MGLShapeSource;

            if (source == null) return false;

            source.Shape = featureCollection.ToShape();

            return true;
        }

        /*
        public bool UpdateSource(string sourceId, ImageSource imageSource)
        {
            var source = mapStyle.SourceWithIdentifier(sourceId) as MGLImageSource;

            if (source == null) return false;

            source.Image = imageSource.GetImage();
            
            return true;
        }
        */
        
        public void RemoveSource(params string[] sourceIds)
        {
            for (int i = 0; i < sourceIds.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(sourceIds[i])) continue;

                var source = MapStyle.SourceWithIdentifier(sourceIds[i]) as MGLSource;

                if (source == null) continue;

                MapStyle.RemoveSource(source);
            }
        }
        
        public void RemoveLayer(params string[] layerIds)
        {
            for (int i = 0; i < layerIds.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(layerIds[i])) continue;

                var layer = MapStyle.LayerWithIdentifier(layerIds[i]);

                if (layer == null) continue;

                MapStyle.RemoveLayer(layer);
                layer.Dispose();
                layer = null;
            }
        }

        public bool AddLayer(params Layer[] layers)
        {
            for (int i = 0; i < layers.Length; i++)
            {
                var layer = CreateLayer(layers[i]);

                if (layer == null) continue;

                MapStyle.AddLayer(layer);
            }

            return true;
        }

        public bool AddLayerAbove(Layer layer, string layerId)
        {
            var aboveLayer = MapStyle.LayerWithIdentifier(layerId);

            if (aboveLayer == null) return false;

            var newLayer = CreateLayer(layer);

            if (newLayer == null) return false;

            MapStyle.InsertLayerAbove(newLayer, aboveLayer);
            return true;
        }

        public bool AddLayerAt(Layer layer, int index)
        {
            if (index < 0) return false;

            var newLayer = CreateLayer(layer);

            if (newLayer == null) return false;

            MapStyle.InsertLayer(newLayer, (System.nuint)index);

            return true;
        }

        public bool AddLayerBelow(Layer layer, string layerId)
        {
            var belowLayer = MapStyle.LayerWithIdentifier(layerId);

            if (belowLayer == null) return false;

            var newLayer = CreateLayer(layer);

            if (newLayer == null) return false;

            MapStyle.InsertLayerBelow(newLayer, belowLayer);
            return true;
        }

        public bool UpdateLayer(Layer layer)
        {
            var nativeLayer = MapStyle.LayerWithIdentifier(layer.Id);

            if (nativeLayer == null) return false;

            layer.UpdateLayer(nativeLayer);

            return true;
        }

        MGLStyleLayer CreateLayer(Layer layer)
        {
            if (string.IsNullOrWhiteSpace(layer.Id)) return null;

            var styleLayer = layer as StyleLayer;

            if (string.IsNullOrWhiteSpace(styleLayer.SourceId)) return null;

            var source = MapStyle.SourceWithIdentifier(styleLayer.SourceId);

            if (source == null) return null;

            return layer.ToLayer(source);
        }

        public StyleLayer[] GetLayers()
        {
            return MapStyle.Layers.Select(x => x.ToForms()).Where(x => x != null).ToArray();
        }
        
        /*
        public void UpdateLight(Light light)
        {
            var native = MapStyle.Light;
            if (!string.IsNullOrWhiteSpace(light.Anchor))
            {
                native.Anchor = NSExpression.FromConstant(new NSString(light.Anchor));
            }

            if (light.Color != null)
            {
                native.Color = NSExpression.FromConstant(light.Color.Value.ToUIColor());
            }

            if (light.ColorTransition != null)
            {
                native.ColorTransition = light.ColorTransition.ToNative();
            }

            if (light.Intensity.HasValue)
            {
                native.Intensity = NSExpression.FromConstant(NSNumber.FromFloat(light.Intensity.Value));
            }

            if (light.IntensityTransition != null)
            {
                native.IntensityTransition = light.IntensityTransition.ToNative();
            }

            if (light.Position.HasValue)
            {
                var position = NSValue_MGLAdditions.ValueWithMGLSphericalPosition(null, new MGLSphericalPosition
                {
                    radial = light.Position.Value.Radial,
                    azimuthal = light.Position.Value.Azimuthal,
                    polar = light.Position.Value.Polar
                });
                native.Position = NSExpression.FromConstant(position);
            }

            if (light.PositionTransition != null)
            {
                native.PositionTransition = light.PositionTransition.ToNative();
            }

            mapStyle.Light = native;
        }
        */
    }
}