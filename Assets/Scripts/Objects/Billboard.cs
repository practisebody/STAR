using LCY;
using STAR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace STAR
{
    public class Billboard : MonoBehaviour
    {
        public Camera Camera;
        protected bool Following { get; set; } = true;

        private void Start()
        {
            Configurations.Instance.SetAndAddCallback("Billboard_Following", Following, v => Following = v);
        }

        private void Update()
        {
            if (Following)
            {
                transform.SetPositionAndRotation(Camera.transform.position, Camera.transform.rotation);
            }
        }
    }
}