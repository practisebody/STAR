using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using LCY;

namespace STAR
{
    public class ObjectFactory : UnitySingleton<ObjectFactory>
    {
        private void Start()
        {
            InitRayPrefab();
            InitCheckerboardPrefab();
            InitToolPrefab();
            InitPolylinePrefab();
        }

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

        #region Checkerboard

        static protected GameObject CheckerPointPrefab;
        static protected GameObject CheckerPointRedPrefab;

        static protected void InitCheckerboardPrefab()
        {
            CheckerPointPrefab = Resources.Load<GameObject>("CheckerPoint");
            CheckerPointRedPrefab = Resources.Load<GameObject>("CheckerPointRed");
        }

        static public void NewCheckerPoints(Transform parent, SE3 transform, int X, int Y, float size)
        {
            for (int i = 0; i < X; ++i)
                for (int j = 0; j < Y; ++j)
                {
                    Vector3 position = transform * new Vector3(i * size, j * size, 0.0f);
                    GameObject obj = Object.Instantiate(CheckerPointRedPrefab, position, Quaternion.identity, parent);
                }
        }

        #endregion

        #region annotation data

        static public Annotation NewAnnotation(JSONNode node)
        {
            JSONNode anno = node["annotation"];
            string type = anno["annotationType"].Value;
            switch (type)
            {
                case "polyline":
                    return new PolylineAnnotation(anno);
                case "tool":
                    return new ToolAnnotation(anno);
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
    }
}
