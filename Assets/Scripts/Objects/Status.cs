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

        protected WebRTCConnection WebRTCConn;
        protected ARUWPController UltrasoundController;

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

            WebRTCConn = ConnectionManager.Instance["WebRTC"] as WebRTCConnection;
            UltrasoundController = GameObject.Find("UltrasoundTracker").GetComponentInChildren<ARUWPController>();

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
                TopLeftText.text = "Self: " + WebRTCConn.StatusInfo + "\n" +
                    "Peer: " + (WebRTCConn.PeerName ?? "NotConnected");
                switch (WebRTCConn.Status)
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
                TopRightText.color = GetColor(ip != null);
            }

            // BottomLeft, Ultrasound
            if (WebRTCConn.WebRTCStatus == WebRTCConnection.WebRTCStatuses.InCall)
            {
                Color ultraColor = GetColor(UltrasoundTracker.Tracked);
                StringBuilder sb = new StringBuilder();
                if (Mode == Modes.DEBUG)
                {
                    sb.Append("FPS: ").Append(UltrasoundController.GetTrackingFPS()).AppendLine();
                }
                sb.Append("Ultrasound: ").Append(UltrasoundTracker.Tracked ? "Tracked" : "Lost tracking");
                BottomLeftText.text = sb.ToString();
                BottomLeftText.color = ultraColor;
            }

            // BottomRight, Vital signs
            NoninOximeterConnection oxiConn = ConnectionManager.Instance["Oximeter"] as NoninOximeterConnection;
            if (oxiConn.StatusInfo == null)
            {
                StringBuilder sb = new StringBuilder();
                Color oxiColor = GetColor(oxiConn.Connected);
                sb.Append("Pulse rate: ").Append(oxiConn.PulseRate).AppendLine();
                sb.Append("SpO2: ").Append(oxiConn.SpO2).Append("%");
                BottomRightText.text = sb.ToString();
                BottomRightText.color = oxiColor;
            }
            else
            {
                BottomRightText.text = oxiConn.StatusInfo;
                BottomRightText.color = Color.red;
            }
            
        }

        protected void Clear()
        {
            TopLeftText.text = string.Empty;
            TopRightText.text = string.Empty;
            BottomLeftText.text = string.Empty;
            BottomRightText.text = string.Empty;
        }

        protected Color GetColor(bool status)
        {
            return status ? Color.green : Color.red;
        }
    }
}
