﻿using LCY;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace STAR
{
    /// <summary>
    /// A polyline annotation
    /// </summary>
    public class PolylineAnnotation : Annotation
    {
        public List<Vector2> Positions { get; protected set; }

        public PolylineAnnotation(JSONNode node) : base(node)
        {
            Type = AnnotationType.POLYLINE;
            JSONNode anno = node["annotation_memory"]["annotation"];
            Positions = new List<Vector2>();
            foreach (JSONNode n in anno["annotationPoints"].AsArray)
            {
                Positions.Add(new Vector2(n["x"].AsFloat, n["y"].AsFloat));
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(100);
            int len = Positions.Count;
            for (int i = 0; i < Math.Min(4, len); ++i)
            {
                sb.Append("(").Append(Positions[i].x.ToString(Utilities.FloatFormat)).Append(",").Append(Positions[i].y.ToString(Utilities.FloatFormat)).Append("),");
            }
            if (len > 5)
                sb.Append("...");
            if (len > 0)
                sb.Append("(").Append(Positions[len - 1].x.ToString(Utilities.FloatFormat)).Append(",").Append(Positions[len - 1].y.ToString(Utilities.FloatFormat)).Append(")");
            return sb.ToString();
        }
    }
}