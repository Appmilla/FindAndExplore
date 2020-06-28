using FindAndExplore.Mapping.Layers;
using GeoJSON.Net;

namespace FindAndExplore.Mapping
{
    public interface IMapLayerController
    {
        bool AddSource(params Sources.Source[] sources);
        bool UpdateSource(string sourceId, IGeoJSONObject featureCollection);
        //bool UpdateSource(string sourceId, ImageSource featureCollection);
        void RemoveSource(params string[] sourceIds);

        bool AddLayer(params Layers.Layer[] layers);
        bool AddLayerBelow(Layers.Layer layer, string layerId);
        bool AddLayerAbove(Layers.Layer layer, string layerId);
        bool AddLayerAt(Layers.Layer layer, int index);
        bool UpdateLayer(Layers.Layer layer);
        void RemoveLayer(params string[] layerIds);

        StyleLayer[] GetLayers();
    }

}