using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if NETFX_CORE
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#else
using System.Net;
using System.Net.Sockets;
using System.Threading;
#endif

namespace LCY
{
    /// <summary>
    /// Universal socket server that works in both Unity editor and Hololens.
    /// Return a USocketClient for each incoming socket.
    /// </summary>
    public sealed class USocketServer
    {
#if NETFX_CORE
        StreamSocketListener listener;
#else
#endif

        public int Port { get; set; }

        public delegate void OnConnection(USocketServer server, USocketClient client);
        public event OnConnection ConnectionReceived;

        public USocketServer(int port)
        {
            this.Port = port;
        }

        public async void Listen()
        {
#if NETFX_CORE
            listener = new StreamSocketListener();
            listener.ConnectionReceived += NewConnection;
            listener.Control.KeepAlive = false;

            try
            {
                await listener.BindServiceNameAsync(Port.ToString());
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                UDebug.LogException(exception);
            }
#else
            TcpListener server = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                server = new TcpListener(localAddr, Port);

                // start listening for client requests.
                server.Start();

                // enter the listening loop
                while (true)
                {
                    // perform a blocking call to accept requests.
                    using (TcpClient tcpClient = server.AcceptTcpClient())
                    {
                        USocketClient client = new USocketClient(tcpClient);
                        ConnectionReceived?.Invoke(this, client);
                    }
                }
            }
            catch (Exception e)
            {
                UDebug.LogException(e);
            }
            finally
            {
                // stop listening for new clients
                server.Stop();
            }
#endif
        }

#if NETFX_CORE
        private void NewConnection(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            USocketClient client = new USocketClient(args.Socket);
            ConnectionReceived?.Invoke(this, client);
        }
#endif

        public void Close()
        {
#if NETFX_CORE
            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }
#else
#endif
        }
    }
}