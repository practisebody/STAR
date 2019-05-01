using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public class VitalSignPlacer : MonoBehaviour
    {
        public Camera Camera;
        protected NoninOximeterConnection oxiConn;

        protected VitalSign HR;
        protected VitalSign SpO2;
        protected float Distantce = 5.0f;

        protected readonly float XOffset = -0.64f;
        protected readonly float YOffset = -0.125f;

        private void Start()
        {
            oxiConn = ConnectionManager.Instance["Oximeter"] as NoninOximeterConnection;

            HR   = ObjectFactory.NewVitalSign(transform, new Vector3(XOffset, YOffset + 0.00f, 0f), Color.green,  "HR", "160", "75");
            SpO2 = ObjectFactory.NewVitalSign(transform, new Vector3(XOffset, YOffset + 0.25f, 0f), Color.cyan, "SpO2", "100", "90");

            Configurations.Instance.SetAndAddCallback("*VitalSigns_Distance", Distantce, v =>
            {
                Distantce = v;
                if (gameObject.activeInHierarchy)
                    Configurations.Instance.Invoke("*VitalSigns_Place");
            }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("*VitalSigns_Place", () =>
            {
                transform.SetPositionAndRotation(Camera.transform.position + Camera.transform.forward * Distantce,
                    Quaternion.LookRotation(Camera.transform.forward, Vector3.up));
                gameObject.SetActive(true);
            }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("*VitalSigns_Remove", () => gameObject.SetActive(false), Configurations.RunOnMainThead.YES);
        }

        private void Update()
        {
            HR.Value = oxiConn.PulseRate;
            SpO2.Value = oxiConn.SpO2;
        }
    }
}