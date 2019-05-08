using LCY;
using Newtonsoft.Json.Linq;
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
    /// <summary>
    /// Utility functions
    /// </summary>
    static public class Utilities
    {
        /// <summary>
        /// Get current IP address
        /// </summary>
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

        /// <summary>
        /// Get current time as a string, typically for folder name
        /// </summary>
        public static string TimeNow()
        {
            return DateTime.Now.ToString("yyMMdd_HHmmss");
        }

        /// <summary>
        /// Persistent data path
        /// </summary>
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

        /// <summary>
        /// Append file name to get full path
        /// </summary>
        public static string FullPath(string name)
        {
            return Path.Combine(FolderName, name);
        }

        /// <summary>
        /// Create a folder
        /// </summary>
        public static void CreateFolder(string name)
        {
            string fullname = Path.Combine(FolderName, name);
            if (Directory.Exists(fullname) == false)
            {
                Directory.CreateDirectory(fullname);
            }
        }

        /// <summary>
        /// Save an array of bytes to a file
        /// </summary>
        public static void SaveFile(byte[] bytes, string name)
        {
            string fullname = Path.Combine(FolderName, name);
            File.WriteAllBytes(fullname, bytes);
        }

        /// <summary>
        /// Save a string to a file
        /// </summary>
        public static void SaveFile(string content, string name)
        {
            string fullname = Path.Combine(FolderName, name);
            File.WriteAllText(fullname, content);
        }

        /// <summary>
        /// Converts a json string to Vector3
        /// </summary>
        public static Vector3 JSON2Vector3(JSONNode node)
        {
            return new Vector3(node[0].AsFloat, node[1].AsFloat, node[2].AsFloat);
        }

        /// <summary>
        /// Converts a Matrix4x4 to json string
        /// </summary>
        public static JArray Matrix4x42JArray(Matrix4x4 m)
        {
            return new JArray(new float[]
                {
                    m.m00, m.m01, m.m02, m.m03,
                    m.m10, m.m11, m.m12, m.m13,
                    m.m20, m.m21, m.m22, m.m23,
                    m.m30, m.m31, m.m32, m.m33
                });
        }

        #region log

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

        #endregion
    }
}