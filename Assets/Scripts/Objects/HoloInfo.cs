using LCY;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace STAR
{
    public class HoloInfo : MonoBehaviour
    {
        protected Canvas Canvas { get; set; }
        
        public Camera Camera;
        public Checkerboard Checkerboard;
        public HololensCamera HololensCamera;
        public TopDownCamera TopDownCamera;
        public Annotations Annotations;

        private void Start()
        {
            Canvas = transform.Find("Canvas").GetComponent<Canvas>();
            LogStart();
            StatusStart();
            Configurations.Instance.SetAndAddCallback("ShowDebug", true, v => gameObject.SetActive(v), Configurations.RunOnMainThead.YES);
        }

        private void Update()
        {
            StatusUpdate();
        }

        #region log

        protected Text LogText { get; set; }
        public string LogString { get; protected set; }
        public int MaxNumMessages;
        protected List<string> Logs = new List<string>();

        protected void LogStart()
        {
            LogText = transform.Find("Canvas/LogPanel/LogText").GetComponent<Text>();
            Application.logMessageReceivedThreaded += LogMessageReceived;
        }

        public string LogLastNString(int n = 20)
        {
            string log = "";
            int start = Math.Max(Logs.Count - 20, 0);
            for (; start < Logs.Count; ++start)
            {
                log += Logs[start] + "\n";
            }
            return log;
        }

        protected void LogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (condition.Contains("RenderTexture.GenerateMips failed"))
            {
                return;
            }
            Logs.Add(condition);
            LogText.text = LogLastNString(MaxNumMessages);
            LogString += condition + "\n";
        }

        #endregion

        #region status

        protected Text StatusText { get; set; }
        public string StatusString { get; protected set; }

        protected void StatusStart()
        {
            StatusText = transform.Find("Canvas/LogPanel/StatusText").GetComponent<Text>(); ;
        }

        protected void StatusUpdate()
        {
            UpdateStatusString();
            StatusText.text = StatusString;
        }

        public void UpdateStatusString()
        {
            StatusString = String.Empty;
            StatusString += "Configs:" + Configurations.Instance.ToString(":", "\n") + "\n";
            StatusString += "Camera: " + Utilities.FormatMatrix4x4(Camera.transform.localToWorldMatrix) + "\n";
            StatusString += "Check2World: " + Utilities.FormatMatrix4x4(Checkerboard.LocalToWorldMatrix) + "\n";
            //if (TopDownCamera.IntrinsicValid != null)
            //    StatusString += "TopDown2Checker: r:" + Utilities.FormatVector3(Utilities.JSON2Vector3(TopDownCamera.CalibrationData["rvec"])) + " t:" + Utilities.FormatVector3(Utilities.JSON2Vector3(TopDownCamera.CalibrationData["tvec"])) + "\n";
            StatusString += "AnnotationServer: " + (Annotations.Connected ? "Connected" : "Connecting") + "\n";
            StatusString += "Anno: " + Annotations?.ToString() + "\n";
        }

        #endregion
    }
}