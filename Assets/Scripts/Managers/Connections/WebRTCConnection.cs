﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LCY;
using System.Runtime.InteropServices;
using System.IO;
#if NETFX_CORE
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Media.Core;
using HoloPoseClient.Signalling;
#endif

namespace STAR
{
    class WebRTCConnection : IConnection
    {
        public enum Statuses
        {
            NotConnected,
            Pending,
            Connected,
        }
        
        public enum WebRTCStatuses
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

        public string Name => "WebRTC";
        public bool Connected => WebRTCStatus == WebRTCStatuses.Connected;

        public event MessageHandler OnMessageReceived;

        public Statuses Status
        {
            get
            {
                switch (WebRTCStatus)
                {
                    case WebRTCStatuses.NotConnected:
                        return Statuses.NotConnected;
                    case WebRTCStatuses.Connecting:
                    case WebRTCStatuses.Disconnecting:
                    case WebRTCStatuses.Calling:
                    case WebRTCStatuses.EndingCall:
                        return Statuses.Pending;
                    case WebRTCStatuses.Connected:
                    case WebRTCStatuses.InCall:
                        return PeerName == null ? Statuses.Pending : Statuses.Connected;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
        public WebRTCStatuses WebRTCStatus { get; set; } = WebRTCStatuses.NotConnected;

        // parameters
        protected string ServerAddress = "https://purduestarproj-webrtc-signal.herokuapp.com";
        protected string ServerPort = "443";
        protected string ClientName = "star-trainee"; // star-trainee, star-mentor, etc
        protected string PreferredVideoCodec = "VP8";

        protected List<Command> commandQueue = new List<Command>();
        public string PeerName { get; protected set; } = null;

        // video saver
        protected bool VideoPose = false;
        protected FileStream VideoFrames;
        protected BinaryWriter VideoBinaryWriter;

        public void Start()
        {
#if NETFX_CORE
            Conductor.Instance.LocalStreamEnabled = true;
            Debug.Log("setting up spatial coordinate system");
            IntPtr spatialCoordinateSystemPtr = UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
            Conductor.Instance.InitializeSpatialCoordinateSystem(spatialCoordinateSystemPtr);

            Conductor.Instance.IncomingRawMessage += Conductor_IncomingRawMessage;
            Conductor.Instance.OnSelfRawFrame += Conductor_OnSelfRawFrame;

            Conductor.Instance.Initialized += Conductor_Initialized;
            Conductor.Instance.Initialize(CoreApplication.MainView.CoreWindow.Dispatcher);
            Conductor.Instance.EnableLogging(Conductor.LogLevel.Verbose);
            Debug.Log("done setting up the rest of the conductor");

            Configurations.Instance.AddCallback("*_Connect", () =>
            {
                if (WebRTCStatus == WebRTCStatuses.NotConnected)
                {
                    new Task(() =>
                    {
                        Conductor.Instance.StartLogin(ServerAddress, ServerPort, ClientName);
                    }).Start();
                    WebRTCStatus = WebRTCStatuses.Connecting;
                }
                else if (WebRTCStatus == WebRTCStatuses.Connected)
                {
                    new Task(() =>
                    {
                        var task = Conductor.Instance.DisconnectFromServer();
                    }).Start();

                    WebRTCStatus = WebRTCStatuses.Disconnecting;
                }
            });
            Configurations.Instance.AddCallback("*_Call", () =>
            {
                if (WebRTCStatus == WebRTCStatuses.Connected && PeerName != null)
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
                    WebRTCStatus = WebRTCStatuses.Calling;
                }
                else if (WebRTCStatus == WebRTCStatuses.InCall)
                {
                    new Task(() =>
                    {
                        var task = Conductor.Instance.DisconnectFromPeer();
                    }).Start();
                    WebRTCStatus = WebRTCStatuses.EndingCall;
                }
            });
#endif
            Configurations.Instance.SetAndAddCallback("Stabilization_SavePose", VideoPose, (v) =>
            {
                if (VideoPose = v)
                {
                    VideoFrames = File.Create(Utilities.FullPath(Utilities.TimeNow() + ".txt"));
                    VideoBinaryWriter = new BinaryWriter(VideoFrames);
                }
                else
                {
                    VideoBinaryWriter.Dispose();
                    VideoFrames.Dispose();
                }
            }, Configurations.RunOnMainThead.YES, Configurations.WaitUntilDone.YES);

            Configurations.Instance.AddCallback("Annotation_DummyDense", () =>
            {
                StringBuilder sb = new StringBuilder(2500);
                sb.Append("{\"id\":0,\"command\":\"CreateAnnotationCommand\",\"annotation_memory\":{\"annotation\":{\"annotationPoints\":[");
                sb.Append("{\"x\":0.0,\"y\":0.0}");
                for (int i = 1; i <= 100; ++i)
                {
                    sb.Append(",{\"x\":").Append(i / 100.0f).Append(",\"y\":").Append(i / 100.0f).Append("}");
                }
                sb.Append("],\"annotationType\":\"polyline\"}}}");
                OnMessageReceived(sb.ToString());
            }, Configurations.RunOnMainThead.NO);
        }

        public void Update()
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
                    else if (command.type == CommandType.SetInCall)
                    {
                        Configurations.Instance.Invoke("*_PrepareUI");
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
            Debug.Log("AddRemotePeer: " + peerName);
            if (peerName != ClientName)
                PeerName = peerName;
        }

        protected void RemoveRemotePeer(string peerName)
        {
            Debug.Log("RemoveRemotePeer: " + peerName);
            PeerName = null;
        }

        // fired whenever we encode one of our own video frames before sending it to the remote peer.
        // if there is pose data, posXYZ and rotXYZW will have non-zero values.
        protected void Conductor_OnSelfRawFrame(uint width, uint height,
            byte[] yPlane, uint yPitch, byte[] vPlane, uint vPitch, byte[] uPlane, uint uPitch,
            float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW)
        {
            if (VideoPose)
            {
                LCY.Utilities.InvokeMain(() =>
                {
                    Matrix4x4 m = HololensCamera.TheCamera.transform.localToWorldMatrix;
                    for (int i = 0; i < 16; ++i)
                        VideoBinaryWriter.Write(m[i]);
                }, true);
            }
        }

        protected void Conductor_IncomingRawMessage(string rawMessageString)
        {
            Debug.Log("incoming raw message from peer: " + rawMessageString);
            OnMessageReceived(rawMessageString);
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
                        if (WebRTCStatus == WebRTCStatuses.Connecting)
                        {
                            WebRTCStatus = WebRTCStatuses.Connected;
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
                        if (WebRTCStatus == WebRTCStatuses.Connecting)
                        {
                            WebRTCStatus = WebRTCStatuses.NotConnected;
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
                        if (WebRTCStatus == WebRTCStatuses.Disconnecting)
                        {
                            WebRTCStatus = WebRTCStatuses.NotConnected;
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
                        if (WebRTCStatus == WebRTCStatuses.Calling)
                        {
                            WebRTCStatus = WebRTCStatuses.InCall;
                            commandQueue.Add(new Command { type = CommandType.SetInCall });
                        }
                        else if (WebRTCStatus == WebRTCStatuses.Connected)
                        {
                            WebRTCStatus = WebRTCStatuses.InCall;
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
                        if (WebRTCStatus == WebRTCStatuses.EndingCall)
                        {
                            Plugin.UnloadLocalMediaStreamSource();
                            Plugin.UnloadRemoteMediaStreamSource();
                            WebRTCStatus = WebRTCStatuses.Connected;
                            commandQueue.Add(new Command { type = CommandType.SetConnected });
                        }
                        else if (WebRTCStatus == WebRTCStatuses.InCall)
                        {
                            Plugin.UnloadLocalMediaStreamSource();
                            Plugin.UnloadRemoteMediaStreamSource();
                            WebRTCStatus = WebRTCStatuses.Connected;
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

            List<Conductor.IceServer> iceServers = new List<Conductor.IceServer>
            {
                new Conductor.IceServer { Host = "stun.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN },
                new Conductor.IceServer { Host = "stun1.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN },
                new Conductor.IceServer { Host = "stun2.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN },
                new Conductor.IceServer { Host = "stun3.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN },
                new Conductor.IceServer { Host = "stun4.l.google.com:19302", Type = Conductor.IceServer.ServerType.STUN }
            };
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
            uint preferredFrameRate = 30;
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
                    if (WebRTCStatus == WebRTCStatuses.InCall)
                    {
                        IMediaSource source;
                        if (Conductor.Instance.VideoCodec.Name == "H264")
                            source = Conductor.Instance.CreateRemoteMediaStreamSource("H264");
                        else
                            source = Conductor.Instance.CreateRemoteMediaStreamSource("I420");
                        Plugin.LoadRemoteMediaStreamSource((MediaStreamSource)source);
                    }
                    else if (WebRTCStatus == WebRTCStatuses.Connected)
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
                    if (WebRTCStatus == WebRTCStatuses.InCall)
                    {
                    }
                    else if (WebRTCStatus == WebRTCStatuses.Connected)
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
                    if (WebRTCStatus == WebRTCStatuses.InCall)
                    {
                        var source = Conductor.Instance.CreateLocalMediaStreamSource("I420");
                        Plugin.LoadLocalMediaStreamSource((MediaStreamSource)source);

                        Conductor.Instance.EnableLocalVideoStream();
                        Conductor.Instance.UnmuteMicrophone();
                    }
                    else if (WebRTCStatus == WebRTCStatuses.Connected)
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
    }
}
