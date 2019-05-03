using LCY;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STAR
{
    public class SocketConnection : IConnection
    {
        protected string Hostname = "192.168.2.123";
        protected int Port = 8988;
        protected USocketClient Client = null;

        public string Name => "Socket";
        public bool Connected => Client?.Connected ?? false;

        public string StatusInfo => Connected.ToString();

        public event MessageHandler OnMessageReceived
        {
            add
            {
                if (Client == null)
                    _OnMessageReceived += value;
                else
                    Client.OnMessageReceived += new USocketClient.MessageHandler(value);
            }
            remove
            {
                if (Client == null)
                    _OnMessageReceived -= value;
                else
                    Client.OnMessageReceived -= new USocketClient.MessageHandler(value);
            }
        }
        protected event MessageHandler _OnMessageReceived;

        public void Start()
        {
            Client = new USocketClient(Hostname, Port)
            {
                Persistent = true,
                Timeout = 1000
            };
            foreach (Delegate del in _OnMessageReceived.GetInvocationList())
                Client.OnMessageReceived += new USocketClient.MessageHandler((MessageHandler)del);
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
