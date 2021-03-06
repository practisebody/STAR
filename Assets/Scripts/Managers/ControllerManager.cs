﻿using System;
using UnityEngine;
using LCY;
using SimpleJSON;
using HoloToolkit.Unity.SpatialMapping;
#if NETFX_CORE
using System.Threading.Tasks.Dataflow;
#endif
using System.Threading.Tasks;

namespace STAR
{
    /// <summary>
    /// Controller that wraps around Configurations, and connects to the controller app
    /// </summary>
    public class ControllerManager : UnitySingleton<ControllerManager>
    {
        // listening to 12345 port
        public int Port = 12345;
        protected USocketServer Server { get; set; }
        protected USocketClient Client { get; set; }
        public bool Connected { get { return Client?.Connected ?? false; } }
#if NETFX_CORE
        public BufferBlock<string> buffer = new BufferBlock<string>();
#endif
        public object bufferLock = new object();

        public HoloInfo Info;
        public GameObject Cursor;
        public GameObject Fps;

        private void Start()
        {
#if NETFX_CORE
            Task.Run(() => Sender(buffer));
#endif
            Configurations.Instance.SetAndAddCallback("SpatialMap_Show", false, v => SpatialMappingManager.Instance.DrawVisualMeshes = v, Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("SpatialMap_Update", true, v =>
            {
                if (v)
                    SpatialMappingManager.Instance.StartObserver();
                else
                    SpatialMappingManager.Instance.StopObserver();
            }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Visual_Cursor", false, v => Cursor.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Visual_FPSCounter", false, v => Fps.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);

            Configurations.Instance.AddCallback("*_PrepareUI", () =>
            {
                Configurations.Instance.Set("SpatialMap_Show", false);
                Configurations.Instance.Set("SpatialMap_Update", false);
                Configurations.Instance.Set("Visual_Cursor", false);
                Configurations.Instance.Set("Visual_FPSCounter", false);
            });

            Server = new USocketServer(Port);
            Server.ConnectionReceived += ConnectionReceived;
            Server.Listen();
        }

        protected void Send(string type, string s)
        {
#if NETFX_CORE
            if (Connected)
            {
                lock (bufferLock)
                {
                    buffer.Post(type);
                    buffer.Post(s);
                    buffer.Post("END");
                }
            }
#endif
        }

        public void SendControl()
        {
            Send("CONTROL", Configurations.Instance.ToString(":", ";"));
        }

        public void SendLog()
        {
            Send("LOG", Info.LogString);
        }

        public void SendStatus()
        {
            Send("STATUS", Info.StatusString);
        }

#if NETFX_CORE
        public async Task Sender(ISourceBlock<string> source)
        {
            while (await source.OutputAvailableAsync())
            {
                try
                {
                    string data = source.Receive();
                    await Client.SendAsync(data);
                }
                catch (Exception)
                {
                }
            }
        }
#endif

        protected void ConnectionReceived(USocketServer server, USocketClient client)
        {
            Client?.Close();
            Client = client;
            Client.OnMessageReceived += MessageReceived;
            Client.BeginRead();
            SendControl();
            SendLog();
            SendStatus();
        }

        /// <summary>
        /// When a message arrived, update corresponding configuration
        /// [internal use]
        /// </summary>
        protected void MessageReceived(string s)
        {
            try
            {
                Debug.Log(s);
                JSONClass json = JSON.Parse(s).AsObject;
                foreach (string key in json.Keys)
                {
                    Configurations.Instance[key] = json[key].Value;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}