using HoloToolkit.Unity.SpatialMapping;
using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    /// <summary>
    /// The class to control the room geoemtry, stabilization plane
    /// </summary>
    public class Room : MonoBehaviour
    {
        protected GameObject _Room;
        protected Plane Plane;

        protected float PlaneX = 0.0f, PlaneY = -0.5f, PlaneZ = 0.4f;
        protected float PlaneDetectOffset = 0.2f;

        private void Start()
        {
            transform.position = new Vector3(PlaneX, PlaneY, PlaneZ);
            transform.rotation = new Quaternion(1.0f, 0.0f, 0.0f, 1.0f);
            _Room = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _Room.transform.parent = transform;
            _Room.transform.localPosition = Vector3.zero;
            _Room.transform.localRotation = Quaternion.identity;
            Plane = new Plane(Vector3.up, transform.position);

            Configurations.Instance.SetAndAddCallback("Stabilization_ShowPlane", true, v => gameObject.SetActive(v), Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Stabilization_PlaneX", PlaneX,
                v => transform.position = new Vector3(PlaneX = v, PlaneY, PlaneZ), Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Stabilization_PlaneY", PlaneY,
                v => transform.position = new Vector3(PlaneX, PlaneY = v, PlaneZ), Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Stabilization_PlaneZ", PlaneZ,
                v => transform.position = new Vector3(PlaneX, PlaneY, PlaneZ = v), Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("Stabilization_Detect", SurfaceMeshesToPlanes.Instance.MakePlanes, Configurations.RunOnMainThead.YES);
            SurfaceMeshesToPlanes.Instance.MakePlanesComplete += SurfaceMeshesToPlanes_MakePlanesComplete;
            Configurations.Instance.SetAndAddCallback("Stabilization_DetectOffset", PlaneDetectOffset, v =>
            {
                PlaneDetectOffset = v;
                Configurations.Instance.Set("Stabilization_PlaneY", SurfaceMeshesToPlanes.Instance.FloorYPosition + PlaneDetectOffset);
                ControllerManager.Instance.SendControl();
            }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("*_PrepareUI", () => Configurations.Instance.Set("Stabilization_ShowPlane", false));
        }

        /// <summary>
        /// Automatic detect the floor plane
        /// </summary>
        private void SurfaceMeshesToPlanes_MakePlanesComplete(object source, System.EventArgs args)
        {
            LCY.Utilities.InvokeMain(() =>
            {
                Configurations.Instance.Set("Stabilization_PlaneY", SurfaceMeshesToPlanes.Instance.FloorYPosition + PlaneDetectOffset);
                ControllerManager.Instance.SendControl();
            }, false);
        }

        /// <summary>
        /// Raycast to intersect with the stablization plane
        /// </summary>
        public bool Raycast(Vector3 origin, Vector3 dir, out Vector3 hitPoint)
        {
            float enter;
            Ray r = new Ray(origin, dir);
            bool result = Plane.Raycast(r, out enter);
            hitPoint = r.GetPoint(enter);
            return result;
        }
    }
}