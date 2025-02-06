using System;
using AOT;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using StinkySteak.SimulationTimer;
using StinkySteak.WebRealtimeCommunication;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class BrowserWebRTCPeer : BaseWebRTCPeer
    {
        private static BrowserWebRTCPeer Instance { get; set; }

        private WebSocketClientSignalingService _signalingServiceClient;
        private WebRTCEndPoint _endPoint = new();

        private string _offer;
        private string _answer;

        private RunMode _peerMode;

        private string[] _iceServers;
        private float _timeoutDuration;

        public override IEndPoint EndPoint => _endPoint;

        private bool _isTimedOut;
        public override bool IsConnectionOpen => Browser.WebRTC_IsConnectionOpen();
        public override bool IsTimedOut => _isTimedOut;

        private SimulationTimer _timerTimeout;

        private bool _sentIceGatheringComplete;
        private int _connectionId;
        private WebSocketSignalingServer _signalingServiceServer;

        public override void Start(RunMode runMode)
        {
            Instance = this;

            _peerMode = runMode;

            if (_peerMode == RunMode.Server)
            {
                ConstructRTCPeerConnection();
            }
            else if (_peerMode == RunMode.Client)
            {
                _signalingServiceClient = new WebSocketClientSignalingService();
            }
        }

        private StringEnumConverter _jsonSettings = new StringEnumConverter()
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        private void ConstructRTCPeerConnection()
        {
            BrowserRTCConfiguration config = GetSelectedSdpSemantics();

            Browser.WebRTC_CreateRTCPeerConnection(config);
            Browser.WebRTC_SetCallbackOnIceCandidate(OnIceCandidate);
            Browser.WebRTC_SetCallbackOnIceConnectionStateChange(OnIceConnectionChanged);
            Browser.WebRTC_SetCallbackOnDataChannel(OnDataChannel);
            Browser.WebRTC_SetCallbackOnIceCandidateGatheringState(OnIceGatheringStateChanged);
            Browser.WebRTC_SetCallbackOnChannelOpen(OnChannelOpen);
        }

        [MonoPInvokeCallback(typeof(OnIceCandidate))]
        private static void OnIceCandidate()
        {
            Instance.Log("OnIceCandidate");
        }

        [MonoPInvokeCallback(typeof(OnIceConnectionStateChange))]
        private static void OnIceConnectionChanged()
        {
            Instance.Log("OnIceConnectionChanged");
        }

        [MonoPInvokeCallback(typeof(OnIceCandidateGatheringState))]
        private static void OnIceGatheringStateChanged(int state)
        {
            BrowserRTCIceGatheringState rtcState = (BrowserRTCIceGatheringState)state;
        }

        [MonoPInvokeCallback(typeof(OnChannelOpen))]
        private static void OnChannelOpen()
        {
            string remoteDescription = Browser.WebRTC_GetRemoteDescription();

            SDPParser.ParseSDP(remoteDescription, out string ip, out int port);

            Instance._endPoint.Init(ip, port);
        }

        [MonoPInvokeCallback(typeof(OnDataChannel))]
        private static void OnDataChannel()
        {
            Instance.Log("OnIceConnectionChanged");

            string remoteDescription = Browser.WebRTC_GetRemoteDescription();

            SDPParser.ParseSDP(remoteDescription, out string ip, out int port);

            Instance._endPoint.Init(ip, port);
        }

        [MonoPInvokeCallback(typeof(OnMessageCallback))]
        private static void OnMessage(IntPtr ptr, int length)
        {
            Instance.BroadcastOnMessageUnmanaged(ptr, length);
        }

        public override void PollUpdate()
        {
            if (_peerMode == RunMode.Client)
            {
                if (_timerTimeout.IsExpired())
                {
                    _timerTimeout = SimulationTimer.None;
                    _isTimedOut = true;
                    BroadcastOnTimeout();
                    return;
                }

                _signalingServiceClient.PollUpdate();

                // Client
                PollOpCreateOffer();
                PollOpSetLocalOffer();
                PollOpSetRemoteAnswer();
            }

            if (_peerMode == RunMode.Server)
            {
                // Server
                PollOpSetRemoteOffer();
                PollOpCreateAnswer();
                PollOpSetLocalAnswer();
            }

            // All
            PollIceCandidate();
        }

        private void PollOpSetLocalAnswer()
        {
            if (!Browser.WebRTC_HasOpSetLocalDescription()) return;

            if (Browser.WebRTC_IsOpSetLocalDescriptionDone())
            {
                Log("Answer has been set to local description!");

                Browser.WebRTC_DisposeOpSetLocalDescription();
            }
        }

        private void Log(string msg)
        {
            Debug.Log($"[{this}]: {msg}");
        }

        private void PollOpCreateAnswer()
        {
            if (!Browser.WebRTC_HasOpCreateAnswer()) return;

            if (Browser.WebRTC_GetOpCreateAnswerIsDone())
            {
                Log("Answer is created. Setting it as local description...");

                string answer = Browser.WebRTC_GetAnswer();

                Browser.WebRTC_SetLocalDescription(answer);

                Browser.WebRTC_DisposeOpCreateAnswer();
            }
        }

        private void PollOpSetRemoteOffer()
        {
            if (!Browser.WebRTC_HasOpSetRemoteDescription()) return;

            if (Browser.WebRTC_IsOpSetRemoteDescriptionDone())
            {
                Log("Offer has been set to remote description. Creating an answer...");

                Browser.WebRTC_CreateAnswer();

                Browser.WebRTC_DisposeOpSetRemoteDescription();
            }
        }

        private void PollOpSetLocalOffer()
        {
            if (!Browser.WebRTC_HasOpSetLocalDescription()) return;

            if (Browser.WebRTC_IsOpSetLocalDescriptionDone())
            {
                Log("Offer has been set to local!");

                Browser.WebRTC_DisposeOpSetLocalDescription();
            }
        }

        private void PollOpSetRemoteAnswer()
        {
            if (!Browser.WebRTC_HasOpSetRemoteDescription()) return;

            if (Browser.WebRTC_IsOpSetRemoteDescriptionDone())
            {
                Log("Answer has been set to remote description");

                Browser.WebRTC_DisposeOpSetRemoteDescription();
            }
        }

        private void PollOpCreateOffer()
        {
            if (!Browser.WebRTC_HasOpCreateOffer()) return;

            if (Browser.WebRTC_GetOpCreateOfferIsDone())
            {
                Log("Offer Created. Setting it to local...");
                string offer = Browser.WebRTC_GetOffer();

                Browser.WebRTC_SetLocalDescription(offer);

                Browser.WebRTC_DisposeOpCreateOffer();
            }
        }

        private void PollIceCandidate()
        {
            if (!Browser.WebRTC_GetIsPeerConnectionCreated()) return;

            if (_sentIceGatheringComplete) return;

            if (Browser.WebRTC_GetGatheringState() == BrowserRTCIceGatheringState.Complete)
            {
                _sentIceGatheringComplete = true;

                if (_peerMode == RunMode.Client)
                {
                    Log("Sending offer to the server...");
                    _offer = Browser.WebRTC_GetLocalDescription();
                    SendOfferToServer();
                }
                if (_peerMode == RunMode.Server)
                {
                    Log("Sending answer to the client...");
                    _answer = Browser.WebRTC_GetLocalDescription();
                    SendAnswerToClient();
                }
            }
        }

        private void SendAnswerToClient()
        {
            _signalingServiceServer.SendAnswerToClient(_connectionId, _answer);
        }

        private void SendOfferToServer()
        {
            _signalingServiceClient.OnServerAnswered += OnServerAnswered;

            _signalingServiceClient.SendOffer(_offer);
        }

        private void OnServerAnswered(int clientId, string message)
        {
            _connectionId = clientId;

            //BrowserRTCSessionDescription sdp = JsonConvert.DeserializeObject<BrowserRTCSessionDescription>(message, _jsonSettings);

            Browser.WebRTC_SetRemoteDescription(message);
        }

        public override void SetConfig(string[] iceServers, float timeoutDuration)
        {
            _iceServers = iceServers;
            _timeoutDuration = timeoutDuration;
        }

        public override void Connect(string address, int port)
        {
            _signalingServiceClient.OnConnectedToServer += OnConnectedToServer;
            _signalingServiceClient.Start();

            _signalingServiceClient.Connect(address, port);

            _timerTimeout = SimulationTimer.CreateFromSeconds(_timeoutDuration);
        }

        private void OnConnectedToServer()
        {
            ConstructRTCPeerConnection();

            Browser.WebRTC_CreateDataChannel();
            Browser.WebRTC_SetCallbackOnMessage(OnMessage);

            Browser.WebRTC_CreateOffer();
        }


        private BrowserRTCConfiguration GetSelectedSdpSemantics()
        {
            BrowserRTCIceServer iceServer = new()
            {
                urls = _iceServers,
            };

            BrowserRTCConfiguration config = default;
            config.iceServers = new[]
            {
                iceServer
            };

            return config;
        }

        public override void Send(IntPtr ptr, int length)
        {
            Browser.WebRTC_DataChannelSend(ptr, length);
        }

        public override void CloseConnection()
        {
            Browser.WebRTC_CloseConnection();
        }

        public override void SetSignalingServer(WebSocketSignalingServer signalingServer)
        {
            Debug.LogError($"This method is not supported for browser webRTC");
        }

        public override void OnReceivedOfferFromClient(string offer)
        {
            Debug.LogError($"This method is not supported for browser webRTC");
        }

        public override void SetConnectionId(int id)
        {
            _connectionId = id;
        }
    }
}
