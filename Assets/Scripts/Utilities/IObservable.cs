using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public delegate void OnChangeCallback<T>(T sender);

    public interface IObservable<T>
    {
        event OnChangeCallback<T> OnChange;
    }
}