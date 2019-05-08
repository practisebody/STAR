using LCY;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace STAR
{
    /// <summary>
    /// Additional information, mainly for debugging
    /// </summary>
    public class HoloInfo : MonoBehaviour
    {
        public Camera Camera;
        public HololensCamera HololensCamera;
        public Room Room;
        public Annotations Annotations;

        private void Start()
        {
            LogStart();
            StatusStart();

            Configurations.Instance.SetAndAddCallback("Billboard_ShowHoloInfo", false, v => gameObject.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("*_PrepareUI", () => Configurations.Instance.Set("Billboard_ShowHoloInfo", false));
        }

        private void Update()
        {
            StatusUpdate();
        }

        #region log

        // Logs, all the information from Debug.Log
        protected Text LogText { get; set; }
        public string LogString { get; protected set; }
        public int MaxNumMessages;
        protected List<string> Logs = new List<string>();

        protected void LogStart()
        {
            LogText = transform.Find("LogPanel/LogText").GetComponent<Text>();
            Application.logMessageReceivedThreaded += LogMessageReceived;
        }

        public string LogLastNString(int n = 20)
        {
            StringBuilder sb = new StringBuilder(2000);
            int start = Math.Max(Logs.Count - 20, 0);
            for (; start < Logs.Count; ++start)
            {
                sb.AppendLine(Logs[start]);
            }
            return sb.ToString(); ;
        }

        protected void LogMessageReceived(string condition, string stackTrace, LogType type)
        {
            Logs.Add(condition);
            LCY.Utilities.InvokeMain(() => LogText.text = LogLastNString(MaxNumMessages), false);
            LogString += condition + "\n";
            ControllerManager.Instance.SendLog();
        }

        #endregion

        #region status

        // Status, Configurations, Object locations and Annotations
        protected Text StatusText { get; set; }
        public string StatusString { get; protected set; }

        protected void StatusStart()
        {
            StatusText = transform.Find("LogPanel/StatusText").GetComponent<Text>(); ;
        }

        protected void StatusUpdate()
        {
            StringBuilder sb = new StringBuilder(1000);
            sb.Append("Configs:").AppendLine(Configurations.Instance.ToString(":", "\n"));
            sb.Append("Camera: ").AppendLine(Utilities.FormatMatrix4x4(Camera.transform.localToWorldMatrix));
            sb.Append("HoloCamera: ").AppendLine(Utilities.FormatMatrix4x4(HololensCamera.transform.localToWorldMatrix));
            sb.Append("Room: ").AppendLine(Utilities.FormatMatrix4x4(Room.transform.localToWorldMatrix));
            sb.Append("Connections:");
            foreach (IConnection conn in ConnectionManager.Instance.Connections)
            {
                sb.Append(conn.Name).Append(":").Append(conn.Connected).AppendLine();
            }
            sb.Append("Anno: ").AppendLine(Annotations?.ToString());
            StatusString = sb.ToString();
            StatusText.text = StatusString;
        }

        #endregion
    }
}