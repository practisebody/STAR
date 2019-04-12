using HoloToolkit.Unity.SpatialMapping;
using LCY;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace STAR
{
    public class Tags : MonoBehaviour
    {
        public Camera Camera;
        protected NoninOximeterConnection oxiConn;

        protected GameObject Tag = null;

        private void Start()
        {
            oxiConn = ConnectionManager.Instance["Oximeter"] as NoninOximeterConnection;

            Configurations.Instance.AddCallback("Tags_Place", () => Tag = ObjectFactory.NewTag(transform, GetPlace()), Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("Tags_RemoveTags", () => LCY.Utilities.DestroyChildren(transform), Configurations.RunOnMainThead.YES);
        }

        private void Update()
        {
            if (Tag)
            {
                TextMesh textMesh = Tag.GetComponentInChildren<TextMesh>();
                StringBuilder sb = new StringBuilder();
                sb.Append("Heart rate: ").Append(oxiConn.PulseRate).AppendLine();
                sb.Append("SpO2: ").Append(oxiConn.SpO2);
                textMesh.text = sb.ToString();
                textMesh.color = oxiConn.Connected ? Color.green : Color.red;
            }
        }

        protected Vector3 GetPlace()
        {
            RaycastHit hitInfo;
            bool result = Physics.Raycast(Camera.transform.position, Camera.transform.forward, out hitInfo, 300.0f, SpatialMappingManager.Instance.LayerMask);
            return hitInfo.point;
        }
    }
}
