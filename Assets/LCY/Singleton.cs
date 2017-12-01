using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCY
{
    public class Singleton<T> where T : Singleton<T>, new()
    {
        public static T Instance { get; } = new T();
    }
}
