using LCY;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
#if NETFX_CORE
using Windows.Networking;
using Windows.Networking.Connectivity;
#endif
#if NETFX_CORE
using Windows.Storage;
#endif

namespace STAR
{
    static public class Utilities
    {
        // TODO(chengyuanlin)
        // move to LCY?

        public static string GetIPAddress()
        {
#if NETFX_CORE
            ConnectionProfile icp = NetworkInformation.GetInternetConnectionProfile();
            HostName hostname = NetworkInformation.GetHostNames().SingleOrDefault(hn =>
                    hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                    == icp.NetworkAdapter.NetworkAdapterId);
            return hostname?.CanonicalName;
#else
            return string.Empty;
#endif
        }
        // Unity
        public static string FolderName
        {
            get
            {
#if !UNITY_EDITOR && UNITY_WSA
                return ApplicationData.Current.RoamingFolder.Path;
#else
                return Application.persistentDataPath;
#endif
            }
        }

        public static string FullPath(string name)
        {
            return Path.Combine(FolderName, name);
        }

        public static void CreateFolder(string name)
        {
            string fullname = Path.Combine(FolderName, name);
            if (Directory.Exists(fullname) == false)
            {
                Directory.CreateDirectory(fullname);
            }
        }

        public static void SaveFile(byte[] bytes, string name)
        {
            string fullname = Path.Combine(FolderName, name);
            File.WriteAllBytes(fullname, bytes);
        }

        public static void SaveFile(string content, string name)
        {
            string fullname = Path.Combine(FolderName, name);
            File.WriteAllText(fullname, content);
        }

        // JSONNode
        public static Vector3 JSON2Vector3(JSONNode node)
        {
            return new Vector3(node[0].AsFloat, node[1].AsFloat, node[2].AsFloat);
        }

        // array
        public static void Flip(byte[] array, int width, int height, int mode, int channel = 4)
        {
            int rowStart, a, b;
            int stride = width * channel;
            byte temp;
            for (int y = 0; y < height / 2; ++y)
            {
                rowStart = y * stride;
                for (int x = 0; x < width; ++x)
                {
                    a = rowStart + x * channel;
                    b = (height - y - 1) * stride + x * channel;
                    for (int i = 0; i < channel; ++i)
                    {
                        temp = array[a + i];
                        array[a + i] = array[b + i];
                        array[b + i] = temp;
                    }
                }
            }
        }

        // Log
        static public string FloatFormat { get; } = "F5";

        static public string FormatQuaternion(Quaternion? v)
        {
            return "(" + v?.w.ToString(FloatFormat) + "," + v?.x.ToString(FloatFormat) + "," + v?.y.ToString(FloatFormat) + "," + v?.z.ToString(FloatFormat) + ")";
        }

        static public string FormatVector3(Vector3? v)
        {
            return "(" + v?.x.ToString(FloatFormat) + "," + v?.y.ToString(FloatFormat) + "," + v?.z.ToString(FloatFormat) + ")";
        }

        static public string FormatMatrix4x4(SE3 m)
        {
            return "R:" + FormatQuaternion(m?.Rotation) + " t:" + FormatVector3(m?.Translation);
        }

        static public void LogVector3(Vector3 v, string name = "")
        {
            Debug.Log(name + " " + FormatVector3(v));
        }

        static public void LogMatrix(Matrix4x4 m, string name = "")
        {
            string first = name + ":" + m.m00.ToString(FloatFormat) + " " + m.m01.ToString(FloatFormat) + " " + m.m02.ToString(FloatFormat) + " " + m.m03.ToString(FloatFormat);
            Debug.Log(first);
            Debug.Log((m.m10.ToString(FloatFormat) + " " + m.m11.ToString(FloatFormat) + " " + m.m12.ToString(FloatFormat) + " " + m.m13.ToString(FloatFormat)).PadLeft(first.Length));
            Debug.Log((m.m20.ToString(FloatFormat) + " " + m.m21.ToString(FloatFormat) + " " + m.m22.ToString(FloatFormat) + " " + m.m23.ToString(FloatFormat)).PadLeft(first.Length));
            Debug.Log((m.m30.ToString(FloatFormat) + " " + m.m31.ToString(FloatFormat) + " " + m.m32.ToString(FloatFormat) + " " + m.m33.ToString(FloatFormat)).PadLeft(first.Length));
        }

        static public void LogException(Exception e)
        {
            Debug.Log(e.Source + " " + e.Message);
            Debug.Log(e.StackTrace);
        }

        public static void PixelCoordToWorldCoord(Matrix4x4 cameraToWorldMatrix, Matrix4x4 projectionMatrix, UnityEngine.Resolution cameraResolution, Vector2 pixelCoordinates, out Vector3 direction)
        {
            pixelCoordinates = LocatableCameraUtils.ConvertPixelCoordsToScaledCoords(new Vector2(cameraResolution.width - pixelCoordinates.x, cameraResolution.height - pixelCoordinates.y), new HoloLensCameraStream.Resolution(cameraResolution.width, cameraResolution.height));
            float focalLengthX = projectionMatrix.GetColumn(0).x;
            float focalLengthY = projectionMatrix.GetColumn(1).y;
            Vector3 dirRay = new Vector3(pixelCoordinates.x / focalLengthX, pixelCoordinates.y / focalLengthY, 1.0f).normalized;
            direction = -new Vector3(Vector3.Dot(dirRay, cameraToWorldMatrix.GetRow(0)), Vector3.Dot(dirRay, cameraToWorldMatrix.GetRow(1)), Vector3.Dot(dirRay, cameraToWorldMatrix.GetRow(2))).normalized;
        }
    }
}