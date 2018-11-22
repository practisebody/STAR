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
            Text.text = "Self: " + ConnectionManager.Instance.WebRTCStatus.ToString() + "\n" +
                "Peer: " + (ConnectionManager.Instance.PeerName != null ? "Connected" : "NotConnected");
            switch (ConnectionManager.Instance.WebRTCStatus)
            {
                case ConnectionManager.Status.NotConnected:
                    Text.color = Color.red;
                    break;
                case ConnectionManager.Status.Connecting:
                case ConnectionManager.Status.Disconnecting:
                case ConnectionManager.Status.Calling:
                case ConnectionManager.Status.EndingCall:
                    Text.color = Color.yellow;
                    break;
                case ConnectionManager.Status.Connected:
                case ConnectionManager.Status.InCall:
                    Text.color = ConnectionManager.Instance.PeerName != null ? Color.green : Color.yellow;
                    break;
            }
        }
    }
}