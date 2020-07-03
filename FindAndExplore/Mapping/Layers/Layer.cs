namespace FindAndExplore.Mapping.Layers
{
    public class Layer
    {
        public string Id { get; set; }

        public float MinZoom { get; set; }

        public float MaxZoom { get; set; }

        public Expressions.Expression Filter { get; set; }

        public Expressions.Expression Visibility { get; set; }

        public Layer(string id)
        {
            Id = id;
        }
    }
}