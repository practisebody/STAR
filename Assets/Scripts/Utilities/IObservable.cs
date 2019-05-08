using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public delegate void OnChangeHandler<T>(T sender);

    /// <summary>
    /// An interface that notify observe when value changes
    /// </summary>
    public interface IObservable<T>
    {
        event OnChangeHandler<T> OnChange;
    }
}