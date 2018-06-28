using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public class WorldManager : UnitySingleton<WorldManager>
    {
        public bool valid { get; protected set; }
        public GameObject Gizmo;

        // TODO(chengyuanlin)
        // maybe can be simplied
        public Vector3 position
        {
            get { return transform.position; }
            set
            {
                transform.position = value;
            }
        }
        public Quaternion rotation
        {
            get { return transform.rotation; }
            protected set
            {
                transform.rotation = value;
            }
        }
        protected Vector3 up = Vector3.zero;
        public Vector3 Up
        {
            get { return up; }
            set
            {
                up = value;
                if (right != Vector3.zero)
                {
                    SetRotation();
                }
            }
        }
        protected Vector3 right = Vector3.zero;
        public Vector3 Right
        {
            get { return right; }
            set
            {
                right = value;
                if (up != Vector3.zero)
                {
                    SetRotation();
                }
            }
        }
        public Vector3 Forward { get; protected set; }

        private void Start()
        {
            Configurations.Instance.SetAndAddCallback("Visual_World", false, v => Gizmo.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
        }

        protected void SetRotation()
        {
            Forward = Vector3.Cross(right, up);
            right = Vector3.Cross(up, Forward);
            rotation = Quaternion.LookRotation(Forward, up);
        }
    }
}