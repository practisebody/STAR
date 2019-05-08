using UnityEngine;

namespace LCY
{
    /// <summary>
    /// A Unity singleton generics class, instantiated in Unity editor
    /// </summary>
    public class UnitySingleton<T> : MonoBehaviour where T : UnitySingleton<T>
    {
        protected static T instance = null;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    Utilities.InvokeMain(() => instance = FindObjectOfType<T>(), true);
                }
                return instance;
            }
        }
    }
}