using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
#if NETFX_CORE
using HoloPoseClient.Signalling;
#endif

namespace STAR
{
    public class UltrasoundTracker : MonoBehaviour
    {
        public GameObject UltrasoundGizmo;
        static public bool Tracked { get; protected set; } = false;

        protected float LastUpdate = 0f;
        protected Matrix4x4 LastPose = Matrix4x4.identity;

        private void Start()
        {
            Configurations.Instance.SetAndAddCallback("Visual_Ultrasound", false, (v) => UltrasoundGizmo.SetActive(v),
                Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("*_PrepareUI", () => Configurations.Instance.Set("Visual_Ultrasound", false));
        }

        private void Update()
        {
            Matrix4x4 pose = UltrasoundGizmo.transform.localToWorldMatrix;

            if (pose != LastPose)
            {
                Tracked = true;
#if NETFX_CORE
                JObject message = new JObject
                {
                    ["type"] = "T",
                    // you have matrix, you have everything
                    ["matrix"] = Utilities.Matrix4x42JArray(pose)
                };

                JObject container = new JObject
                {
                    ["message"] = message
                };
                string jsonString = container.ToString();
                Conductor.Instance.SendMessage(WebRTCConnection.MentorName, Windows.Data.Json.JsonObject.Parse(jsonString));
#endif
                LastPose = pose;
                LastUpdate = Time.time;
            }
            else
            {
                if (Time.time - LastUpdate > 0.5f)
                    Tracked = false;
            }
        }
    }
}