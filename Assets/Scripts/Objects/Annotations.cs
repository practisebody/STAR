using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using LCY;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA;

namespace STAR
{
    public class Annotations : MonoBehaviour
    {
        public TopDownCamera TopDownCamera;
        public Camera Camera;
        protected SortedDictionary<int, Annotation> _Annotations { get; } = new SortedDictionary<int, Annotation>();

        private void Start()
        {
            SocketStart();
            AnnotationStart();

            Configurations.Instance.SetAndAddCallback("DummyAnnotaion", 0, v =>
            {
                if (TopDownCamera.CheckerPoints?.Count != 0)
                {
                    Vector2 first = TopDownCamera.CheckerPoints[0];
                    Vector2 last = TopDownCamera.CheckerPoints[TopDownCamera.CheckerPoints.Count - 1];
                    AnnotationReceived("{\"id\":" + v + ",\"command\":\"CreateAnnotationCommand\",\"annotation_memory\":{\"annotation\":{\"annotationPoints\":["
                        + "{\"x\":" + first.x / TopDownCamera.Width + ",\"y\":" + (1.0 - first.y / TopDownCamera.Height) + "},"
                        + "{\"x\":" + last.x / TopDownCamera.Width + ",\"y\":" + (1.0 - last.y / TopDownCamera.Height) + "}"
                        + "],\"annotationType\":\"polyline\"}}}");
                }
            }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("DummyDenseAnnotaion", 100, v =>
            {
                string str = "{\"id\":0,\"command\":\"CreateAnnotationCommand\",\"annotation_memory\":{\"annotation\":{\"annotationPoints\":[";
                str += "{\"x\":0.0,\"y\":0.0}";
                for (int i = 1; i < v; ++i)
                {
                    str += ",{\"x\":" + ((float)i / v) + ",\"y\":" + ((float)i / v) + "}";
                }
                str += "],\"annotationType\":\"polyline\"}}}";
                AnnotationReceived(str);
            }, Configurations.RunOnMainThead.NO);
        }

        #region socket

        public string Hostname;
        public int Port;
        protected USocketClient Client = null;
        public bool Connected { get { return Client?.Connected ?? false; } }

        protected void SocketStart()
        {
            Client = new USocketClient(Hostname, Port);
            Client.MessageReceived += AnnotationReceived;
            Client.Persistent = true;
            Client.Timeout = 1000;
            Client.Connect();
            Configurations.Instance.SetAndAddCallback("AnnotationServerIP", Hostname, v => Client.Host = v);
            Configurations.Instance.SetAndAddCallback("AnnotationServerPort", Port, v => Client.Port = v);
        }

        protected void AnnotationReceived(string s)
        {
            try
            {
                JSONNode node = JSON.Parse(s);
                string type = node["command"].Value;
                int id = node["id"].AsInt;
                switch (type)
                {
                    case "CreateAnnotationCommand":
                    case "UpdateAnnotationCommand":
                        _Annotations[id] = ObjectFactory.NewAnnotation(node["annotation_memory"]);
                        break;
                    case "DeleteAnnotationCommand":
                        _Annotations.Remove(id);
                        break;
                    default:
                        throw new Exception("Unrecognized command type");
                }
            }
            catch (Exception e)
            {
                Utilities.LogException(e);
            }
        }

        #endregion

        #region annotation objects

        public Checkerboard Checkerboard;
        protected Transform AnnotationsTransform;
        protected Transform RaysTransform;
        protected bool ShowAnnotationRays { get; set; } = true;
        protected float ToolScale { get; set; } = 1.0f;
        protected float ToolOffset { get; set; } = 0.0f;
        protected float PolylineWidth { get; set; } = 0.015f;
        protected float PolylineBrightnessMultiplier { get; set; } = 1.0f;
        protected bool AnnotationAnchor { get; set; } = false;
        static protected readonly Color DefaultPolylineColor = new Color(0.498f, 1.0f, 0.831f);
        protected Color PolylineColor { get; set; } = DefaultPolylineColor;

        protected void AnnotationStart()
        {
            AnnotationsTransform = transform.Find("Annotations");
            RaysTransform = transform.Find("Rays");
            TryAttach(SpatialMappingManager.Instance.Source);
            SpatialMappingManager.Instance.SourceChanged += OnSourceChanged;
            Configurations.Instance.SetAndAddCallback("ShowAnnotationRays", true, v => RaysTransform.gameObject.SetActive(ShowAnnotationRays = v), Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("ToolScale", 1.0f, v => { ToolScale = v; Refresh(); });
            Configurations.Instance.SetAndAddCallback("ToolOffset", 0.0f, v => { ToolOffset = v; Refresh(); });
            Configurations.Instance.SetAndAddCallback("PolylineWidth", 0.015f, v => { PolylineWidth = v; Refresh(); });
            Configurations.Instance.SetAndAddCallback("PolylineBrightnessMultiplier", 1.0f, v => { PolylineColor = v * DefaultPolylineColor; Refresh(); });
            Configurations.Instance.SetAndAddCallback("AnnotationAnchor", false, v => { AnnotationAnchor = v; Refresh(); });
        }

        protected void OnSourceChanged(object sender, HoloToolkit.Unity.PropertyChangedEventArgsEx<SpatialMappingSource> e)
        {
            TryDetach(e.OldValue);
            TryAttach(e.NewValue);
        }

        protected void TryAttach(SpatialMappingSource source)
        {
            if (source != null)
            {
                source.SurfaceAdded += OnSurfaceAdded;
                source.SurfaceUpdated += OnSurfaceUpdated;
                source.SurfaceRemoved += OnSurfaceRemoved;
                source.RemovingAllSurfaces += OnRemovingAllSurfaces;
            }
        }

        protected void TryDetach(SpatialMappingSource source)
        {
            if (source != null)
            {
                source.SurfaceAdded -= OnSurfaceAdded;
                source.SurfaceUpdated -= OnSurfaceUpdated;
                source.SurfaceRemoved -= OnSurfaceRemoved;
                source.RemovingAllSurfaces -= OnRemovingAllSurfaces;
            }
        }

        protected void OnSurfaceAdded(object sender, DataEventArgs<SpatialMappingSource.SurfaceObject> e)
        {
            Refresh();
        }

        protected void OnSurfaceUpdated(object sender, DataEventArgs<SpatialMappingSource.SurfaceUpdate> e)
        {
            Refresh();
        }

        protected void OnSurfaceRemoved(object sender, DataEventArgs<SpatialMappingSource.SurfaceObject> e)
        {
            Refresh();
        }

        protected void OnRemovingAllSurfaces(object sender, EventArgs e)
        {
            Refresh();
        }
        
        protected void Refresh()
        {
            if (TopDownCamera.Valid == false)
                return;
            LCY.Utilities.DestroyChildren(AnnotationsTransform);
            LCY.Utilities.DestroyChildren(RaysTransform);
            RaycastHit hitInfo;
            Vector3 unproj, direction;
            SE3 matrix = TopDownCamera.LocalToWorldMatrix;
            foreach (KeyValuePair<int, Annotation> entry in _Annotations)
            {
                switch (entry.Value.Type)
                {
                    case Annotation.AnnotationType.TOOL:
                        ToolAnnotation tool = (ToolAnnotation)entry.Value;
                        unproj = Unproj(tool.Position);
                        direction = matrix.Rotation * unproj;
                        if (Raycast(matrix.Translation, direction, out hitInfo))
                        {
                            GameObject obj = ObjectFactory.NewTool(AnnotationsTransform, tool.ToolType, Quaternion.LookRotation(TopDownCamera.transform.up, hitInfo.normal), hitInfo.point, ToolScale);
                            if (AnnotationAnchor)
                            {
                                obj.AddComponent<WorldAnchor>();
                            }
                        }
                        break;
                    case Annotation.AnnotationType.POLYLINE:
                        PolylineAnnotation polyline = (PolylineAnnotation)entry.Value;
                        List<Vector3> positions = new List<Vector3>();
                        foreach (Vector2 p in polyline.Positions)
                        {
                            unproj = Unproj(p);
                            direction = matrix.Rotation * unproj;
                            if (Raycast(matrix.Translation, direction, out hitInfo))
                            {
                                positions.Add(hitInfo.point);
                            }
                        }
                        if (positions.Count > 0)
                        {
                            LineRenderer line = ObjectFactory.NewPolyline(AnnotationsTransform, positions, PolylineColor, PolylineWidth);
                            if (AnnotationAnchor)
                            {
                                line.gameObject.AddComponent<WorldAnchor>();
                            }
                        }
                        break;
                }
            }
        }

        protected Vector3 Unproj(Vector2 p)
        {
            return TopDownCamera.Unproj(p, SE3Camera.DoUndistort.YES, false, true);
        }

        protected bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo)
        {
            bool result = Physics.Raycast(origin, direction, out hitInfo, 300.0f, SpatialMappingManager.Instance.LayerMask);
            hitInfo.point += direction * ToolOffset;
            if (ShowAnnotationRays)
            {
                ObjectFactory.NewRay(RaysTransform, TopDownCamera.LocalToWorldMatrix.Translation, hitInfo.point, Color.white);

                // try intersect with checkerplane
                Ray ray = new Ray(origin, direction);
                float depth;
                Checkerboard.Plane.Raycast(ray, out depth);
                ObjectFactory.NewRay(RaysTransform, hitInfo.point, origin + depth * direction, (depth < hitInfo.distance + ToolOffset) ? Color.red : Color.blue, 0.005f);
            }
            return result;
        }

        #endregion

        public override string ToString()
        {
            string result = "";
            if (_Annotations != null)
            {
                foreach (KeyValuePair<int, Annotation> entry in _Annotations)
                {
                    result += entry.ToString() + "\n";
                }
            }
            return result;
        }
    }
}