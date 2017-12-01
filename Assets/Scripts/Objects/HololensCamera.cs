using HoloToolkit.Unity.SpatialMapping;
using LCY;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VR.WSA;
using UnityEngine.VR.WSA.WebCam;

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

        protected bool ShowHololensCamera { get; set; }
        protected int ShowCameraNumber { get; set; }

        public VideoPreview VideoPreview;
        volatile PhotoCapture photoCaptureObject = null;
        Resolution cameraResolution;
        protected byte[] LatestImageBytes;
        protected bool SaveImage { get; set; }

        protected string OutputDirectory { get; } = DateTime.Now.ToString("yyMMddhhmmss");
        protected int Counter { get; set; } = 0;

        //protected List<SE3> checkerToWorldMatrices = new List<SE3>();
        public SE3 CheckerToLocalMatrix
        {
            set
            {
                Checkerboard.LocalToWorldMatrix = LocalToWorldMatrix * value;
            }
        }

        protected Matrix4x4 CameraToWorldMatrix;
        protected Matrix4x4 ProjectionMatrix;

        // TODO(chengyuanlin)
        // where to put? checker or hololens
        public List<Vector2> CheckerPoints
        {
            set
            {
                Vector3 origin = LocalToWorldMatrix.Translation;
                Quaternion rotation = LocalToWorldMatrix.Rotation;
                if (ShowHololensCamera)
                {
                    Instantiate(GizmoPrefab, origin, rotation, HololensCameras);
                }
                foreach (Vector2 p in value)
                {
                    Vector3 direction;
                    Utilities.PixelCoordToWorldCoord(CameraToWorldMatrix, ProjectionMatrix, cameraResolution, new Vector2(p.x, cameraResolution.height - p.y), out direction);
                    RaycastHit hitInfo;
                    Physics.Raycast(origin, direction, out hitInfo, 300.0f, SpatialMappingManager.Instance.LayerMask);
                    if (ShowHololensCamera)
                    {
                        ObjectFactory.NewRay(HololensCameraRays, origin, hitInfo.point);
                    }
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
            PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);

            Configurations.Instance.SetAndAddCallback("ShowHololensCamera", false, v => transform.parent.gameObject.SetActive(ShowHololensCamera = v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("TakePicture", false, v => photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory), Configurations.RunOnMainThead.YES, Configurations.WaitUntilDone.YES);

            GizmoPrefab = Resources.Load<GameObject>("Gizmo");
            CheckerPointPrefab = Resources.Load<GameObject>("CheckerPoint");

            Utilities.CreateFolder(OutputDirectory);
        }

        protected void OnPhotoCaptureCreated(PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;

            cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

            CameraParameters c = new CameraParameters
            {
                hologramOpacity = 0.0f,
                cameraResolutionWidth = cameraResolution.width,
                cameraResolutionHeight = cameraResolution.height,
                pixelFormat = CapturePixelFormat.BGRA32,
            };

            initChessPoseController();
            setImageSize(cameraResolution.height, cameraResolution.width);
            VideoPreview.SetResolution(cameraResolution.width, cameraResolution.height);

            captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
        }

        protected void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
        {
            //photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }

        protected void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            List<byte> imageBufferList = new List<byte>();
            photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);
            LatestImageBytes = imageBufferList.ToArray();
            Utilities.Flip(LatestImageBytes, cameraResolution.width, cameraResolution.height, 0);
            try
            {
                if (TopDownCamera.IntrinsicValid == false)
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
                float halfWidth = cameraResolution.width / 2f;
                float halfHeight = cameraResolution.height / 2f;
                Fx = ProjectionMatrix.GetColumn(0).x * halfWidth;
                Fy = ProjectionMatrix.GetColumn(1).y * halfHeight;
                float offsetX = ProjectionMatrix.GetColumn(2).x;
                float offsetY = ProjectionMatrix.GetColumn(2).y;
                Cx = halfWidth + offsetX * halfWidth;
                Cy = halfHeight + offsetY * halfHeight;
                IntrinsicValid = true;
                bool gotValidPose = findExtrinsics(Checkerboard.X, Checkerboard.Y, Checkerboard.Size, Fx, Fy, Cx, Cy, K1, K2, K3, P1, P2);
                if (gotValidPose)
                {
                    LocalToWorldMatrix = SE3.ConvertLeftHandedMatrix4x4ToSE3(CameraToWorldMatrix);
                    List<Vector2> checker = new List<Vector2>();
                    for (int i = 0; i < Checkerboard.X * Checkerboard.Y; ++i)
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
                Marshal.Copy(imageHandle, LatestImageBytes, 0, LatestImageBytes.Length);
                Marshal.FreeHGlobal(imageHandle);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            VideoPreview.SetBytes(LatestImageBytes, false);
        }

        void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {
            photoCaptureObject.Dispose();
            photoCaptureObject = null;
        }
    }
}