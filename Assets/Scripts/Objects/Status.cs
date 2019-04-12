using LCY;
using STAR;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace STAR
{
    public class Status : MonoBehaviour
    {
        public enum Modes
        {
            DEBUG,
            RUN,
        };

        protected Text TopLeft;
        protected Text TopRight;
        protected Text BottomLeft;
        protected Text BottomRight;

        public Modes Mode { get; private set; } = Modes.DEBUG;

        private void Start()
        {
            TopLeft = transform.Find("TopLeft").GetComponentInChildren<Text>();
            TopRight = transform.Find("TopRight").GetComponentInChildren<Text>();
            BottomLeft = transform.Find("BottomLeft").GetComponentInChildren<Text>();
            BottomRight = transform.Find("BottomRight").GetComponentInChildren<Text>();

            Configurations.Instance.SetAndAddCallback("Billboard_StatusDebugMode", true, v =>
            {
                Mode = v ? Modes.DEBUG : Mode = Modes.RUN;
                TopLeft.text = string.Empty;
                TopRight.text = string.Empty;
                BottomLeft.text = string.Empty;
                BottomRight.text = string.Empty;
            }, Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("*_PrepareUI", () => Configurations.Instance.Set("Billboard_StatusDebugMode", false));
        }

        private void Update()
        {
            if (Mode == Modes.DEBUG)
            {
                // TopLeft, WebRTC Status
                WebRTCConnection conn = ConnectionManager.Instance["WebRTC"] as WebRTCConnection;
                TopLeft.text = "Self: " + conn.StatusInfo + "\n" +
                    "Peer: " + (conn.PeerName ?? "NotConnected");
                switch (conn.Status)
                {
                    case WebRTCConnection.Statuses.NotConnected:
                        TopLeft.color = Color.red;
                        break;
                    case WebRTCConnection.Statuses.Pending:
                        TopLeft.color = Color.yellow;
                        break;
                    case WebRTCConnection.Statuses.Connected:
                        TopLeft.color = Color.green;
                        break;
                }

                // TopRight, IP address
                string ip = Utilities.GetIPAddress();
                if (ip != null)
                    TopRight.text = Utilities.GetIPAddress();
                TopRight.color = ip == null ? Color.red : Color.green;
            }

            // BottomLeft, Vital signs
            NoninOximeterConnection oxiConn = ConnectionManager.Instance["Oximeter"] as NoninOximeterConnection;
            if (oxiConn.StatusInfo == null)
            {
                StringBuilder sb = new StringBuilder();
                Color oxiColor = oxiConn.Connected ? Color.green : Color.red;
                sb.Append("Pulse rate: ").Append(oxiConn.PulseRate).AppendLine();
                sb.Append("SpO2: ").Append(oxiConn.SpO2).Append("%");
                BottomLeft.text = sb.ToString();
                BottomLeft.color = oxiColor;
            }
            else
            {
                BottomLeft.text = oxiConn.StatusInfo;
                BottomLeft.color = Color.red;
            }
            
        }
    }
}
