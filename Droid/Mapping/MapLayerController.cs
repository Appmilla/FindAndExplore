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
using System.Collections.Generic;
using Java.Lang;

using MapboxsdkLayers = Com.Mapbox.Mapboxsdk.Style.Layers;
using System;
using Android.Animation;
using Com.Mapbox.Turf;
using Android.Views.Animations;
using static Android.Animation.ValueAnimator;
using Java.Interop;

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

        GeoJsonSource _geoJsonSource;
        MapboxsdkLayers.LineLayer _lineLayer;
        List<Com.Mapbox.Geojson.Point> _points;
        List<Com.Mapbox.Geojson.Point> _currentPoints;
        int _routeIndex;
        private Animator _currentAnimator;

        public void AddDirections(ICollection<GeoJSON.Net.Geometry.Position> positions)
        {
            var guid = Guid.NewGuid();

            if (_lineLayer != null)
            {
                MapStyle.RemoveLayer(_lineLayer);
                _lineLayer = null;
            }
            if (_geoJsonSource != null)
            {
                MapStyle.RemoveSource(_geoJsonSource);
                _geoJsonSource = null;
            }

            _points = new List<Com.Mapbox.Geojson.Point>();
            _currentPoints = new List<Com.Mapbox.Geojson.Point>();
            _routeIndex = 0;

            foreach (var position in positions)
            {
                _points.Add(Com.Mapbox.Geojson.Point.FromLngLat(position.Longitude, position.Latitude));
            }

            _geoJsonSource = new GeoJsonSource("line-source",
                FeatureCollection.FromFeatures(new Feature[] {Feature.FromGeometry(
                LineString.FromLngLats(MultiPoint.FromLngLats(new List<Com.Mapbox.Geojson.Point>())))}));
            MapStyle.AddSource(_geoJsonSource);

            _lineLayer = new MapboxsdkLayers.LineLayer("linelayer", "line-source");
            _lineLayer.SetProperties(
                MapboxsdkLayers.PropertyFactory.LineCap(MapboxsdkLayers.Property.LineCapRound),
                MapboxsdkLayers.PropertyFactory.LineJoin(MapboxsdkLayers.Property.LineJoinRound),
                MapboxsdkLayers.PropertyFactory.LineWidth(new Float(5)),
                MapboxsdkLayers.PropertyFactory.LineColor(Color.ParseColor("#e55e5e"))
                );
            MapStyle.AddLayer(_lineLayer);
            Animate();
        }

        private void Animate()
        {
            if (_points.Count - 1 > _routeIndex)
            {
                Com.Mapbox.Geojson.Point indexPoint = _points[_routeIndex];
                Com.Mapbox.Geojson.Point newPoint = Com.Mapbox.Geojson.Point.FromLngLat(indexPoint.Longitude(), indexPoint.Latitude());
                _currentAnimator = CreateLatLngAnimator(indexPoint, newPoint);
                _currentAnimator.Start();
                _routeIndex++;
            }
        }

        private Animator CreateLatLngAnimator(Com.Mapbox.Geojson.Point currentPosition, Com.Mapbox.Geojson.Point targetPosition)
        {
            ValueAnimator latLngAnimator = ValueAnimator.OfObject(new PointEvaluator(), currentPosition, targetPosition);
            //TODO fix this, currenrtly returning 0
            //latLngAnimator.SetDuration((long)TurfMeasurement.Distance(currentPosition, targetPosition, "meters"));
            latLngAnimator.SetDuration(100);
            latLngAnimator.SetInterpolator(new LinearInterpolator());
            latLngAnimator.AddListener(new MyAnimatorListenerAdapter(Animate));
            latLngAnimator.AddUpdateListener(new MyAnimatorUpdateListener(_geoJsonSource, _currentPoints));
            return latLngAnimator;
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

    class MyAnimatorListenerAdapter : AnimatorListenerAdapter
    {
        Action _action;

        public MyAnimatorListenerAdapter(Action action)
        {
            _action = action;
        }

        public override void OnAnimationEnd(Animator animation)
        {
            base.OnAnimationEnd(animation);
            _action();
        }
    }

    class MyAnimatorUpdateListener : Java.Lang.Object, IAnimatorUpdateListener
    {
        private List<Com.Mapbox.Geojson.Point> _markerLinePointList;
        GeoJsonSource _geoJsonSource;

        public MyAnimatorUpdateListener(GeoJsonSource geoJsonSource, List<Com.Mapbox.Geojson.Point> markerLinePointList)
        {
            _geoJsonSource = geoJsonSource;
            _markerLinePointList = markerLinePointList;
        }

        public void OnAnimationUpdate(ValueAnimator animation)
        {
            Com.Mapbox.Geojson.Point point = (Com.Mapbox.Geojson.Point)animation.AnimatedValue;
            _markerLinePointList.Add(point);
            _geoJsonSource.SetGeoJson(Feature.FromGeometry(LineString.FromLngLats(_markerLinePointList)));
        }
    }

    class PointEvaluator : Java.Lang.Object, ITypeEvaluator
    {
        public Java.Lang.Object Evaluate(float fraction, Java.Lang.Object startValue, Java.Lang.Object endValue)
        {
            Com.Mapbox.Geojson.Point start = (Com.Mapbox.Geojson.Point)startValue;
            Com.Mapbox.Geojson.Point end = (Com.Mapbox.Geojson.Point)endValue;

            return Com.Mapbox.Geojson.Point.FromLngLat(
                start.Longitude() + ((end.Longitude() - start.Longitude()) * fraction),
                start.Latitude() + ((end.Latitude() - start.Latitude()) * fraction));
         }
    }
}