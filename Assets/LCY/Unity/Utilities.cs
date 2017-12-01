using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCY
{
    public static partial class Utilities
    {
        public static void InvokeMain(UnityEngine.WSA.AppCallbackItem item, bool waitUntilDone = true)
        {
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
    }
}