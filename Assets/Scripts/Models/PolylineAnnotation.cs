using LCY;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public class PolylineAnnotation : Annotation
    {
        public List<Vector2> Positions { get; set; }
        //public List<Stablizer<Vector3>> Positions3 { get; set; }

        public PolylineAnnotation(JSONNode node)
        {
            Type = AnnotationType.POLYLINE;
            Positions = new List<Vector2>();
            foreach (JSONNode n in node["annotationPoints"].AsArray)
            {
                Positions.Add(new Vector2(n["x"].AsFloat, n["y"].AsFloat));
            }
        }

        public override string ToString()
        {
            string result = "";
            for (int i = 0; i < Positions.Count; ++i)
            {
                result += "(" + Positions[i].x.ToString(Utilities.FloatFormat) + "," + Positions[i].y.ToString(Utilities.FloatFormat) + ") ";
                if (i % 10 == 9 && i != Positions.Count - 1)
                    result += "\n";
            }
            return result;
        }
    }
}