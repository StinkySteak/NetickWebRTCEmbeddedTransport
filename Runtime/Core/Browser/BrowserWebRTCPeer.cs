using System;
using AOT;
using StinkySteak.Timer;
using StinkySteak.WebRealtimeCommunication;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class BrowserWebRTCPeer : BaseWebRTCPeer
    {
        private static BrowserWebRTCPeer Instance { get; set; }

        private WebSocketSignalingServer _signalingServiceServer;
        private WebSocketClientSignalingService _signalingServiceClient;
        private WebRTCEndPoint _endPoint = new();

        private int _connectionId;
        private string _offer;
        private string _answer;
        private RunMode _peerMode;
        private bool _hasSentIceGatheringComplete;
        private bool _isTimedOut;

        private FlexTimer _timerLocalTimeout;
        private FlexTimer _timerIceTrickling;

        private UserRTCConfig _userRTCConfig;
        private WebSocketSignalingConfig _webSocketSignalingConfig;

        public override IEndPoint EndPoint => _endPoint;
        public override bool IsConnectionOpen => Browser.WebRTC_IsConnectionOpen();
        public override bool IsTimedOut => _isTimedOut;

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
            Instance._timerLocalTimeout = FlexTimer.None;

            string remoteDescription = Browser.WebRTC_GetRemoteDescription();

            SDPParser.ParseSDP(remoteDescription, out string ip, out int port);

            Instance._endPoint.Init(ip, port);
        }

        [MonoPInvokeCallback(typeof(OnDataChannel))]
        private static void OnDataChannel()
        {
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
                if (_timerLocalTimeout.IsExpired())
                {
                    _timerLocalTimeout = FlexTimer.None;
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

                if (_userRTCConfig.IceTricklingConfig.IsManual)
                    _timerIceTrickling = FlexTimer.CreateFromSeconds(_userRTCConfig.IceTricklingConfig.Duration);

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

                if (_userRTCConfig.IceTricklingConfig.IsManual)
                    _timerIceTrickling = FlexTimer.CreateFromSeconds(_userRTCConfig.IceTricklingConfig.Duration);

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

            if (_hasSentIceGatheringComplete) return;

            if (Browser.WebRTC_GetGatheringState() == BrowserRTCIceGatheringState.Complete || _timerIceTrickling.IsExpired())
            {
                _timerIceTrickling = FlexTimer.None;
                _hasSentIceGatheringComplete = true;

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
            Debug.LogError($"This method is not supported for browser webRTC");
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

            Browser.WebRTC_SetRemoteDescription(message);
        }

        public override void SetConfig(UserRTCConfig userRTCConfig, WebSocketSignalingConfig webSocketSignalingConfig)
        {
            _userRTCConfig = userRTCConfig;
            _webSocketSignalingConfig = webSocketSignalingConfig;
        }

        public override void Connect(string address, int port)
        {
            _signalingServiceClient.SetConfig(_webSocketSignalingConfig);
            _signalingServiceClient.OnConnectedToServer += OnConnectedToSignalingServer;
            _signalingServiceClient.OnDisconnectedFromServer += OnDisconnectedFromSignalingServer;
            _signalingServiceClient.Start();

            _signalingServiceClient.Connect(address, port);

            _timerLocalTimeout = FlexTimer.CreateFromSeconds(_userRTCConfig.TimeoutDuration);
        }

        private void OnDisconnectedFromSignalingServer()
        {
            bool isSuccess = _signalingServiceClient.IsSuccess;

            if (isSuccess) return;

            _timerLocalTimeout = FlexTimer.None;
            BroadcastOnTimeout();
        }

        private void OnConnectedToSignalingServer()
        {
            ConstructRTCPeerConnection();

            BrowserRTCDataChannelInit rtcDataChannelConfig = new BrowserRTCDataChannelInit();
            rtcDataChannelConfig.maxRetransmits = 0;
            rtcDataChannelConfig.ordered = false;

            Browser.WebRTC_CreateDataChannel(rtcDataChannelConfig);
            Browser.WebRTC_SetCallbackOnMessage(OnMessage);

            Browser.WebRTC_CreateOffer();
        }

        private BrowserRTCConfiguration GetSelectedSdpSemantics()
        {
            BrowserRTCConfiguration config = default;
            config.iceServers = GetRTCIceFromUserIce(_userRTCConfig.IceServers);

            return config;
        }

        protected BrowserRTCIceServer[] GetRTCIceFromUserIce(IceServer[] iceServers)
        {
            BrowserRTCIceServer[] rtcIceServers = new BrowserRTCIceServer[iceServers.Length];

            for (int i = 0; i < iceServers.Length; i++)
            {
                IceServer ice = iceServers[i];

                BrowserRTCIceServer rtcIce = new BrowserRTCIceServer()
                {
                    credential = ice.Credential,
                    credentialType = BrowserRTCIceCredentialType.Password,
                    urls = ice.Url,
                    username = ice.Username,
                };

                rtcIceServers[i] = rtcIce;
            }

            return rtcIceServers;
        }

        public override void Send(IntPtr ptr, int length)
        {
            Browser.WebRTC_DataChannelSend(ptr, length);
        }

        public override void CloseConnection()
        {
            Browser.WebRTC_CloseConnection();
            Browser.WebRTC_Reset();
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
