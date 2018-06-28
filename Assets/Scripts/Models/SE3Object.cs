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
                valid = true;
                NotifyObserver();
            }
        }

        public void NotifyObserver()
        {
            OnChange?.Invoke(this);
        }

        public bool valid { get; protected set; } = false;

        public event OnChangeCallback<SE3Object> OnChange;
    }
}