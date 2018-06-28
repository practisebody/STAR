using HoloToolkit.Unity.SpatialMapping;
using LCY;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public class TopDownCamera : SE3Camera
    {
        protected List<Vector2> checkerPoints;
        public List<Vector2> CheckerPoints
        {
            get { return checkerPoints; }
            protected set
            {
                if ((checkerPoints = value) != null)
                {
                    Vector3 origin = localToWorldMatrix.Translation;
                    Quaternion rotation = localToWorldMatrix.Rotation;
                    LCY.Utilities.DestroyChildren(TopDownCameraRays);
                    foreach (Vector2 p in value)
                    {
                        Vector3 direction = rotation * UnprojRaw(p, DoUndistort.YES);
                        RaycastHit hitInfo;
                        Physics.Raycast(origin, direction, out hitInfo, 300.0f, SpatialMappingManager.Instance.LayerMask);
                        ObjectFactory.NewRay(TopDownCameraRays, origin, hitInfo.point, Color.green);
                    }
                }
            }
        }

        public JSONNode CalibrationData
        {
            set
            {
                try
                {
                    // validating checker parameters
                    int chessX = value["chess_x"].AsInt;
                    int chessY = value["chess_y"].AsInt;
                    float chessSize = value["chess_square_size_meters"].AsFloat;

                    // validating intrinsics
                    int width = value["width"].AsInt;
                    int height = value["height"].AsInt;
                    float fx = value["fx"].AsFloat;
                    float fy = value["fy"].AsFloat;
                    float cx = value["cx"].AsFloat;
                    float cy = value["cy"].AsFloat;
                    float k1 = value["k1"].AsFloat;
                    float k2 = value["k2"].AsFloat;
                    float p1 = value["p1"].AsFloat;
                    float p2 = value["p2"].AsFloat;
                    float k3 = value["k3"].AsFloat;

                    // validating extrinsics
                    Quaternion R = LCY.Utilities.Rodrigues2Quaternion(Utilities.JSON2Vector3(value["rvec"]));
                    Vector3 t = Utilities.JSON2Vector3(value["tvec"]);

                    // if validated, assign data
                    Checkerboard.SetCheckerSize(chessX, chessY, chessSize);

                    // intrinsics
                    Width = width;
                    Height = height;
                    Fx = fx;
                    Fy = fy;
                    Cx = cx;
                    Cy = cy;
                    K1 = k1;
                    K2 = k2;
                    P1 = p1;
                    P2 = p2;
                    K3 = k3;
                    IntrinsicValid = true;

                    // extrinsics
                    CheckerToLocalMatrix = Matrix4x4.TRS(t, R, Vector3.one);
                    if (Checkerboard.localToWorldMatrix != null)
                    {
                        UpdateLocation(null);
                    }

                    // additional
                    if (value["points"] != null)
                    {
                        List<Vector2> checker = new List<Vector2>();
                        foreach (JSONNode n in value["points"].AsArray)
                        {
                            checker.Add(new Vector2(n[0].AsFloat, n[1].AsFloat));
                        }
                        CheckerPoints = checker;
                    }
                }
                catch (Exception e)
                {
                    Utilities.LogException(e);
                }
            }
        }
        
        //protected readonly string TEST_RECEIVED_MSG_FROM_TOPDOWN = "{\"cx\": 328.90630324279033, \"cy\": 237.78931042000124, \"fx\": 514.24881005892848, \"fy\": 507.79248508954851, \"height\": 480, \"k1\": 0.0914051948147546, \"k2\": -0.22685463029938513, \"k3\": 0.18341275494159623, \"p1\": -0.022599626729701242, \"p2\": 0.002208592710481527, \"rms\": 0.39623399395854214, \"rvec\": [0.0, 0.0, 0.0], \"tvec\": [0.0, 0.0, 0.0], \"width\": 640, \"chess_x\": 5, \"chess_y\": 4, \"chess_square_size_meters\": 0.03}";
        //protected readonly string TEST_RECEIVED_MSG_FROM_TOPDOWN = "{\"cx\": 328.90630324279033, \"cy\": 237.78931042000124, \"fx\": 514.24881005892848, \"fy\": 507.79248508954851, \"height\": 480, \"k1\": 0.0914051948147546, \"k2\": -0.22685463029938513, \"k3\": 0.18341275494159623, \"p1\": -0.022599626729701242, \"p2\": 0.002208592710481527, \"rms\": 0.39623399395854214, \"rvec\": [0.0, 0.0, 0.0], \"tvec\": [0.0, 0.0, 0.0], \"width\": 640, \"chess_x\": 7, \"chess_y\": 5, \"chess_square_size_meters\": 0.03}";
        //protected readonly string TEST_RECEIVED_MSG_FROM_TOPDOWN = "{\"cx\": 328.90630324279033, \"cy\": 237.78931042000124, \"fx\": 514.24881005892848, \"fy\": 507.79248508954851, \"height\": 480, \"k1\": 0.0914051948147546, \"k2\": -0.22685463029938513, \"k3\": 0.18341275494159623, \"p1\": -0.022599626729701242, \"p2\": 0.002208592710481527, \"rms\": 0.39623399395854214, \"rvec\": [0.0, 0.0, 0.0], \"tvec\": [0.0, 0.0, 0.0], \"width\": 640, \"chess_x\": 9, \"chess_y\": 6, \"chess_square_size_meters\": 0.05}";

        public Checkerboard Checkerboard;
        public Transform TopDownCameraRays;
        public HololensCamera HololensCamera;

        protected USocketServer Server { get; set; }
        public int Port = 4434;

        protected SE3 CheckerToLocalMatrix { get; set; }

        void Start()
        {
            Server = new USocketServer(Port);
            Server.ConnectionReceived += ConnectionReceived;
            Server.Listen();

            Configurations.Instance.SetAndAddCallback("Visual_TopdownCamera", false, v => transform.parent.gameObject.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);

            Checkerboard.OnChange += UpdateLocation;

            // dummy input
            //MessageReceived(TEST_RECEIVED_MSG_FROM_TOPDOWN);
        }

        protected void ConnectionReceived(USocketServer server, USocketClient client)
        {
            client.MessageReceived += MessageReceived;
            client.BeginRead();
        }

        protected void MessageReceived(string s)
        {
            try
            {
                JSONNode jsonObj = JSON.Parse(s);
                LCY.Utilities.InvokeMain(() => CalibrationData = jsonObj, false);
            }
            catch (Exception e)
            {
                Utilities.LogException(e);
            }
        }

        protected void UpdateLocation(SE3Object sender)
        {
            localToWorldMatrix = Checkerboard.localToWorldMatrix * CheckerToLocalMatrix.inverse;
            WorldManager.Instance.Right = transform.right;
            CheckerPoints = checkerPoints;
            ExtrinsicValid = true;
        }
    }
}