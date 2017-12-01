using UnityEngine;

namespace LCY
{
    public class UnitySingleton<T> : MonoBehaviour where T : UnitySingleton<T>
    {
        protected static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<T>();
                }
                return instance;
            }
        }
    }
}