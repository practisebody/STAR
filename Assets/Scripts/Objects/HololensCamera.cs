﻿using HoloToolkit.Unity;
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
    /// <summary>
    /// Hololens camera class
    /// </summary>
    public class HololensCamera : SE3Camera
    {
        public Camera Camera;
        public Room Room;
        protected Transform HololensCameraRays;

        protected Matrix4x4 CameraToWorldMatrix;
        protected Matrix4x4 ProjectionMatrix;

        static public Camera TheCamera;

        HololensCamera()
        {
            TheCamera = Camera;
            
            // intrinsics, pre-calibrated
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
            Configurations.Instance.AddCallback("*_StablizationInit", () =>
            {
                Matrix4x4 c = Camera.transform.localToWorldMatrix;
#if NETFX_CORE
                Matrix4x4 p = Room.transform.localToWorldMatrix;
                // prepare initialization message
                // send plane and cmaera information
                JObject message = new JObject
                {
                    ["type"] = "I",
                    ["camera"] = new JObject
                    {
                        ["width"] = Width,
                        ["height"] = Height,
                        ["fx"] = Fx,
                        ["fy"] = Fy,
                        ["cx"] = Cx,
                        ["cy"] = Cy
                    },
                    ["plane"] = Utilities.Matrix4x42JArray(p),
                    ["cameraMatrix"] = Utilities.Matrix4x42JArray(c)
                };

                JObject container = new JObject
                {
                    ["message"] = message
                };
                string jsonString = container.ToString();
                Conductor.Instance.SendMessage(WebRTCConnection.MentorName, Windows.Data.Json.JsonObject.Parse(jsonString));
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
            Configurations.Instance.AddCallback("Stabilization_DebugUpdate", () =>
            {
                Matrix4x4 c = Camera.transform.localToWorldMatrix;
#if NETFX_CORE
                // send camera location now, for debug only
                JObject message = new JObject
                {
                    ["type"] = "U",
                    ["cameraMatrix"] = Utilities.Matrix4x42JArray(c)
                };

                JObject container = new JObject
                {
                    ["message"] = message
                };
                string jsonString = container.ToString();
                Conductor.Instance.SendMessage(WebRTCConnection.MentorName, Windows.Data.Json.JsonObject.Parse(jsonString));
#endif
                localToWorldMatrix = c;
            }, Configurations.RunOnMainThead.YES);

            Configurations.Instance.AddCallback("*_PrepareUI", () => Configurations.Instance.Set("Visual_HololensCamera", false));

            VideoStart();
        }

        #region video

        protected HoloLensCameraStream.Resolution VideoResolution;
        protected byte[] VideoFrameBuffer;
        protected FileStream VideoFrames;
        protected BinaryWriter VideoBinaryWriter;
        protected string OutputDirectory;

        /// <summary>
        /// To record a first person video, as well as the room geometry
        /// For debugging
        /// [interal use]
        /// </summary>
        protected void VideoStart()
        {
            UVideoCapture.Instance.GetVideoCaptureAsync(OnVideoCreated);
            UVideoCapture.Instance.FrameSampleAcquired += OnFrameCaptured;
            Configurations.Instance.SetAndAddCallback("Utilities_Record", false, v =>
            {
                if (v)
                {
                    OutputDirectory = Utilities.TimeNow();
                    Utilities.CreateFolder(OutputDirectory);
                    VideoFrames = File.Create(Utilities.FullPath(Path.Combine(OutputDirectory, "frames.raw")));
                    VideoBinaryWriter = new BinaryWriter(VideoFrames);
                    UVideoCapture.Instance.StartCamera();
                }
                else
                {
                    UVideoCapture.Instance.StopCamera();
                    VideoBinaryWriter.Dispose();
                    VideoFrames.Dispose();

                    MeshSaver.Save("room", SpatialMappingManager.Instance.GetMeshFilters());
                }
            }, Configurations.RunOnMainThead.YES, Configurations.WaitUntilDone.NO);
        }

        void OnVideoCreated(HoloLensCameraStream.VideoCapture capture)
        {
            UVideoCapture.Instance.SetNativeISpatialCoordinateSystemPtr(UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr());
            VideoResolution = new HoloLensCameraStream.Resolution(1344, 756);
            float framerate = UVideoCapture.Instance.GetHighestFrameRate(VideoResolution);
            UVideoCapture.Instance.Params = new HoloLensCameraStream.CameraParameters
            {
                pixelFormat = HoloLensCameraStream.CapturePixelFormat.BGRA32,
                cameraResolutionWidth = VideoResolution.width,
                cameraResolutionHeight = VideoResolution.height,
                frameRate = Mathf.RoundToInt(framerate),
            };
        }

        void OnFrameCaptured(HoloLensCameraStream.VideoCaptureSample sample)
        {
            if (VideoFrameBuffer == null)
                VideoFrameBuffer = new byte[sample.dataLength];
            float[] matrix = null;
            if (File.Exists(Path.Combine(OutputDirectory, "proj.txt")) == false)
            {
                if (sample.TryGetProjectionMatrix(out matrix) == false)
                {
                    return;
                }
                Matrix4x4 m = LocatableCameraUtils.ConvertFloatArrayToMatrix4x4(matrix);
                Vector4 column2 = m.GetColumn(2);
                column2.x = -column2.x;
                column2.y = -column2.y;
                m.SetColumn(2, column2);
                float halfWidth = VideoResolution.width / 2f;
                float halfHeight = VideoResolution.height / 2f;
                float Fx = m.GetColumn(0).x * halfWidth;
                float Fy = m.GetColumn(1).y * halfHeight;
                float offsetX = m.GetColumn(2).x;
                float offsetY = m.GetColumn(2).y;
                float Cx = halfWidth + offsetX * halfWidth;
                float Cy = halfHeight + offsetY * halfHeight;
                Utilities.SaveFile(string.Format("{0} {1}\n{2} {3} {4} {5}", VideoResolution.width, VideoResolution.height, Fx, Fy, Cx, Cy),
                    Path.Combine(OutputDirectory, "proj.txt"));
            }
            if (sample.TryGetCameraToWorldMatrix(out matrix) == false)
            {
                return;
            }
            sample.CopyRawImageDataIntoBuffer(VideoFrameBuffer);
            for (int i = 0; i < 16; ++i)
                VideoBinaryWriter.Write(matrix[i]);
            VideoBinaryWriter.Write(VideoFrameBuffer);
        }

        #endregion
    }
}
