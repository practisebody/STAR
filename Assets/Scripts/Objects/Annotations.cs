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
        public SE3Camera Camera;
        public Room Room;

        protected Transform AnnotationsTransform;
        protected Transform Rays;
        protected Transform Cameras;

        // annotation parameters
        protected bool OnPlane { get; set; } = true;

        protected float ToolScale { get; set; } = 1.0f;
        protected float ToolOffset { get; set; } = 0.02f;
        protected float PolylineWidth { get; set; } = 0.002f;
        protected float PolylineBrightnessMultiplier { get; set; } = 1.0f;
        protected bool AnnotationAnchor { get; set; } = true;
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
            Rays = transform.Find("Rays");
            Cameras = transform.Find("Cameras");

            TryAttach(SpatialMappingManager.Instance.Source);
            SpatialMappingManager.Instance.SourceChanged += OnSourceChanged;

            Configurations.Instance.SetAndAddCallback("Annotation_OnPlane", OnPlane,
                v => { OnPlane = v; Refresh(); }, Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);

            Configurations.Instance.SetAndAddCallback("Visual_AnnotationRays", false,
                v => Rays.gameObject.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Visual_AnnotationCameraRays", false,
                v => Cameras.gameObject.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("AnnotationTool_Scale", ToolScale,
                v => { ToolScale = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("AnnotationTool_Offset", ToolOffset,
                v => { ToolOffset = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("AnnotationPolyline_LineWidth", PolylineWidth,
                v => { PolylineWidth = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("AnnotationPolyline_BrightnessMultiplier", 1.0f,
                v => { PolylineColor = v * DefaultPolylineColor; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_Anchor", AnnotationAnchor,
                v => { AnnotationAnchor = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_XOffset", AnnotationXOffset,
                v => { AnnotationXOffset = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_YOffset", AnnotationYOffset,
                v => { AnnotationYOffset = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Annotation_ZOffset", AnnotationZOffset,
                v => { AnnotationZOffset = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("Annotation_RemoveAll",
                () => { _Annotations.Clear(); Refresh(); }, Configurations.RunOnMainThead.YES);

            Configurations.Instance.AddCallback("*_PrepareUI", () =>
            {
                Configurations.Instance.Set("Visual_AnnotationRays", false);
                Configurations.Instance.Set("Visual_AnnotationCameraRays", false);
            });
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

        public void Add(int id, Annotation a)
        {
            _Annotations[id] = a;
            Refresh();
        }

        public void Remove(int id)
        {
            _Annotations.Remove(id);
            Refresh();
        }

        #region annotation

        public void Refresh()
        {
            LCY.Utilities.DestroyChildren(AnnotationsTransform);
            LCY.Utilities.DestroyChildren(Rays);
            LCY.Utilities.DestroyChildren(Cameras);
            RaycastHit hitInfo;
            SE3 matrix = Camera.localToWorldMatrix;
            foreach (KeyValuePair<int, Annotation> entry in _Annotations)
            {
                switch (entry.Value.Type)
                {
                    case Annotation.AnnotationType.TOOL:
                        ToolAnnotation tool = (ToolAnnotation)entry.Value;
                        InitAnnotationMatrix(tool.Matrix);
                        if (Raycast(matrix, tool.Position, out hitInfo))
                        {
                            Quaternion localRotation = Quaternion.AngleAxis(tool.Rotation, Vector3.up);
                            GameObject obj = ObjectFactory.NewTool(AnnotationsTransform, tool.ToolType, localRotation, hitInfo.point, ToolScale);
                            if (AnnotationAnchor)
                            {
                                obj.AddComponent<WorldAnchor>();
                            }
                        }
                        break;
                    case Annotation.AnnotationType.POLYLINE:
                        PolylineAnnotation polyline = (PolylineAnnotation)entry.Value;
                        InitAnnotationMatrix(polyline.Matrix);
                        List<Vector3> positions = new List<Vector3>();
                        foreach (Vector2 p in polyline.Positions)
                        {
                            if (Raycast(matrix, p, out hitInfo))
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

        protected SE3 AnnotationMatrix;
        protected Quaternion rotation;
        protected Vector3 translation;

        protected void InitAnnotationMatrix(SE3 matrix)
        {
            AnnotationMatrix = matrix;
            rotation = matrix.Rotation;
            translation = matrix.Translation;
            // (0, 1) (1, 1)
            // (0, 0) (1, 0)
            Vector3 dir00 = rotation * Camera.Unproj(Vector2.zero);
            Vector3 dir01 = rotation * Camera.Unproj(Vector2.up);
            Vector3 dir10 = rotation * Camera.Unproj(Vector2.right);
            Vector3 p00, p01, p10;
            Room.Raycast(translation, dir00, out p00);
            Room.Raycast(translation, dir01, out p01);
            Room.Raycast(translation, dir10, out p10);
            ObjectFactory.NewGizmo(Cameras, translation, rotation);
            ObjectFactory.NewRay(Cameras, translation, translation + 10.0f * dir00, Color.red);
            ObjectFactory.NewRay(Cameras, translation, translation + 10.0f * dir01, Color.green);
            ObjectFactory.NewRay(Cameras, translation, translation + 10.0f * dir10, Color.blue);
        }

        protected bool Raycast(SE3 matrix, Vector2 anno, out RaycastHit hitInfo)
        {
            //Vector3 p;
            //Room.Raycast(matrix.Translation, matrix.Rotation * Camera.Unproj(anno), out p);
            //ObjectFactory.NewRay(Rays, matrix.Translation, p, Color.green);
            //Vector3 projected = Camera.Project(AnnotationMatrix.inverse * p);
            //Vector2 uv = new Vector2(projected.x, projected.y);

            //Vector3 direction = rotation * Camera.Unproj(uv);
            //bool result = Physics.Raycast(translation, direction, out hitInfo, 300.0f, SpatialMappingManager.Instance.LayerMask);
            Vector3 direction = matrix.Rotation * Camera.Unproj(anno);
            bool result;
            if (OnPlane)
            {
                Vector3 p;
                result = Room.Raycast(matrix.Translation, matrix.Rotation * Camera.Unproj(anno), out p);
                hitInfo = new RaycastHit();
                hitInfo.point = p;
            }
            else
                result = Physics.Raycast(matrix.Translation, direction, out hitInfo, 300.0f, SpatialMappingManager.Instance.LayerMask);
            //hitInfo.distance += ToolOffset;
            hitInfo.point += ToolOffset * direction;
            hitInfo.point += AnnotationXOffset * Vector3.right;
            hitInfo.point += AnnotationYOffset * Vector3.forward;
            hitInfo.point += AnnotationZOffset * Vector3.up;
            //ObjectFactory.NewRay(Rays, translation, hitInfo.point, Color.white, 0.005f);
            ObjectFactory.NewRay(Rays, matrix.Translation, hitInfo.point, Color.white, 0.005f);
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