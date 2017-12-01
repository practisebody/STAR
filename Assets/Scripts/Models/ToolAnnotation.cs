using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public class ToolAnnotation : Annotation
    {
        public Vector2 Position { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; }
        public string ToolType { get; set; }
        public string SelectableColor { get; set; }

        public ToolAnnotation(JSONNode node)
        {
            Type = AnnotationType.TOOL;
            Position = new Vector2(node["annotationPoints"][0]["x"].AsFloat, node["annotationPoints"][0]["y"].AsFloat);
            Rotation = node["rotation"].AsFloat;
            Scale = node["scale"].AsFloat;
            ToolType = node["toolType"].Value;
            SelectableColor = node["selectableColor"].Value;
        }

        public override string ToString()
        {
            return "(" + Position.x.ToString(Utilities.FloatFormat) + "," + Position.y.ToString(Utilities.FloatFormat) + ") r:" + Rotation + " s:" + Scale + " " + ToolType + " " + SelectableColor;
        }
    }
}