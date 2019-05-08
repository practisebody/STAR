using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    /// <summary>
    /// A frame to highlight Hololens FOV, and also use color as the WebRTC status indicator
    /// </summary>
    public class Frame : MonoBehaviour
    {
        protected Color Color
        {
            set
            {
                LineTopLeft.startColor = LineTopLeft.endColor = value;
                LineTopRight.startColor = LineTopRight.endColor = value;
                LineBottomLeft.startColor = LineBottomLeft.endColor = value;
                LineBottomRight.startColor = LineBottomRight.endColor = value;
            }
        }
        protected LineRenderer LineTopLeft;
        protected LineRenderer LineTopRight;
        protected LineRenderer LineBottomLeft;
        protected LineRenderer LineBottomRight;

        private void Start()
        {
            LineTopLeft = transform.Find("TopLeft").GetComponent<LineRenderer>();
            LineTopRight = transform.Find("TopRight").GetComponent<LineRenderer>();
            LineBottomLeft = transform.Find("BottomLeft").GetComponent<LineRenderer>();
            LineBottomRight = transform.Find("BottomRight").GetComponent<LineRenderer>();

            Configurations.Instance.SetAndAddCallback("Billboard_Frame", true, v => gameObject.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.AddCallback("*_PrepareUI", () => Configurations.Instance.Set("Billboard_Frame", true));
        }

        private void Update()
        {
            // Red if not connected
            // Yellow if connecting
            // Green if conntected
            WebRTCConnection conn = ConnectionManager.Instance["WebRTC"] as WebRTCConnection;
            switch (conn.Status)
            {
                case WebRTCConnection.Statuses.NotConnected:
                    Color = Color.red;
                    break;
                case WebRTCConnection.Statuses.Pending:
                    Color = Color.yellow;
                    break;
                case WebRTCConnection.Statuses.Connected:
                    Color = Color.green;
                    break;
            }
        }
    }
}