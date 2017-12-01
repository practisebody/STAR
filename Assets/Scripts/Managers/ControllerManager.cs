using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LCY;
using SimpleJSON;
using System.Threading.Tasks;
using System.Threading;
using HoloToolkit.Unity.SpatialMapping;

namespace STAR
{
    public class ControllerManager : UnitySingleton<ControllerManager>
    {
        public int Port = 12345;
        protected USocketServer Server { get; set; }
        protected USocketClient Client { get; set; }
        public bool Connected { get { return Client?.Connected ?? false; } }

        private void Start()
        {
            // TODO(chengyuanlin) show my ip address
            //Debug.Log(UnityEngine.Networking.NetworkManager.singleton.networkAddress);

            Configurations.Instance["SendLog"] = true;
            Configurations.Instance.SetAndAddCallback("ShowSpatial", false, v => SpatialMappingManager.Instance.DrawVisualMeshes = v, Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);

            Server = new USocketServer(Port);
            Server.ConnectionReceived += ConnectionReceived;
            Server.Listen();

            /*Task.Run(async () =>
            {
                while (true)
                {
                    SpinWait.SpinUntil(() => Configurations.Instance.Get<bool>("SendLog"));
                    Send("LOG", DebugInfoManager.Instance.LogString);
                    Send("STATUS", DebugInfoManager.Instance.StatusString);
                    await Task.Delay(1000);
                }
            });*/
        }

        protected void Send(string type, string s)
        {
            Task.Run(async () =>
            {
                await Client.SendAsync(type);
                await Client.SendAsync(s);
                await Client.SendAsync("END");
            }).Wait();
        }

        protected void ConnectionReceived(USocketServer server, USocketClient client)
        {
            Client = client;
            Client.MessageReceived += MessageReceived;
            Client.BeginRead();
            Send("CONTROL", Configurations.Instance.ToString(":", ";"));
        }

        protected void MessageReceived(string s)
        {
            try
            {
                JSONClass json = JSON.Parse(s).AsObject;
                foreach (string key in json.Keys)
                {
                    Configurations.Instance.Set(key, json[key].Value);
                }
            }
            catch (Exception e)
            {
                UDebug.LogException(e);
            }
        }
    }
}