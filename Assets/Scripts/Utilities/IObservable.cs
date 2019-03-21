using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public delegate void OnChangeHandler<T>(T sender);

    public interface IObservable<T>
    {
        event OnChangeHandler<T> OnChange;
    }
}