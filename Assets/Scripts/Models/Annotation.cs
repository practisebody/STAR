namespace STAR
{
    abstract public class Annotation
    {
        public int ID { get; set; }
        public enum AnnotationType
        {
            TOOL,
            POLYLINE,
        };
        public AnnotationType Type { get; set; }
    }
}