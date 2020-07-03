namespace FindAndExplore.Mapping.Layers
{
    public class StyleLayer : Layer
    {
        public string SourceId
        {
            get;
            private set;
        }

        public string SourceLayer { get; set; }

        public StyleLayer(string id, string sourceId) : base(id)
        {
            SourceId = sourceId;
        }
    }
}