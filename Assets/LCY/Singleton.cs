using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCY
{
    /// <summary>
    /// Singleton generic class, instantiate once
    /// </summary>
    public class Singleton<T> where T : Singleton<T>, new()
    {
        public static T Instance { get; } = new T();
    }
}
