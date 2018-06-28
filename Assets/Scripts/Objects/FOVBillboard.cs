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

    protected GameObject FOVQuad;
    protected GameObject HoloInfo;
    protected GameObject Status;
    protected Text StatusText;

    private void Start ()
    {
        FOVQuad = transform.Find("FOV").gameObject;
        HoloInfo = transform.Find("HoloInfo").gameObject;
        Status = transform.Find("Status").gameObject;
        StatusText = Status.GetComponentInChildren<Text>();

        Configurations.Instance.SetAndAddCallback("Billboard_Following", Following, v => Following = v);
        Configurations.Instance.SetAndAddCallback("Billboard_ShowFOVQuad", true, v => FOVQuad.SetActive(v), Configurations.RunOnMainThead.YES);
        Configurations.Instance.SetAndAddCallback("Billboard_ShowHoloInfo", false, v => HoloInfo.SetActive(v), Configurations.RunOnMainThead.YES);
        Configurations.Instance.SetAndAddCallback("Billboard_ShowStatus", true, v => Status.SetActive(v), Configurations.RunOnMainThead.YES);
    }

    void Update ()
    {
        if (Following)
        {
            transform.SetPositionAndRotation(Camera.transform.position, Camera.transform.rotation);
        }

        StatusText.text = ConnectionManager.Instance.WebRTCStatus.ToString();
        switch (ConnectionManager.Instance.WebRTCStatus)
        {
            case ConnectionManager.Status.NotConnected:
                StatusText.color = Color.red;
                break;
            case ConnectionManager.Status.Connecting:
            case ConnectionManager.Status.Disconnecting:
            case ConnectionManager.Status.Calling:
            case ConnectionManager.Status.EndingCall:
                StatusText.color = Color.yellow;
                break;
            case ConnectionManager.Status.Connected:
            case ConnectionManager.Status.InCall:
                StatusText.color = Color.green;
                break;
        }
    }
}
