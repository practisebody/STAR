using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WSAUnity;
using System;
using SimpleJSON;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Media.Core;
using Newtonsoft.Json.Linq;
using HoloPoseClient.Signalling;
#endif

namespace STAR
{
    public class ConnectionManager : UnitySingleton<ConnectionManager>
    {
        public Annotations Annotations;

        private void Start()
        {
            SocketStart();
            //WebRTCStart();
            WebRTCPoseStart();
        }

        private void Update()
        {
            WebRTCPoseUpdate();
        }

        protected void AnnotationReceived(string s)
        {
            try
            {
                JSONNode node = JSON.Parse(s);
                string type = node["command"].Value;
                int id = node["id"].AsInt;
                LCY.Utilities.InvokeMain(() =>
                {
                    switch (type)
                    {
                        case "CreateAnnotationCommand":
                        case "UpdateAnnotationCommand":
                            Annotations.Add(id, ObjectFactory.NewAnnotation(node["annotation_memory"]));
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
            catch (Exception e)
            {
                Utilities.LogException(e);
            }
        }

        #region socket

        protected string Hostname = "192.168.2.123";
        protected int Port = 8988;
        protected USocketClient Client = null;
        public bool SocketConnected { get { return Client?.Connected ?? false; } }

        protected void SocketStart()
        {
            Client = new USocketClient(Hostname, Port);
            Client.MessageReceived += AnnotationReceived;
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

        #endregion

        #region webrtc

        protected StarWebrtcContext StarWebrtcContext;
        protected bool WebRTCVerboseLog { get; set; }
        public bool WebRTCConnected { get; protected set; } = false;

        protected void WebRTCStart()
        {
            StarWebrtcContext = StarWebrtcContext.CreateAnnotationReceiverContext();
            //StarWebrtcContext.SignallingServerUrl = "https://webrtc-signal-iusm-study.herokuapp.com";
            Messenger.AddListener<string>(SympleLog.LogTrace, WebRTCLog);
            Messenger.AddListener<string>(SympleLog.LogDebug, WebRTCLog);
            Messenger.AddListener<string>(SympleLog.LogInfo, WebRTCLog);
            Messenger.AddListener<string>(SympleLog.LogError, WebRTCLog);
            Messenger.AddListener<string>(SympleLog.PeerAdded, (s) =>
            {
                Debug.Log(s + " connected");
                switch (s)
                {
                    case "star-mentor":
                        WebRTCConnected = true;
                        // remove all the annotations
                        Annotations.Clear();
                        LCY.Utilities.InvokeMain(() => Annotations.Refresh(), false);
                        break;
                }
            });
            Messenger.AddListener<string>(SympleLog.PeerRemoved, (s) =>
            {
                Debug.Log(s + " disconnected");
                switch (s)
                {
                    case "star-mentor":
                        WebRTCConnected = false;
                        break;
                }
            });
            Messenger.AddListener<string>(SympleLog.IncomingMessage, WebRTCReceived);
            Configurations.Instance.SetAndAddCallback("ConnectionWebRTC_VerboseLog", false, v => WebRTCVerboseLog = v, Configurations.CallNow.YES);
            Configurations.Instance.SetAndAddCallback("ConnectionWebRTC_Connect", true, v =>
            {
                if (v)
                    StarWebrtcContext.initAndStartWebRTC();
                else
                {
                    StarWebrtcContext.teardown();
                    WebRTCConnected = false;
                }
            }, Configurations.CallNow.YES);
        }

        protected void WebRTCLog(string s)
        {
            if (WebRTCVerboseLog)
            {
                Debug.Log(s);
            }
        }

        protected void WebRTCReceived(string s)
        {
            try
            {
#if NETFX_CORE
                JObject obj = JObject.Parse(s);
                string t = obj["message"].ToString();
                AnnotationReceived(t);
#endif
            }
            catch (Exception e)
            {
                Utilities.LogException(e);
            }
        }

        #endregion

        #region webrtcpose

        public enum Status
        {
            NotConnected,
            Connecting,
            Disconnecting,
            Connected,
            Calling,
            EndingCall,
            InCall
        }

        protected enum CommandType
        {
            Empty,
            SetNotConnected,
            SetConnected,
            SetInCall,
            AddRemotePeer,
            RemoveRemotePeer
        }

        protected struct Command
        {
            public CommandType type;
#if NETFX_CORE
            public Conductor.Peer remotePeer;
#endif
        }

        // make it a configuration
        protected string ServerAddress = "https://purduestarproj-webrtc-signal.herokuapp.com";
        protected string ServerPort = "443";
        protected string ClientName = "star-trainee"; // star-trainee, star-mentor, etc
        protected string PreferredVideoCodec = "VP8";

        public Status WebRTCStatus { get; private set; } = Status.NotConnected;
        protected List<Command> commandQueue = new List<Command>();
        //protected int selectedPeerIndex = -1;

        protected void WebRTCPoseStart()
        {
#if NETFX_CORE
            Conductor.Instance.LocalStreamEnabled = true;
            Debug.Log("setting up spatial coordinate system");
            IntPtr spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
            Conductor.Instance.InitializeSpatialCoordinateSystem(spatialCoordinateSystemPtr);

            Conductor.Instance.IncomingRawMessage += Conductor_IncomingRawMessage;
            Conductor.Instance.OnSelfRawFrame += Conductor_OnSelfRawFrame;
            //Conductor.Instance.OnPeerRawFrame += Conductor_OnPeerRawFrame;

            Conductor.Instance.Initialized += Conductor_Initialized;
            Conductor.Instance.Initialize(CoreApplication.MainView.CoreWindow.Dispatcher);
            Conductor.Instance.EnableLogging(Conductor.LogLevel.Verbose);
            Debug.Log("done setting up the rest of the conductor");

            Configurations.Instance.AddCallback("ConnectionWebRTC_Connect", () =>
            {
                if (WebRTCStatus == Status.NotConnected)
                {
                    new Task(() =>
                    {
                        Conductor.Instance.StartLogin(ServerAddress, ServerPort, ClientName);
                    }).Start();
                    WebRTCStatus = Status.Connecting;
                }
                else if (WebRTCStatus == Status.Connected)
                {
                    new Task(() =>
                    {
                        var task = Conductor.Instance.DisconnectFromServer();
                    }).Start();

                    WebRTCStatus = Status.Disconnecting;
                }
            });
            Configurations.Instance.AddCallback("ConnectionWebRTC_Call", () =>
            {
                if (WebRTCStatus == Status.Connected)
                {
                    new Task(() =>
                    {
                        // given the selectedPeerIndex, find which remote peer that matches. 
                        // Note: it's not just that index in Conductor.Instance.GetPeers() because that list contains both remote peers and ourselves.
                        Conductor.Peer selectedConductorPeer = null;

                        var conductorPeers = Conductor.Instance.GetPeers();
                        foreach (var conductorPeer in conductorPeers)
                        {
                            if (conductorPeer.Name != ClientName)
                            {
                                selectedConductorPeer = conductorPeer;
                                break;
                            }
                        }

                        if (selectedConductorPeer != null)
                        {
                            Conductor.Instance.ConnectToPeer(selectedConductorPeer);
                        }
                    }).Start();
                    WebRTCStatus = Status.Calling;
                }
                else if (WebRTCStatus == Status.InCall)
                {
                    new Task(() =>
                    {
                        var task = Conductor.Instance.DisconnectFromPeer();
                    }).Start();
                    WebRTCStatus = Status.EndingCall;
                }
            });
#endif
        }

        protected void WebRTCPoseUpdate()
        {
            lock (this)
            {
#if NETFX_CORE
                while (commandQueue.Count != 0)
                {
                    Command command = commandQueue.First();
                    commandQueue.RemoveAt(0);
                    if (command.type == CommandType.AddRemotePeer)
                    {
                        string remotePeerName = command.remotePeer.Name;
                        AddRemotePeer(remotePeerName);
                    }
                    else if (command.type == CommandType.RemoveRemotePeer)
                    {
                        string remotePeerName = command.remotePeer.Name;
                        RemoveRemotePeer(remotePeerName);
                    }
                }
#endif
            }
        }

#if NETFX_CORE
        protected IAsyncAction RunOnUiThread(Action fn)
        {
            return CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(fn));
        }
#endif

        protected void AddRemotePeer(string peerName)
        {
            bool isSelf = (peerName == ClientName); // when we connect, our own user appears as a peer. we don't want to accidentally try to call ourselves.

            Debug.Log("AddRemotePeer: " + peerName);
            //GameObject textItem = (GameObject)Instantiate(TextItemPrefab);

            //textItem.GetComponent<Text>().text = peerName;

            //if (isSelf)
            //{
            //    textItem.transform.SetParent(SelfConnectedAsContent, false);
            //}
            //else
            //{
            //    textItem.transform.SetParent(PeerContent, false);

            //    EventTrigger trigger = textItem.GetComponentInChildren<EventTrigger>();
            //    EventTrigger.Entry entry = new EventTrigger.Entry();
            //    entry.eventID = EventTriggerType.PointerDown;
            //    entry.callback.AddListener((data) => { OnRemotePeerItemClick((PointerEventData)data); });
            //    trigger.triggers.Add(entry);

            //    if (selectedPeerIndex == -1)
            //    {
            //        textItem.GetComponent<Text>().fontStyle = FontStyle.Bold;
            //        selectedPeerIndex = PeerContent.transform.childCount - 1;
            //    }
            //}
        }

        protected void RemoveRemotePeer(string peerName)
        {
            bool isSelf = (peerName == ClientName); // when we connect, our own user appears as a peer. we don't want to accidentally try to call ourselves.

            Debug.Log("RemoveRemotePeer: " + peerName);

            //if (isSelf)
            //{
            //    for (int i = 0; i < SelfConnectedAsContent.transform.childCount; i++)
            //    {
            //        if (SelfConnectedAsContent.GetChild(i).GetComponent<Text>().text == peerName)
            //        {
            //            SelfConnectedAsContent.GetChild(i).SetParent(null);
            //            break;
            //        }
            //    }
            //}
            //else
            //{
            //    for (int i = 0; i < PeerContent.transform.childCount; i++)
            //    {
            //        if (PeerContent.GetChild(i).GetComponent<Text>().text == peerName)
            //        {
            //            PeerContent.GetChild(i).SetParent(null);
            //            if (selectedPeerIndex == i)
            //            {
            //                if (PeerContent.transform.childCount > 0)
            //                {
            //                    PeerContent.GetChild(0).GetComponent<Text>().fontStyle = FontStyle.Bold;
            //                    selectedPeerIndex = 0;
            //                }
            //                else
            //                {
            //                    selectedPeerIndex = -1;
            //                }
            //            }
            //            break;
            //        }
            //    }
            //}
        }

        // fired whenever we encode one of our own video frames before sending it to the remote peer.
        // if there is pose data, posXYZ and rotXYZW will have non-zero values.
        protected void Conductor_OnSelfRawFrame(uint width, uint height,
                byte[] yPlane, uint yPitch, byte[] vPlane, uint vPitch, byte[] uPlane, uint uPitch,
                float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW)
        {
            Debug.Log("ControlScript: OnSelfRawFrame " + width + " " + height + " " + posX + " " + posY + " " + posZ + " " + rotX + " " + rotY + " " + rotZ + " " + rotW);
        }

        protected void Conductor_IncomingRawMessage(string rawMessageString)
        {
            Debug.Log("incoming raw message from peer: " + rawMessageString);
        }

        protected void Conductor_Initialized(bool succeeded)
        {
            if (succeeded)
            {
                Initialize();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Conductor initialization failed");
            }
        }

        protected void Initialize()
        {
#if NETFX_CORE
            // A Peer is connected to the server event handler
            Conductor.Instance.Signaller.OnPeerConnected += (peerId, peerName) =>
            {
                var task = RunOnUiThread(() =>
                {
                    lock (this)
                    {
                        Conductor.Peer peer = new Conductor.Peer { Id = peerId, Name = peerName };
                        Conductor.Instance.AddPeer(peer);
                        commandQueue.Add(new Command { type = CommandType.AddRemotePeer, remotePeer = peer });
                    }
                });
            };

            // A Peer is disconnected from the server event handler
            Conductor.Instance.Signaller.OnPeerDisconnected += peerId =>
            {
                var task = RunOnUiThread(() =>
                {
                    lock (this)
                    {
                        var peerToRemove = Conductor.Instance.GetPeers().FirstOrDefault(p => p.Id == peerId);
                        if (peerToRemove != null)
                        {
                            Conductor.Peer peer = new Conductor.Peer { Id = peerToRemove.Id, Name = peerToRemove.Name };
                            Conductor.Instance.RemovePeer(peer);
                            commandQueue.Add(new Command { type = CommandType.RemoveRemotePeer, remotePeer = peer });
                        }
                    }
                });
            };

            // The user is Signed in to the server event handler
            Conductor.Instance.Signaller.OnSignedIn += () =>
            {
                var task = RunOnUiThread(() =>
                {
                    lock (this)
                    {
                        if (WebRTCStatus == Status.Connecting)
                        {
                            WebRTCStatus = Status.Connected;
                            commandQueue.Add(new Command { type = CommandType.SetConnected });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Signaller.OnSignedIn() - wrong status - " + WebRTCStatus);
                        }
                    }
                });
            };

            // Failed to connect to the server event handler
            Conductor.Instance.Signaller.OnServerConnectionFailure += () =>
            {
                var task = RunOnUiThread(() =>
                {
                    lock (this)
                    {
                        if (WebRTCStatus == Status.Connecting)
                        {
                            WebRTCStatus = Status.NotConnected;
                            commandQueue.Add(new Command { type = CommandType.SetNotConnected });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Signaller.OnServerConnectionFailure() - wrong status - " + WebRTCStatus);
                        }
                    }
                });
            };

            // The current user is disconnected from the server event handler
            Conductor.Instance.Signaller.OnDisconnected += () =>
            {
                var task = RunOnUiThread(() =>
                {
                    lock (this)
                    {
                        if (WebRTCStatus == Status.Disconnecting)
                        {
                            WebRTCStatus = Status.NotConnected;
                            commandQueue.Add(new Command { type = CommandType.SetNotConnected });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Signaller.OnDisconnected() - wrong status - " + WebRTCStatus);
                        }
                    }
                });
            };

            Conductor.Instance.OnAddRemoteStream += Conductor_OnAddRemoteStream;
            Conductor.Instance.OnRemoveRemoteStream += Conductor_OnRemoveRemoteStream;
            Conductor.Instance.OnAddLocalStream += Conductor_OnAddLocalStream;

            // Connected to a peer event handler
            Conductor.Instance.OnPeerConnectionCreated += () =>
            {
                var task = RunOnUiThread(() =>
                {
                    lock (this)
                    {
                        if (WebRTCStatus == Status.Calling)
                        {
                            WebRTCStatus = Status.InCall;
                            commandQueue.Add(new Command { type = CommandType.SetInCall });
                        }
                        else if (WebRTCStatus == Status.Connected)
                        {
                            WebRTCStatus = Status.InCall;
                            commandQueue.Add(new Command { type = CommandType.SetInCall });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Conductor.OnPeerConnectionCreated() - wrong status - " + WebRTCStatus);
                        }
                    }
                });
            };

            // Connection between the current user and a peer is closed event handler
            Conductor.Instance.OnPeerConnectionClosed += () =>
            {
                var task = RunOnUiThread(() =>
                {
                    lock (this)
                    {
                        if (WebRTCStatus == Status.EndingCall)
                        {
                            Plugin.UnloadLocalMediaStreamSource();
                            Plugin.UnloadRemoteMediaStreamSource();
                            WebRTCStatus = Status.Connected;
                            commandQueue.Add(new Command { type = CommandType.SetConnected });
                        }
                        else if (WebRTCStatus == Status.InCall)
                        {
                            Plugin.UnloadLocalMediaStreamSource();
                            Plugin.UnloadRemoteMediaStreamSource();
                            WebRTCStatus = Status.Connected;
                            commandQueue.Add(new Command { type = CommandType.SetConnected });
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Conductor.OnPeerConnectionClosed() - wrong status - " + WebRTCStatus);
                        }
                    }
                });
            };

            // Ready to connect to the server event handler
            Conductor.Instance.OnReadyToConnect += () => { var task = RunOnUiThread(() => { }); };

            List<Conductor.IceServer> iceServers = new List<Conductor.IceServer>();
            iceServers.Add(new Conductor.IceServer { Host = "stun.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN });
            iceServers.Add(new Conductor.IceServer { Host = "stun1.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN });
            iceServers.Add(new Conductor.IceServer { Host = "stun2.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN });
            iceServers.Add(new Conductor.IceServer { Host = "stun3.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN });
            iceServers.Add(new Conductor.IceServer { Host = "stun4.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN });
            Conductor.IceServer turnServer = new Conductor.IceServer { Host = "turnserver3dstreaming.centralus.cloudapp.azure.com:5349", Type = Conductor.IceServer.ServerType.TURN };
            turnServer.Credential = "3Dtoolkit072017";
            turnServer.Username = "user";
            iceServers.Add(turnServer);
            Conductor.Instance.ConfigureIceServers(iceServers);

            var audioCodecList = Conductor.Instance.GetAudioCodecs();
            Conductor.Instance.AudioCodec = audioCodecList.FirstOrDefault(c => c.Name == "opus");
            System.Diagnostics.Debug.WriteLine("Selected audio codec - " + Conductor.Instance.AudioCodec.Name);

            var videoCodecList = Conductor.Instance.GetVideoCodecs();
            Conductor.Instance.VideoCodec = videoCodecList.FirstOrDefault(c => c.Name == PreferredVideoCodec);

            System.Diagnostics.Debug.WriteLine("Selected video codec - " + Conductor.Instance.VideoCodec.Name);

            uint preferredWidth = 1344;
            uint preferredHeght = 756;
            uint preferredFrameRate = 15;
            uint minSizeDiff = uint.MaxValue;
            Conductor.CaptureCapability selectedCapability = null;
            var videoDeviceList = Conductor.Instance.GetVideoCaptureDevices();
            foreach (Conductor.MediaDevice device in videoDeviceList)
            {
                Conductor.Instance.GetVideoCaptureCapabilities(device.Id).AsTask().ContinueWith(capabilities =>
                {
                    foreach (Conductor.CaptureCapability capability in capabilities.Result)
                    {
                        uint sizeDiff = (uint)Math.Abs(preferredWidth - capability.Width) + (uint)Math.Abs(preferredHeght - capability.Height);
                        if (sizeDiff < minSizeDiff)
                        {
                            selectedCapability = capability;
                            minSizeDiff = sizeDiff;
                        }
                        System.Diagnostics.Debug.WriteLine("Video device capability - " + device.Name + " - " + capability.Width + "x" + capability.Height + "@" + capability.FrameRate);
                    }
                }).Wait();
            }

            if (selectedCapability != null)
            {
                selectedCapability.FrameRate = preferredFrameRate;
                Conductor.Instance.VideoCaptureProfile = selectedCapability;
                Conductor.Instance.UpdatePreferredFrameFormat();
                System.Diagnostics.Debug.WriteLine("Selected video device capability - " + selectedCapability.Width + "x" + selectedCapability.Height + "@" + selectedCapability.FrameRate);
            }

#endif
        }

        protected void Conductor_OnAddRemoteStream()
        {
#if NETFX_CORE
            var task = RunOnUiThread(() =>
            {
                lock (this)
                {
                    if (WebRTCStatus == Status.InCall)
                    {
                        IMediaSource source;
                        if (Conductor.Instance.VideoCodec.Name == "H264")
                            source = Conductor.Instance.CreateRemoteMediaStreamSource("H264");
                        else
                            source = Conductor.Instance.CreateRemoteMediaStreamSource("I420");
                        Plugin.LoadRemoteMediaStreamSource((MediaStreamSource)source);
                    }
                    else if (WebRTCStatus == Status.Connected)
                    {
                        IMediaSource source;
                        if (Conductor.Instance.VideoCodec.Name == "H264")
                            source = Conductor.Instance.CreateRemoteMediaStreamSource("H264");
                        else
                            source = Conductor.Instance.CreateRemoteMediaStreamSource("I420");
                        Plugin.LoadRemoteMediaStreamSource((MediaStreamSource)source);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Conductor.OnAddRemoteStream() - wrong status - " + WebRTCStatus);
                    }
                }
            });
#endif
        }

        protected void Conductor_OnRemoveRemoteStream()
        {
#if NETFX_CORE
            var task = RunOnUiThread(() =>
            {
                lock (this)
                {
                    if (WebRTCStatus == Status.InCall)
                    {
                    }
                    else if (WebRTCStatus == Status.Connected)
                    {
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Conductor.OnRemoveRemoteStream() - wrong status - " + WebRTCStatus);
                    }
                }
            });
#endif
        }

        protected void Conductor_OnAddLocalStream()
        {
#if NETFX_CORE
            var task = RunOnUiThread(() =>
            {
                lock (this)
                {
                    if (WebRTCStatus == Status.InCall)
                    {
                        var source = Conductor.Instance.CreateLocalMediaStreamSource("I420");
                        Plugin.LoadLocalMediaStreamSource((MediaStreamSource)source);

                        Conductor.Instance.EnableLocalVideoStream();
                        Conductor.Instance.UnmuteMicrophone();
                    }
                    else if (WebRTCStatus == Status.Connected)
                    {
                        var source = Conductor.Instance.CreateLocalMediaStreamSource("I420");
                        Plugin.LoadLocalMediaStreamSource((MediaStreamSource)source);

                        Conductor.Instance.EnableLocalVideoStream();
                        Conductor.Instance.UnmuteMicrophone();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Conductor.OnAddLocalStream() - wrong status - " + WebRTCStatus);
                    }
                }
            });
#endif
        }

        protected static class Plugin
        {
            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "CreateLocalMediaPlayback")]
            internal static extern void CreateLocalMediaPlayback();

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "CreateRemoteMediaPlayback")]
            internal static extern void CreateRemoteMediaPlayback();

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "ReleaseLocalMediaPlayback")]
            internal static extern void ReleaseLocalMediaPlayback();

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "ReleaseRemoteMediaPlayback")]
            internal static extern void ReleaseRemoteMediaPlayback();

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "GetLocalPrimaryTexture")]
            internal static extern void GetLocalPrimaryTexture(UInt32 width, UInt32 height, out System.IntPtr playbackTexture);

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "GetRemotePrimaryTexture")]
            internal static extern void GetRemotePrimaryTexture(UInt32 width, UInt32 height, out System.IntPtr playbackTexture);

#if NETFX_CORE
            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "LoadLocalMediaStreamSource")]
            internal static extern void LoadLocalMediaStreamSource(MediaStreamSource IMediaSourceHandler);

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "UnloadLocalMediaStreamSource")]
            internal static extern void UnloadLocalMediaStreamSource();

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "LoadRemoteMediaStreamSource")]
            internal static extern void LoadRemoteMediaStreamSource(MediaStreamSource IMediaSourceHandler);

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "UnloadRemoteMediaStreamSource")]
            internal static extern void UnloadRemoteMediaStreamSource();
#endif

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "LocalPlay")]
            internal static extern void LocalPlay();

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "RemotePlay")]
            internal static extern void RemotePlay();

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "LocalPause")]
            internal static extern void LocalPause();

            [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall, EntryPoint = "RemotePause")]
            internal static extern void RemotePause();
        }

#endregion
    }
}