using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using LCY;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.WebCam;
#if NETFX_CORE
using HoloPoseClient.Signalling;
#endif

namespace STAR
{
    public class HololensCamera : SE3Camera
    {
        [DllImport("HoloOpenCVHelper")]
        protected static extern void initChessPoseController();
        [DllImport("HoloOpenCVHelper")]
        protected static extern void destroyChessPoseController();
        [DllImport("HoloOpenCVHelper")]
        protected static extern void newImage(IntPtr imageData);
        [DllImport("HoloOpenCVHelper")]
        protected static extern void setImageSize(int row, int col);
        [DllImport("HoloOpenCVHelper")]
        protected static extern void detect();
        [DllImport("HoloOpenCVHelper")]
        protected static extern int getNumMarkers();
        [DllImport("HoloOpenCVHelper")]
        protected static extern int getSize();
        [DllImport("HoloOpenCVHelper")]
        protected static extern int getRows();
        [DllImport("HoloOpenCVHelper")]
        protected static extern int getCols();
        [DllImport("HoloOpenCVHelper")]
        protected static extern int getInt();
        [DllImport("HoloOpenCVHelper")]
        protected static extern bool findExtrinsics(int chessX, int chessY, float chessSquareMeters, double cam_mtx_fx, double cam_mtx_fy,
            double cam_mtx_cx, double cam_mtx_cy, double dist_k1, double dist_k2, double dist_p1, double dist_p2, double dist_k3);
        [DllImport("HoloOpenCVHelper")]
        protected static extern float getCheckerPoints(int index, int axis);
        [DllImport("HoloOpenCVHelper")]
        protected static extern IntPtr getProcessedImage();
        [DllImport("HoloOpenCVHelper")]
        protected static extern double GetRvec0();
        [DllImport("HoloOpenCVHelper")]
        protected static extern double GetRvec1();
        [DllImport("HoloOpenCVHelper")]
        protected static extern double GetRvec2();
        [DllImport("HoloOpenCVHelper")]
        protected static extern double GetTvec0();
        [DllImport("HoloOpenCVHelper")]
        protected static extern double GetTvec1();
        [DllImport("HoloOpenCVHelper")]
        protected static extern double GetTvec2();

        public Camera Camera;
        public Room Room;
        protected Transform HololensCameraRays;

        protected Matrix4x4 CameraToWorldMatrix;
        protected Matrix4x4 ProjectionMatrix;

        HololensCamera()
        {
            Width = 1344;
            Height = 756;
            Fx = 1037.806f;
            Fy = 1035.896f;
            Cx = 659.0923f;
            Cy = 373.4973f;
            IntrinsicValid = true;
        }

        private void Start()
        {
            HololensCameraRays = transform.Find("Rays");

            Configurations.Instance.SetAndAddCallback("Visual_HololensCamera", false, v => gameObject.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("Stabilization_Init", () =>
            {
                // prepare initialization message
                JObject message = new JObject();
                message["type"] = "I";
                message["camera"] = new JObject();
                message["camera"]["width"] = Width;
                message["camera"]["height"] = Height;
                message["camera"]["fx"] = Fx;
                message["camera"]["fy"] = Fy;
                message["camera"]["cx"] = Cx;
                message["camera"]["cy"] = Cy;

                Matrix4x4 p = Room.transform.localToWorldMatrix;
                message["plane"] = new JArray(new float[]
                {
                    p.m00, p.m01, p.m02, p.m03,
                    p.m10, p.m11, p.m12, p.m13,
                    p.m20, p.m21, p.m22, p.m23,
                    p.m30, p.m31, p.m32, p.m33
                });
                Matrix4x4 c = Camera.transform.localToWorldMatrix;
                message["cameraMatrix"] = new JArray(new float[]
                {
                    c.m00, c.m01, c.m02, c.m03,
                    c.m10, c.m11, c.m12, c.m13,
                    c.m20, c.m21, c.m22, c.m23,
                    c.m30, c.m31, c.m32, c.m33
                });

                JObject container = new JObject();
                container["message"] = message;
                string jsonString = container.ToString();

#if NETFX_CORE
                Conductor.Instance.SendMessage(Windows.Data.Json.JsonObject.Parse(jsonString));
#endif

                // local visualization
                // camera
                localToWorldMatrix = c;
                Vector3 origin = localToWorldMatrix.Translation;
                Quaternion rotation = localToWorldMatrix.Rotation;

                // rays
                LCY.Utilities.DestroyChildren(HololensCameraRays);
                Vector2[] corners = new Vector2[4]
                {
                    Vector2.zero,
                    new Vector2(0.0f, 1.0f),
                    Vector2.one,
                    new Vector2(1.0f, 0.0f)
                };
                foreach (Vector2 v in corners)
                {
                    Vector3 dir = rotation * Unproj(v, DoUndistort.NO);
                    ObjectFactory.NewRay(HololensCameraRays, origin, origin + 10.0f * dir);
                }

                ExtrinsicValid = true;
            }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("Stabilization_Update", () =>
            {
                // prepare initialization message
                JObject message = new JObject();
                message["type"] = "U";

                Matrix4x4 c = Camera.transform.localToWorldMatrix;
                message["cameraMatrix"] = new JArray(new float[]
                {
                    c.m00, c.m01, c.m02, c.m03,
                    c.m10, c.m11, c.m12, c.m13,
                    c.m20, c.m21, c.m22, c.m23,
                    c.m30, c.m31, c.m32, c.m33
                });

                JObject container = new JObject();
                container["message"] = message;
                string jsonString = container.ToString();

#if NETFX_CORE
                Conductor.Instance.SendMessage(Windows.Data.Json.JsonObject.Parse(jsonString));
#endif
                localToWorldMatrix = c;
            }, Configurations.RunOnMainThead.YES);
        }
    }
}