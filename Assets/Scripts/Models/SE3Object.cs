using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    /// <summary>
    /// A SE3 object, that triggers observer if position changed
    /// Works when other object's position relies on this object
    /// </summary>
    public class SE3Object : MonoBehaviour, IObservable<SE3Object>
    {
        public SE3 localToWorldMatrix
        {
            get
            {
                return transform.localToWorldMatrix;
            }
            set
            {
                transform.SetPositionAndRotation(value.Translation, value.Rotation);
                Valid = true;
                NotifyObserver();
            }
        }

        public SE3 worldToLocalMatrix
        {
            get
            {
                return transform.worldToLocalMatrix;
            }
        }

        public void NotifyObserver()
        {
            OnChange?.Invoke(this);
        }

        public bool Valid { get; protected set; } = false;

        public event OnChangeHandler<SE3Object> OnChange;
    }
}