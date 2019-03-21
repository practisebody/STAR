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
    protected GameObject Frame;
    protected LineRenderer FrameTopLeft;
    protected LineRenderer FrameTopRight;
    protected LineRenderer FrameBottomRight;
    protected LineRenderer FrameBottomLeft;

    private void Start()
    {
        FOVQuad = transform.Find("FOV").gameObject;
        HoloInfo = transform.Find("Canvas/HoloInfo").gameObject;
        Frame = transform.Find("Frame").gameObject;
        FrameTopLeft = Frame.transform.Find("TopLeft").GetComponentInChildren<LineRenderer>();
        FrameTopRight = Frame.transform.Find("TopRight").GetComponentInChildren<LineRenderer>();
        FrameBottomRight = Frame.transform.Find("BottomRight").GetComponentInChildren<LineRenderer>();
        FrameBottomLeft = Frame.transform.Find("BottomLeft").GetComponentInChildren<LineRenderer>();

        Configurations.Instance.SetAndAddCallback("Billboard_Following", Following, v => Following = v);
        Configurations.Instance.SetAndAddCallback("Billboard_ShowFOVQuad", false, v => FOVQuad.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
        Configurations.Instance.SetAndAddCallback("Billboard_ShowHoloInfo", false, v => HoloInfo.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
        Configurations.Instance.SetAndAddCallback("Billboard_Frame", true, v => Frame.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);

        Configurations.Instance.AddCallback("*_PrepareUI", () =>
        {
            Configurations.Instance.Set("Billboard_ShowFOVQuad", false);
            Configurations.Instance.Set("Billboard_ShowHoloInfo", false);
            Configurations.Instance.Set("Billboard_Frame", true);
        });
    }

    private void Update()
    {
        if (Following)
        {
            transform.SetPositionAndRotation(Camera.transform.position, Camera.transform.rotation);
        }

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
        FrameTopLeft.startColor = FrameTopLeft.endColor = Color;
        FrameTopRight.startColor = FrameTopRight.endColor = Color;
        FrameBottomRight.startColor = FrameBottomRight.endColor = Color;
        FrameBottomLeft.startColor = FrameBottomLeft.endColor = Color;
    }
}
