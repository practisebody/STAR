using LCY;
using System.Collections.Generic;
using UnityEngine;
using System;
using SimpleJSON;

namespace STAR
{
    public class ConnectionManager : UnitySingleton<ConnectionManager>
    {
        public Annotations Annotations;
        public HololensCamera HololensCamera;

        public Dictionary<string, IConnection>.ValueCollection Connections => _Connections.Values;
        protected Dictionary<string, IConnection> _Connections = new Dictionary<string, IConnection>();
        public IConnection this[string key] => _Connections[key];

        private void Start()
        {
            _Connections["WebRTC"] = new WebRTCConnection();
            _Connections["Socket"] = new SocketConnection();
            foreach (IConnection conn in Connections)
            {
                conn.Start();
                conn.OnMessageReceived += MessageReceived;
            }
        }

        private void Update()
        {
            foreach (IConnection conn in Connections)
            {
                conn.Update();
            }
        }

        protected void MessageReceived(string s)
        {
            try
            {
                JSONNode node = JSON.Parse(s);
                if (node["posX"] != null)
                {
                    LCY.Utilities.InvokeMain(() =>
                    {
                        Matrix4x4 m = new Matrix4x4();
                        m.SetTRS(new Vector3(node["posX"].AsFloat, node["posY"].AsFloat, node["posZ"].AsFloat),
                            new Quaternion(node["rotX"].AsFloat, node["rotY"].AsFloat, node["rotZ"].AsFloat, node["rotW"].AsFloat), Vector3.one);
                        m.m02 = -m.m02;
                        m.m12 = -m.m12;
                        m.m20 = -m.m20;
                        m.m21 = -m.m21;
                        m.m23 = -m.m23;
                        HololensCamera.localToWorldMatrix = m;
                    }, false);
                }
                else if (node["command"].Value == "REINIT_CAMERA")
                {
                    Configurations.Instance.Set("*_StablizationInit", null);
                }
                else
                    AnnotationReceived(node);
            }
            catch (Exception e)
            {
                Utilities.LogException(e);
            }
        }

        protected void AnnotationReceived(JSONNode node)
        {
            string type = node["command"].Value;
            int id = node["id"].AsInt;
            LCY.Utilities.InvokeMain(() =>
            {
                switch (type)
                {
                    case "CreateAnnotationCommand":
                    case "UpdateAnnotationCommand":
                        Annotations.Add(id, ObjectFactory.NewAnnotation(node));
                        break;
                    case "DeleteAnnotationCommand":
                        Annotations.Remove(id);
                        break;
                    default:
                        throw new Exception("Unrecognized command type");
                }
                Annotations.Refresh();
            }, true);
        }
    }
}