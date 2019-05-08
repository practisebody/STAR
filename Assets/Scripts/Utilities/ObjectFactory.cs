using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using LCY;

namespace STAR
{
    /// <summary>
    /// Instantiate prefab objects
    /// </summary>
    public class ObjectFactory : UnitySingleton<ObjectFactory>
    {
        private void Awake()
        {
            InitGizmoPrefab();
            InitRayPrefab();
            InitTagPrefab();
            InitToolPrefab();
            InitPolylinePrefab();
            InitVitalSignPrefab();
        }

        #region gizmo
        
        static protected GameObject GizmoPrefab { get; set; }

        static protected void InitGizmoPrefab()
        {
            GizmoPrefab = Resources.Load<GameObject>("Gizmo");
        }

        static public GameObject NewGizmo(Transform parent, Vector3 position, Quaternion rotation)
        {
            return Instantiate(GizmoPrefab, position, rotation, parent);
        }

        #endregion

        #region ray

        static protected LineRenderer RayPrefab { get; set; }

        static protected void InitRayPrefab()
        {
            RayPrefab = Resources.Load<LineRenderer>("Ray");
        }

        static public LineRenderer NewRay(Transform parent, Vector3 start, Vector3 end)
        {
            LineRenderer ray = Object.Instantiate<LineRenderer>(RayPrefab, parent);
            ray.positionCount = 2;
            ray.SetPosition(0, start);
            ray.SetPosition(1, end);
            return ray;
        }

        static public LineRenderer NewRay(Transform parent, Vector3 start, Vector3 end, Color color)
        {
            LineRenderer ray = NewRay(parent, start, end);
            ray.startColor = color;
            ray.endColor = color;
            return ray;
        }

        static public LineRenderer NewRay(Transform parent, Vector3 start, Vector3 end, Color color, float width)
        {
            LineRenderer ray = NewRay(parent, start, end, color);
            ray.startWidth = width;
            ray.endWidth = width;
            return ray;
        }

        #endregion

        #region Tag

        static protected GameObject TagPrefab { get; set; }

        static protected void InitTagPrefab()
        {
            TagPrefab = Resources.Load<GameObject>("Tag");
        }

        static public GameObject NewTag(Transform parent, Vector3 position)
        {
            return Instantiate(TagPrefab, position, new Quaternion(), parent);
        }

        #endregion

        #region annotation data

        static public Annotation NewAnnotation(JSONNode node)
        {
            string type = node["annotation_memory"]["annotation"]["annotationType"].Value;
            switch (type)
            {
                case "polyline":
                    return new PolylineAnnotation(node);
                case "tool":
                    return new ToolAnnotation(node);
                default:
                    throw new System.Exception("Unknown annotation type!");
            }
        }

        #endregion

        #region annotation objects

        #region tool annotation

        static protected Dictionary<string, GameObject> ToolPrefabs { get; set; }
        static protected GameObject PlaceholderToolPrefab { get; set; }

        static protected void InitToolPrefab()
        {
            GameObject[] all = Resources.LoadAll<GameObject>("Annotations/Tools");
            ToolPrefabs = new Dictionary<string, GameObject>();
            foreach (GameObject prefab in all)
            {
                string name = prefab.name.ToLower();
                if (name == "placeholder")
                    PlaceholderToolPrefab = prefab;
                else
                    ToolPrefabs.Add(name, prefab);
            }
        }

        static public GameObject NewTool(Transform parent, string toolType, Quaternion rotation, Vector3 position, float scale)
        {
            GameObject tool;
            if (ToolPrefabs.TryGetValue(toolType.ToLower(), out tool) == false)
                tool = PlaceholderToolPrefab;
            GameObject obj = Object.Instantiate(tool, parent);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.transform.localScale = new Vector3(scale, scale, scale);
            return obj;
        }

        #endregion

        #region polyline annotation

        static protected LineRenderer PolylinePrefab { get; set; }

        static protected void InitPolylinePrefab()
        {
            PolylinePrefab = Resources.Load<LineRenderer>("Annotations/Polyline");
        }

        static public LineRenderer NewPolyline(Transform parent, List<Vector3> positions)
        {
            LineRenderer polyline = Object.Instantiate<LineRenderer>(PolylinePrefab, parent);
            polyline.positionCount = positions.Count;
            polyline.SetPositions(positions.ToArray());
            return polyline;
        }

        static public LineRenderer NewPolyline(Transform parent, List<Vector3> positions, Color color)
        {
            LineRenderer polyline = NewPolyline(parent, positions);
            polyline.startColor = color;
            polyline.endColor = color;
            return polyline;
        }

        static public LineRenderer NewPolyline(Transform parent, List<Vector3> positions, Color color, float width)
        {
            LineRenderer polyline = NewPolyline(parent, positions, color);
            polyline.startWidth = width;
            polyline.endWidth = width;
            return polyline;
        }

        #endregion

        #endregion

        #region vital sign

        static protected GameObject VitalSignPrefab { get; set; }

        static protected void InitVitalSignPrefab()
        {
            VitalSignPrefab = Resources.Load<GameObject>("VitalSign/VitalSign");
        }

        static public VitalSign NewVitalSign(Transform parent, Vector3 pos, Color color, string name, string high, string low)
        {
            VitalSign vital = Instantiate(VitalSignPrefab, parent).GetComponentInChildren<VitalSign>();
            vital.Init(pos, color, name, high, low);
            return vital;
        }

        #endregion
    }
}
