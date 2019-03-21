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

        static public Camera TheCamera;

        HololensCamera()
        {
            TheCamera = Camera;
            
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
                    }
                };
                message["plane"] = new JArray(new float[]
                {
                    p.m00, p.m01, p.m02, p.m03,
                    p.m10, p.m11, p.m12, p.m13,
                    p.m20, p.m21, p.m22, p.m23,
                    p.m30, p.m31, p.m32, p.m33
                });
                message["cameraMatrix"] = new JArray(new float[]
                {
                    c.m00, c.m01, c.m02, c.m03,
                    c.m10, c.m11, c.m12, c.m13,
                    c.m20, c.m21, c.m22, c.m23,
                    c.m30, c.m31, c.m32, c.m33
                });
                JObject container = new JObject
                {
                    ["message"] = message
                };
                string jsonString = container.ToString();
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
            Configurations.Instance.AddCallback("Stabilization_DebugUpdate", () =>
            {
                Matrix4x4 c = Camera.transform.localToWorldMatrix;
#if NETFX_CORE
                // prepare initialization message
                JObject message = new JObject
                {
                    ["type"] = "U",
                    ["cameraMatrix"] = new JArray(new float[]
                {
                    c.m00, c.m01, c.m02, c.m03,
                    c.m10, c.m11, c.m12, c.m13,
                    c.m20, c.m21, c.m22, c.m23,
                    c.m30, c.m31, c.m32, c.m33
                })
                };

                JObject container = new JObject
                {
                    ["message"] = message
                };
                string jsonString = container.ToString();
                Conductor.Instance.SendMessage(Windows.Data.Json.JsonObject.Parse(jsonString));
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
