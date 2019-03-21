using LCY;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAR
{
    class SocketConnection : IConnection
    {
        protected string Hostname = "192.168.2.123";
        protected int Port = 8988;
        protected USocketClient Client = null;

        public string Name => "Socket";
        public bool Connected => Client?.Connected ?? false;

        public event MessageHandler OnMessageReceived
        {
            add
            {
                Client.OnMessageReceived += new LCY.USocketClient.MessageHandler(value);
            }
            remove
            {
                Client.OnMessageReceived -= new LCY.USocketClient.MessageHandler(value);
            }
        }

        public void Start()
        {
            Client = new USocketClient(Hostname, Port);
            Client.Persistent = true;
            Client.Timeout = 1000;
            Configurations.Instance.SetAndAddCallback("ConnectionSocket_IP", Hostname, v => Client.Host = v);
            Configurations.Instance.SetAndAddCallback("ConnectionSocket_Port", Port, v => Client.Port = v);
            Configurations.Instance.SetAndAddCallback("ConnectionSocket_Connect", false, v =>
            {
                if (v)
                    Client.Connect();
                else
                    Client.Close();
            });
        }

        public void Update()
        {
        }

    }
}
