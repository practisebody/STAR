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

        protected Transform TopLeft;
        protected Text TopLeftText;
        protected Transform TopLeftOther;

        protected Transform TopRight;
        protected Text TopRightText;
        protected Transform TopRightOther;

        protected Transform BottomLeft;
        protected Text BottomLeftText;
        protected Transform BottomLeftOther;

        protected Transform BottomRight;
        protected Text BottomRightText;
        protected Transform BottomRightOther;

        public Modes Mode { get; private set; } = Modes.DEBUG;

        private void Start()
        {
            TopLeft = transform.Find("TopLeft");
            TopLeftText = TopLeft.GetComponentInChildren<Text>();
            TopLeftOther = TopLeft.Find("Other");

            TopRight = transform.Find("TopRight");
            TopRightText = TopRight.GetComponentInChildren<Text>();
            TopRightOther = TopRight.Find("Other");

            BottomLeft = transform.Find("BottomLeft");
            BottomLeftText = BottomLeft.GetComponentInChildren<Text>();
            BottomLeftOther = BottomLeft.Find("Other");

            BottomRight = transform.Find("BottomRight");
            BottomRightText = BottomRight.GetComponentInChildren<Text>();
            BottomRightOther = BottomRight.Find("Other");

            Configurations.Instance.SetAndAddCallback("Billboard_StatusDebugMode", true, v =>
            {
                Mode = v ? Modes.DEBUG : Mode = Modes.RUN;
                Clear();
            }, Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("*_PrepareUI", () => Configurations.Instance.Set("Billboard_StatusDebugMode", false));
        }

        private void Update()
        {
            if (Mode == Modes.DEBUG)
            {
                // TopLeft, WebRTC Status
                WebRTCConnection conn = ConnectionManager.Instance["WebRTC"] as WebRTCConnection;
                TopLeftText.text = "Self: " + conn.StatusInfo + "\n" +
                    "Peer: " + (conn.PeerName ?? "NotConnected");
                switch (conn.Status)
                {
                    case WebRTCConnection.Statuses.NotConnected:
                        TopLeftText.color = Color.red;
                        break;
                    case WebRTCConnection.Statuses.Pending:
                        TopLeftText.color = Color.yellow;
                        break;
                    case WebRTCConnection.Statuses.Connected:
                        TopLeftText.color = Color.green;
                        break;
                }

                // TopRight, IP address
                string ip = Utilities.GetIPAddress();
                if (ip != null)
                    TopRightText.text = Utilities.GetIPAddress();
                TopRightText.color = ip == null ? Color.red : Color.green;
            }

            // BottomRight, Vital signs
            NoninOximeterConnection oxiConn = ConnectionManager.Instance["Oximeter"] as NoninOximeterConnection;
            if (oxiConn.StatusInfo == null)
            {
                StringBuilder sb = new StringBuilder();
                Color oxiColor = oxiConn.Connected ? Color.green : Color.red;
                sb.Append("Pulse rate: ").Append(oxiConn.PulseRate).AppendLine();
                sb.Append("SpO2: ").Append(oxiConn.SpO2).Append("%");
                BottomRightText.text = sb.ToString();
                BottomRightText.color = oxiColor;
            }
            else
            {
                BottomLeftText.text = oxiConn.StatusInfo;
                BottomLeftText.color = Color.red;
            }
            
        }

        protected void Clear()
        {
            TopLeftText.text = string.Empty;
            TopRightText.text = string.Empty;
            BottomLeftText.text = string.Empty;
            BottomRightText.text = string.Empty;
        }
    }
}
