using LCY;
using STAR;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FOVBillboard : MonoBehaviour
{
    public Camera Camera;
    protected bool Following { get; set; } = true;

    protected Color Color;

    protected GameObject FOVQuad;
    protected GameObject HoloInfo;
    protected GameObject Status;
    protected Text Text;
    protected GameObject Frame;
    protected LineRenderer FrameTopLeft;
    protected LineRenderer FrameTopRight;
    protected LineRenderer FrameBottomRight;
    protected LineRenderer FrameBottomLeft;

    private void Start()
    {
        FOVQuad = transform.Find("FOV").gameObject;
        HoloInfo = transform.Find("Canvas/HoloInfo").gameObject;
        Status = transform.Find("Canvas/Status").gameObject;
        Text = Status.GetComponentInChildren<Text>();
        Frame = transform.Find("Frame").gameObject;
        FrameTopLeft = Frame.transform.Find("TopLeft").GetComponentInChildren<LineRenderer>();
        FrameTopRight = Frame.transform.Find("TopRight").GetComponentInChildren<LineRenderer>();
        FrameBottomRight = Frame.transform.Find("BottomRight").GetComponentInChildren<LineRenderer>();
        FrameBottomLeft = Frame.transform.Find("BottomLeft").GetComponentInChildren<LineRenderer>();

        Configurations.Instance.SetAndAddCallback("Billboard_Following", Following, v => Following = v);
        Configurations.Instance.SetAndAddCallback("Billboard_ShowFOVQuad", false, v => FOVQuad.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
        Configurations.Instance.SetAndAddCallback("Billboard_ShowHoloInfo", false, v => HoloInfo.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
        Configurations.Instance.SetAndAddCallback("Billboard_ShowStatus", false, v => Status.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
        Configurations.Instance.SetAndAddCallback("Billboard_Frame", true, v => Frame.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
    }

    private void Update()
    {
        if (Following)
        {
            transform.SetPositionAndRotation(Camera.transform.position, Camera.transform.rotation);
        }

        WebRTCConnection conn = ConnectionManager.Instance["WebRTC"] as WebRTCConnection;
        switch (conn.WebRTCStatus)
        {
            case WebRTCConnection.Status.NotConnected:
                Color = Color.red;
                break;
            case WebRTCConnection.Status.Connecting:
            case WebRTCConnection.Status.Disconnecting:
            case WebRTCConnection.Status.Calling:
            case WebRTCConnection.Status.EndingCall:
                Color = Color.yellow;
                break;
            case WebRTCConnection.Status.Connected:
            case WebRTCConnection.Status.InCall:
                Color = conn.PeerName != null ? Color.green : Color.yellow;
                break;
        }

        Text.text = "Self: " + conn.WebRTCStatus.ToString() + "\n" +
            "Peer: " + (conn.PeerName != null ? "Connected" : "NotConnected");
        Text.color = Color;
        FrameTopLeft.startColor = FrameTopLeft.endColor = Color;
        FrameTopRight.startColor = FrameTopRight.endColor = Color;
        FrameBottomRight.startColor = FrameBottomRight.endColor = Color;
        FrameBottomLeft.startColor = FrameBottomLeft.endColor = Color;
    }
}
