using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LCY
{
    /// <summary>
    /// Unity utility functions
    /// </summary>
    public static partial class Utilities
    {
        /// <summary>
        /// Invoke a task in main thread, used when setting properties of gameobjects
        /// </summary>
        public static void InvokeMain(UnityEngine.WSA.AppCallbackItem item, bool waitUntilDone)
        {
            if (UnityEngine.WSA.Application.RunningOnAppThread())
                item.Invoke();
            else
                UnityEngine.WSA.Application.InvokeOnAppThread(item, waitUntilDone);
        }

        /// <summary>
        /// Set the visibility recursively
        /// </summary>
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

        /// <summary>
        /// Destroy all children in a transform
        /// </summary>
        public static void DestroyChildren(Transform transform)
        {
            foreach (Transform child in transform)
                GameObject.Destroy(child.gameObject);
        }
    }
}