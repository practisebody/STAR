using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCY
{
    public static partial class Utilities
    {
        public static void InvokeMain(UnityEngine.WSA.AppCallbackItem item, bool waitUntilDone = true)
        {
            if (UnityEngine.WSA.Application.RunningOnAppThread())
                item.Invoke();
            else
                UnityEngine.WSA.Application.InvokeOnAppThread(item, waitUntilDone);
        }

        public static void SetVisibility(Transform t, bool v)
        {
            Renderer r = t.GetComponent<Renderer>();
            if (r != null)
            {
                r.enabled = v;
            }
            foreach (Transform child in t)
            {
                SetVisibility(child, v);
            }
        }

        public static void DestroyChildren(Transform transform)
        {
            foreach (Transform child in transform)
                GameObject.Destroy(child.gameObject);
        }

        //public static string MeshToString(MeshFilter mf)
        //{
        //    Mesh m = mf.mesh;
        //    Material[] mats = mf.GetComponent<MeshRenderer>().sharedMaterials;

        //    StringBuilder sb = new StringBuilder();

        //    sb.Append("g ").Append(mf.name).Append("\n");
        //    foreach (Vector3 v in m.vertices)
        //    {
        //        sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
        //    }
        //    sb.Append("\n");
        //    foreach (Vector3 v in m.normals)
        //    {
        //        sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        //    }
        //    sb.Append("\n");
        //    foreach (Vector3 v in m.uv)
        //    {
        //        sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        //    }
        //    for (int material = 0; material < m.subMeshCount; material++)
        //    {
        //        sb.Append("\n");
        //        sb.Append("usemtl ").Append(mats[material].name).Append("\n");
        //        sb.Append("usemap ").Append(mats[material].name).Append("\n");

        //        int[] triangles = m.GetTriangles(material);
        //        for (int i = 0; i < triangles.Length; i += 3)
        //        {
        //            sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
        //                triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
        //        }
        //    }
        //    return sb.ToString();
        //}
    }
}