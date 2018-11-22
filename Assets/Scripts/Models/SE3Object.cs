using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
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

        public event OnChangeCallback<SE3Object> OnChange;
    }
}