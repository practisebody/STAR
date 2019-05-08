using LCY;
using SimpleJSON;
using UnityEngine;

namespace STAR
{
    /// <summary>
    /// Base class for an annotation
    /// </summary>
    abstract public class Annotation
    {
        public enum AnnotationType
        {
            TOOL,
            POLYLINE,
        };
        
        public int ID { get; protected set; }
        public AnnotationType Type { get; protected set; }
        public SE3 Matrix { get; protected set; }

        /// <summary>
        /// Constructor that construct from a json string
        /// </summary>
        public Annotation(JSONNode node)
        {
            JSONNode pose = node["pose_information"];
            Matrix4x4 m = new Matrix4x4();
            m.SetTRS(new Vector3(pose["posX"].AsFloat, pose["posY"].AsFloat, pose["posZ"].AsFloat),
                new Quaternion(pose["rotX"].AsFloat, pose["rotY"].AsFloat, pose["rotZ"].AsFloat, pose["rotW"].AsFloat), Vector3.one);
            m.m02 = -m.m02;
            m.m12 = -m.m12;
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m23 = -m.m23;
            Matrix = m;
        }
    }
}