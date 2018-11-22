using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public class ToolAnnotation : Annotation
    {
        public Vector2 Position { get; protected set; }
        public float Rotation { get; protected set; }
        public float Scale { get; protected set; }
        public string ToolType { get; protected set; }
        public string SelectableColor { get; protected set; }

        public ToolAnnotation(JSONNode node) : base(node)
        {
            Type = AnnotationType.TOOL;
            JSONNode anno = node["annotation_memory"]["annotation"];
            Position = new Vector2(anno["annotationPoints"][0]["x"].AsFloat, anno["annotationPoints"][0]["y"].AsFloat);
            Rotation = anno["rotation"].AsFloat;
            Scale = anno["scale"].AsFloat;
            ToolType = anno["toolType"].Value;
            SelectableColor = anno["selectableColor"].Value;
        }

        public override string ToString()
        {
            return "(" + Position.x.ToString(Utilities.FloatFormat) + "," + Position.y.ToString(Utilities.FloatFormat) + ") r:" + Rotation + " s:" + Scale + " " + ToolType + " " + SelectableColor;
        }
    }
}