using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.Foundation;
using Windows.Networking;
using Windows.Networking.Sockets;
#else
using System.Net.Sockets;
#endif

namespace LCY
{
    /// <summary>
    /// Universal socket client that works in both Unity editor and Hololens.
    /// </summary>
    public sealed class USocketClient
    {
#if NETFX_CORE
        private StreamSocket Client { get; set; }
#else
        private TcpClient Client { get; set; }
#endif

        public string Host { get; set; }
        public int Port { get; set; }

        public delegate void MessageHandler(string s);
        public event MessageHandler OnMessageReceived;
        public delegate void DisconnectionHandler();
        public event DisconnectionHandler OnDisconnected;

        public bool Persistent { get; set; } = false;
        public bool Connected { get; private set; }

        public int Timeout { get; set; } = 2000;
        private CancellationTokenSource TokenSource { get; set; }

        public Encoding Encoding { get; set; } = Encoding.ASCII;
        public int BufferSize { get; set; } = 1048576;

        private const int READ_BUFFER_SIZE = 1048576;
        private byte[] readBuffer = new byte[READ_BUFFER_SIZE];

#if !NETFX_CORE
        private bool ShuttingDown { get; set; } = false;
#endif

        private StreamReader Reader { get; set; }
        private StreamWriter Writer { get; set; }

#if NETFX_CORE
        public USocketClient(StreamSocket client)
#else
        public USocketClient(TcpClient client)
#endif
        {
            Client = client;
            PostConnect();
        }

        public USocketClient(string host, int port)
        {
            Host = host;
            Port = port;
            Connected = false;
        }

        public async void Connect()
        {
#if NETFX_CORE
            Client = new StreamSocket();
#else
            Client = new TcpClient(Host, Port);
#endif
            do
            {
                try
                {
#if NETFX_CORE
                    TokenSource = new CancellationTokenSource(Timeout);
                    IAsyncAction action = Client.ConnectAsync(new HostName(Host), Port.ToString());
                    await action.AsTask(TokenSource.Token);
#else
#endif
                    PostConnect();
                    BeginRead();
                    break;
                }
                catch (Exception e)
                {
                    if (Persistent == false)
                    {
                        UDebug.LogException(e);
                        Close();
                    }
                }
            } while (Persistent);
        }

        private void PostConnect()
        {
#if NETFX_CORE
            Reader = new StreamReader(Client.InputStream.AsStreamForRead(), Encoding, false, BufferSize);
            Writer = new StreamWriter(Client.OutputStream.AsStreamForWrite(), Encoding, BufferSize);
#else
#endif
            Connected = true;
        }

#if NETFX_CORE
        public async void BeginRead()
        {
            try
            {
                while (true)
                {
                    string response = await Reader.ReadLineAsync();
                    if (response == null)
                        break;
                    else
                        OnMessageReceived?.Invoke(response);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                Connected = false;
                OnDisconnected?.Invoke();
                if (Persistent)
                    Connect();
                else
                    Close();
            }
        }
#else
        public void BeginRead()
        {
            Client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(ReadCallBack), null);
        }

        private void ReadCallBack(IAsyncResult ar)
        {
            try
            {
                int BytesRead;
                BytesRead = Client.GetStream().EndRead(ar);
                if (BytesRead < 1)
                {
                    if (!ShuttingDown)
                        Close();
                    return;
                }
                string incomingCommandJsonString = Encoding.ASCII.GetString(readBuffer, 0, BytesRead);

                if (BytesRead > 0)
                {
                    try
                    {
                        OnMessageReceived?.Invoke(incomingCommandJsonString);
                    }
                    catch (Exception e)
                    {
                        UDebug.Log("exception when attempting to process command, throwing away command");
                        UDebug.Log(e.Message);
                        UDebug.Log(e.StackTrace);
                    }
                }
                Client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(ReadCallBack), null);
            }
            catch (Exception e)
            {
                UDebug.Log("got exception: " + e.Message);
                UDebug.Log(e.StackTrace);
                if (Persistent)
                    Connect();
                else
                    Close();
            }
        }
#endif

        public async Task SendAsync(string s)
        {
#if NETFX_CORE
            try
            {
                await Writer.WriteLineAsync(s);
                await Writer.FlushAsync();
            }
            catch (Exception)
            {
            }
#else
#endif
        }

        public void Close()
        {
            Persistent = false;
#if NETFX_CORE
            TokenSource?.Cancel();
            Client?.Dispose();
            Reader = null;
            Writer = null;
            Client = null;
#else
            ShuttingDown = true;
            if (Client != null)
            {
                if (Client.Connected)
                {
                    //Client.GetStream ().Close ();
                    Client.Close();
                }
            }
#endif
                Connected = false;
        }
    }
}
