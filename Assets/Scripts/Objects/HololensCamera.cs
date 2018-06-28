using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using LCY;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.WebCam;

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
        protected static extern bool findExtrinsics(
                int chessX, int chessY, float chessSquareMeters,
                double cam_mtx_fx, double cam_mtx_fy, double cam_mtx_cx, double cam_mtx_cy,
                double dist_k1, double dist_k2, double dist_p1, double dist_p2, double dist_k3);
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

        public TopDownCamera TopDownCamera;
        public Checkerboard Checkerboard;
        public Transform HololensCameras;
        public Transform HololensCameraRays;
        public Transform CheckerPointsObject;

        protected GameObject GizmoPrefab;
        protected GameObject CheckerPointPrefab;

        public SE3 CheckerToLocalMatrix
        {
            set
            {
                Checkerboard.localToWorldMatrix = localToWorldMatrix * value;
            }
        }

        protected Matrix4x4 CameraToWorldMatrix;
        protected Matrix4x4 ProjectionMatrix;

        public List<Vector2> CheckerPoints
        {
            set
            {
                Vector3 origin = localToWorldMatrix.Translation;
                Quaternion rotation = localToWorldMatrix.Rotation;
                Instantiate(GizmoPrefab, origin, rotation, HololensCameras);
                foreach (Vector2 p in value)
                {
                    Vector3 direction;
                    Utilities.PixelCoordToWorldCoord(CameraToWorldMatrix, ProjectionMatrix, CameraResolution, new Vector2(p.x, CameraResolution.height - p.y), out direction);
                    RaycastHit hitInfo;
                    Physics.Raycast(origin, direction, out hitInfo, 300.0f, SpatialMappingManager.Instance.LayerMask);
                    ObjectFactory.NewRay(HololensCameraRays, origin, hitInfo.point);
                }
            }
        }

        HololensCamera()
        {
            K1 = 0.0f;
            K2 = 0.0f;
            P1 = 0.0f;
            P2 = 0.0f;
            K3 = 0.0f;
        }

        private void Start()
        {
            GizmoPrefab = Resources.Load<GameObject>("Gizmo");
            CheckerPointPrefab = Resources.Load<GameObject>("CheckerPoint");

            //PhotoStart();
            //VideoStart();
        }

        #region photo

        protected PhotoCapture PhotoCaptureObject = null;
        public VideoPreview VideoPreview;
        protected CameraParameters Parameters;
        protected Resolution CameraResolution;
        protected byte[] LatestImageBytes;

        protected void PhotoStart()
        {
            PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);

            Configurations.Instance.SetAndAddCallback("Visual_HololensCamera", false, v => transform.parent.gameObject.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("TakePicture", () => PhotoCaptureObject.StartPhotoModeAsync(Parameters, OnPhotoModeStarted), Configurations.RunOnMainThead.YES, Configurations.WaitUntilDone.NO);
        }

        protected void OnPhotoCaptureCreated(PhotoCapture captureObject)
        {
            PhotoCaptureObject = captureObject;

            CameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

            Parameters = new CameraParameters
            {
                hologramOpacity = 0.0f,
                cameraResolutionWidth = CameraResolution.width,
                cameraResolutionHeight = CameraResolution.height,
                pixelFormat = CapturePixelFormat.BGRA32,
            };

            Debug.Log("Res:" + CameraResolution.width + "," + CameraResolution.height);

            initChessPoseController();
            setImageSize(CameraResolution.height, CameraResolution.width);
            VideoPreview.SetResolution(CameraResolution.width, CameraResolution.height);
        }

        protected void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
        {
            PhotoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }

        protected void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            List<byte> imageBufferList = new List<byte>();
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);
            LatestImageBytes = imageBufferList.ToArray();
            Utilities.Flip(LatestImageBytes, CameraResolution.width, CameraResolution.height, 0);
            try
            {
                if (Checkerboard.valid == false)
                {
                    throw new Exception();
                }
                if (photoCaptureFrame.TryGetCameraToWorldMatrix(out CameraToWorldMatrix) == false)
                {
                    throw new Exception();
                }
                if (photoCaptureFrame.TryGetProjectionMatrix(out ProjectionMatrix) == false)
                {
                    throw new Exception();
                }
                Vector4 column2 = ProjectionMatrix.GetColumn(2);
                column2.x = -column2.x;
                column2.y = -column2.y;
                ProjectionMatrix.SetColumn(2, column2);
                IntPtr imageHandle = Marshal.AllocHGlobal(LatestImageBytes.Length);
                Marshal.Copy(LatestImageBytes, 0, imageHandle, LatestImageBytes.Length);
                newImage(imageHandle);
                float halfWidth = CameraResolution.width / 2f;
                float halfHeight = CameraResolution.height / 2f;
                Fx = ProjectionMatrix.GetColumn(0).x * halfWidth;
                Fy = ProjectionMatrix.GetColumn(1).y * halfHeight;
                float offsetX = ProjectionMatrix.GetColumn(2).x;
                float offsetY = ProjectionMatrix.GetColumn(2).y;
                Cx = halfWidth + offsetX * halfWidth;
                Cy = halfHeight + offsetY * halfHeight;
                IntrinsicValid = true;
                bool gotValidPose = findExtrinsics(Checkerboard.x, Checkerboard.y, Checkerboard.size, Fx, Fy, Cx, Cy, K1, K2, K3, P1, P2);
                if (gotValidPose)
                {
                    localToWorldMatrix = SE3.ConvertLeftHandedMatrix4x4ToSE3(CameraToWorldMatrix);
                    List<Vector2> checker = new List<Vector2>();
                    for (int i = 0; i < Checkerboard.x * Checkerboard.y; ++i)
                    {
                        float x = getCheckerPoints(i, 0);
                        float y = getCheckerPoints(i, 1);
                        checker.Add(new Vector2(x, y));
                    }
                    CheckerPoints = checker;

                    // TODO(chengyuanlin)
                    // abs(Quaternion.z) < 0.35 and T.z < 0.6 is good pose.
                    Quaternion R_holo = LCY.Utilities.Rodrigues2Quaternion(new Vector3((float)GetRvec0(), (float)GetRvec1(), (float)GetRvec2()));
                    Vector3 t_holo = new Vector3((float)GetTvec0(), (float)GetTvec1(), (float)GetTvec2());
                    CheckerToLocalMatrix = Matrix4x4.TRS(t_holo, R_holo, new Vector3(1.0f, 1.0f, 1.0f));
                }

                // Fetch the processed image and render
                imageHandle = getProcessedImage();
                Marshal.FreeHGlobal(imageHandle);
            }
            catch (Exception)
            {
            }
            VideoPreview.SetBytes(LatestImageBytes, false);
            PhotoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }

        void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {
        }

        #endregion

        #region video

        protected HoloLensCameraStream.Resolution VideoResolution;
        protected byte[] VideoFrameBuffer;
        protected FileStream VideoFrames;
        protected BinaryWriter VideoBinaryWriter;
        protected string OutputDirectory { get; } = DateTime.Now.ToString("yyMMddHHmmss");

        protected void VideoStart()
        {
            Utilities.CreateFolder(OutputDirectory);

            UVideoCapture.Instance.GetVideoCaptureAsync(OnVideoCreated);
            UVideoCapture.Instance.FrameSampleAcquired += OnFrameCaptured;
            Configurations.Instance.SetAndAddCallback("Utilities_Record", false, v =>
            {
                if (v)
                {
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
            if (!File.Exists(Path.Combine(OutputDirectory, "proj.txt")))
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