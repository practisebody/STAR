using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using LCY;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.XR.WSA;
using WSAUnity;
#if NETFX_CORE
using Newtonsoft.Json.Linq;
#endif

namespace STAR
{
    public class Annotations : MonoBehaviour
    {
        public TopDownCamera TopDownCamera;
        public HololensCamera HololensCamera;
        public Checkerboard Checkerboard;
        protected Transform AnnotationsTransform;
        protected Transform RaysTransform;
        protected bool ShowAnnotationRays { get; set; } = false;
        protected bool ShowAnnotationRaysExtra { get; set; } = false;
        protected float ToolScale { get; set; } = 1.0f;
        protected float ToolOffset { get; set; } = 0.02f;
        protected float PolylineWidth { get; set; } = 0.002f;
        protected float PolylineBrightnessMultiplier { get; set; } = 1.0f;
        protected bool AnnotationAnchor { get; set; } = true;
        protected bool AnnotationFixOutlier { get; set; } = true;
        protected float AnnotationOutlierThreshold { get; set; } = 0.4f;
        static protected readonly Color DefaultPolylineColor = new Color(0.498f, 1.0f, 0.831f);
        protected Color PolylineColor { get; set; } = DefaultPolylineColor;
        protected float AnnotationXOffset { get; set; } = 0.0f;
        protected float AnnotationYOffset { get; set; } = 0.0f;
        protected float AnnotationZOffset { get; set; } = 0.0f;

        protected SortedDictionary<int, Annotation> _Annotations { get; } = new SortedDictionary<int, Annotation>();

