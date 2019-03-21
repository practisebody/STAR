using STAR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace STAR
{
    public class Status : MonoBehaviour
    {
        protected Text Text;

        private void Start()
        {
            Text = GetComponentInChildren<Text>();
        }

        private void Update()
        {
            WebRTCConnection conn = ConnectionManager.Instance["WebRTC"] as WebRTCConnection;
            Text.text = "Self: " + conn.WebRTCStatus.ToString() + "\n" +
                "Peer: " + (conn.PeerName != null ? "Connected" : "NotConnected");
            switch (conn.WebRTCStatus)
            {
                case WebRTCConnection.Status.NotConnected:
                    Text.color = Color.red;
                    break;
                case WebRTCConnection.Status.Connecting:
                case WebRTCConnection.Status.Disconnecting:
                case WebRTCConnection.Status.Calling:
                case WebRTCConnection.Status.EndingCall:
                    Text.color = Color.yellow;
                    break;
                case WebRTCConnection.Status.Connected:
                case WebRTCConnection.Status.InCall:
                    Text.color = conn.PeerName != null ? Color.green : Color.yellow;
                    break;
            }
        }
    }
}