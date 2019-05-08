using HoloLensCameraStream;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LCY
{
    /// <summary>
    /// Universal video capture class, have two ways to start a camera, polling and interrupt.
    /// The first is request new frames, while the second is whenever a new frame arrives, we process that.
    /// </summary>
    public class UVideoCapture
    {
        public event FrameSampleAcquiredCallback FrameSampleAcquired;
        private event OnVideoCaptureResourceCreatedCallback VideoCaptureCreated;
        private OnVideoModeStartedCallback VideoModeStartedCallback;
        private OnVideoModeStoppedCallback VideoModeStoppedCallback;

        private static UVideoCapture instance = new UVideoCapture();
        public static UVideoCapture Instance => instance;

        public CameraParameters Params { get; set; }
        public enum Mode
        {
            POLLING,
            LISTENER,
        };
        private Mode VideoMode { get; set; }
        private static VideoCapture VideoCapture { get; set; }

        private UVideoCapture()
        {
            VideoCapture.CreateAync(OnVideoCaptureInstanceCreated);
        }

        public void SetNativeISpatialCoordinateSystemPtr(IntPtr ptr)
        {
            if (VideoCapture != null)
            {
                VideoCapture.WorldOriginPtr = ptr;
            }
        }

        public void GetVideoCaptureAsync(OnVideoCaptureResourceCreatedCallback onVideoCaptureAvailable)
        {
            if (VideoCapture == null)
            {
                VideoCaptureCreated += onVideoCaptureAvailable;
            }
            else
            {
                onVideoCaptureAvailable?.Invoke(VideoCapture);
            }
        }

        public Resolution GetHighestResolution()
        {
            return VideoCapture?.GetSupportedResolutions().OrderByDescending((r) => r.width * r.height).FirstOrDefault() ?? default(Resolution);
        }

        public Resolution GetLowestResolution()
        {
            return VideoCapture?.GetSupportedResolutions().OrderBy((r) => r.width * r.height).FirstOrDefault() ?? default(Resolution);
        }

        public float GetHighestFrameRate(Resolution forResolution)
        {
            return VideoCapture?.GetSupportedFrameRatesForResolution(forResolution).OrderByDescending(r => r).FirstOrDefault() ?? default(float);
        }

        public float GetLowestFrameRate(Resolution forResolution)
        {
            return VideoCapture?.GetSupportedFrameRatesForResolution(forResolution).OrderBy(r => r).FirstOrDefault() ?? default(float);
        }

        public void StartCamera(Mode mode = Mode.LISTENER, OnVideoModeStartedCallback videoStart = null)
        {
            VideoModeStartedCallback = videoStart;
            VideoMode = mode;
            VideoCapture?.StartVideoModeAsync(Params, OnVideoModeStarted);
        }

        public void StopCamera(OnVideoModeStoppedCallback videoStop = null)
        {
            VideoModeStoppedCallback = videoStop;
            VideoCapture?.StopVideoModeAsync(OnVideoModeStopped);
        }

        private void OnVideoCaptureInstanceCreated(VideoCapture videoCapture)
        {
            if (videoCapture != null)
            {
                VideoCapture = videoCapture;
                VideoCaptureCreated?.Invoke(VideoCapture);
            }
        }

        private void OnVideoModeStarted(VideoCaptureResult result)
        {
            switch (VideoMode)
            {
                case Mode.LISTENER:
                    VideoCapture.FrameSampleAcquired += OnFrameSampleAcquiredListener;
                    VideoModeStartedCallback?.Invoke(result);
                    break;
                case Mode.POLLING:
                    VideoModeStartedCallback?.Invoke(result);
                    VideoCapture.RequestNextFrameSample(OnFrameSampleAcquiredPolling);
                    break;
            }
        }

        private void OnVideoModeStopped(VideoCaptureResult result)
        {
            VideoModeStoppedCallback?.Invoke(result);
        }

        private void OnFrameSampleAcquiredListener(VideoCaptureSample sample)
        {
            FrameSampleAcquired?.Invoke(sample);
        }

        private void OnFrameSampleAcquiredPolling(VideoCaptureSample sample)
        {
            FrameSampleAcquired?.Invoke(sample);
            VideoCapture.RequestNextFrameSample(OnFrameSampleAcquiredPolling);
        }
    }
}