        private void Start()
        {
            AnnotationsTransform = transform.Find("Annotations");
            RaysTransform = transform.Find("Rays");
            TryAttach(SpatialMappingManager.Instance.Source);
            SpatialMappingManager.Instance.SourceChanged += OnSourceChanged;
            Configurations.Instance.SetAndAddCallback("Visual_AnnotationRays", ShowAnnotationRays, v => RaysTransform.gameObject.SetActive(ShowAnnotationRays = v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Visual_AnnotationRaysExtra", ShowAnnotationRaysExtra, v => { ShowAnnotationRaysExtra = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("AnnotationTool_Scale", ToolScale, v => { ToolScale = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("AnnotationTool_Offset", ToolOffset, v => { ToolOffset = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("AnnotationPolyline_LineWidth", PolylineWidth, v => { PolylineWidth = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("AnnotationPolyline_BrightnessMultiplier", 1.0f, v => { PolylineColor = v * DefaultPolylineColor; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_Anchor", AnnotationAnchor, v => { AnnotationAnchor = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_FixOutlier", AnnotationFixOutlier, v => { AnnotationFixOutlier = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_OutlierThreshold", AnnotationOutlierThreshold, v => { AnnotationFixOutlier = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_XOffset", AnnotationXOffset, v => { AnnotationXOffset = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_YOffset", AnnotationYOffset, v => { AnnotationYOffset = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_ZOffset", AnnotationZOffset, v => { AnnotationZOffset = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_RemoveAll", true, v =>
            {
                _Annotations.Clear();
                Refresh();
            }, Configurations.RunOnMainThead.YES);

            // test helper
            /*Configurations.Instance.SetAndAddCallback("Annotation_DummyDiagonal", 0, v =>
            {
                if (TopDownCamera.CheckerPoints?.Count != 0)
                {
                    Vector2 first = TopDownCamera.CheckerPoints[0];
                    Vector2 last = TopDownCamera.CheckerPoints[TopDownCamera.CheckerPoints.Count - 1];
                    StringBuilder sb = new StringBuilder(200);
                    sb.Append("{\"id\":").Append(v).Append(",\"command\":\"CreateAnnotationCommand\",\"annotation_memory\":{\"annotation\":{\"annotationPoints\":[");
                    sb.Append("{\"x\":").Append(first.x / TopDownCamera.Width).Append(",\"y\":").Append(1.0 - first.y / TopDownCamera.Height).Append("},");
                    sb.Append("{\"x\":").Append(last.x / TopDownCamera.Width).Append(",\"y\":").Append(1.0 - last.y / TopDownCamera.Height).Append("}");
                    sb.Append("],\"annotationType\":\"polyline\"}}}");
                    AnnotationReceived(sb.ToString());
                }
            }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_DummyDense", 300, v =>
            {
                float fv = (float)v;
                StringBuilder sb = new StringBuilder(200 + v * 20);
                sb.Append("{\"id\":0,\"command\":\"CreateAnnotationCommand\",\"annotation_memory\":{\"annotation\":{\"annotationPoints\":[");
                sb.Append("{\"x\":0.0,\"y\":0.0}");
                for (int i = 1; i < v; ++i)
                {
                    sb.Append(",{\"x\":").Append(i / fv).Append(",\"y\":").Append(i / fv).Append("}");
                }
                sb.Append("],\"annotationType\":\"polyline\"}}}");
                AnnotationReceived(sb.ToString());
            }, Configurations.RunOnMainThead.NO);*/
        }

        #region spatial mapping

        protected void OnSourceChanged(object sender, PropertyChangedEventArgsEx<SpatialMappingSource> e)
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
        
        #endregion

        public void Clear()
        {
            _Annotations.Clear();
        }

        public void Add(int id, Annotation a)
        {
            _Annotations.Add(id, a);
        }

        public void Remove(int id)
        {
            _Annotations.Remove(id);
        }
        
        public void Refresh()
        {
            if (TopDownCamera.valid == false)
                return;
            try
            {
                LCY.Utilities.DestroyChildren(AnnotationsTransform);
                LCY.Utilities.DestroyChildren(RaysTransform);
                RaycastHit hitInfo;
                Vector3 unproj, direction;
                SE3 matrix = TopDownCamera.localToWorldMatrix;
                Quaternion rotation = WorldManager.Instance.rotation;
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
                                Quaternion localRotation = Quaternion.AngleAxis(tool.Rotation, Vector3.up);
                                GameObject obj = ObjectFactory.NewTool(AnnotationsTransform, tool.ToolType, rotation * localRotation, hitInfo.point, ToolScale);
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
            catch (Exception e)
            {
                Debug.Log(e);
                Debug.LogException(e);
            }
        }

        protected Vector3 Unproj(Vector2 p)
        {
            return HololensCamera.Unproj(p, SE3Camera.DoUndistort.YES, false, true);
            //return TopDownCamera.Unproj(p, SE3Camera.DoUndistort.YES, false, true);
        }

        protected bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo)
        {
            bool result = Physics.Raycast(origin, direction, out hitInfo, 300.0f, SpatialMappingManager.Instance.LayerMask);
            hitInfo.distance += ToolOffset;
            hitInfo.point += ToolOffset * direction;
            hitInfo.point += AnnotationXOffset * WorldManager.Instance.Right;
            hitInfo.point += AnnotationYOffset * WorldManager.Instance.Forward;
            hitInfo.point += AnnotationZOffset * WorldManager.Instance.Up;

            // try intersect with checker plane
            Ray ray = new Ray(origin, direction);
            float depth = 0.0f;
            Checkerboard.Plane.Raycast(ray, out depth);
            // simple test for outliers
            if (AnnotationFixOutlier)
            {
                if (Math.Abs(hitInfo.distance - depth) > AnnotationOutlierThreshold)
                {
                    return false;
                }
            }
            // Annotation Rays
            if (ShowAnnotationRaysExtra)
            {
                ObjectFactory.NewRay(RaysTransform, TopDownCamera.localToWorldMatrix.Translation, hitInfo.point, Color.white);
                ObjectFactory.NewRay(RaysTransform, hitInfo.point, origin + depth * direction, depth < hitInfo.distance ? Color.red : Color.blue, 0.005f);
            }
            else
            {
                ObjectFactory.NewRay(RaysTransform, TopDownCamera.localToWorldMatrix.Translation, hitInfo.point, Color.white, 0.005f);
            }

            return result;
        }

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