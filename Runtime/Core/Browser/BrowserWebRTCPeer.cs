using System;
using System.Collections.Generic;
using AOT;
using StinkySteak.Timer;
using StinkySteak.WebRealtimeCommunication;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class BrowserWebRTCPeer : BaseWebRTCPeer
    {
        private static Dictionary<int, BrowserWebRTCPeer> _peers = new Dictionary<int, BrowserWebRTCPeer>();

        private WebSocketSignalingServer _signalingServiceServer;
        private WebSocketClientSignalingService _signalingServiceClient;
        private WebRTCEndPoint _endPoint = new();

        private int _connectionId;
        private WebRTCSessionDescription _offer;
        private WebRTCSessionDescription _answer;
        private RunMode _peerMode;
        private bool _hasSentIceGatheringComplete;
        private bool _isTimedOut;
        private int _peerIndex;

        private FlexTimer _timerLocalTimeout;
        private FlexTimer _timerIceTrickling;

        private UserRTCConfig _userRTCConfig;
        private WebSocketSignalingConfig _webSocketSignalingConfig;

        public override IEndPoint EndPoint => _endPoint;
        public override bool IsConnectionOpen => Browser.WebRTC_IsConnectionOpen(_peerIndex);
        public override bool IsTimedOut => _isTimedOut;

        public override void Start(RunMode runMode)
        {
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

            Browser.WebRTC_SetCallbackOnIceCandidate(_peerIndex, OnIceCandidate);
            Browser.WebRTC_SetCallbackOnIceConnectionStateChange(_peerIndex, OnIceConnectionChanged);
            Browser.WebRTC_SetCallbackOnDataChannelCreated(_peerIndex, OnDataChannelCreated);
            Browser.WebRTC_SetCallbackOnIceCandidateGatheringState(_peerIndex, OnIceGatheringStateChanged);

            Browser.WebRTC_SetCallbackOnDataChannelOpen(_peerIndex, OnDataChannelOpen);
            Browser.WebRTC_SetCallbackOnDataChannelReliableOpen(_peerIndex, OnDataChannelReliableOpen);
        }

        [MonoPInvokeCallback(typeof(OnIceCandidate))]
        private static void OnIceCandidate(int index)
        {
        }

        [MonoPInvokeCallback(typeof(OnIceConnectionStateChange))]
        private static void OnIceConnectionChanged(int index)
        {
        }

        [MonoPInvokeCallback(typeof(OnIceCandidateGatheringState))]
        private static void OnIceGatheringStateChanged(int index, int state)
        {
            BrowserRTCIceGatheringState rtcState = (BrowserRTCIceGatheringState)state;
        }

        [MonoPInvokeCallback(typeof(OnDataChannelOpen))]
        private static void OnDataChannelOpen(int index)
        {
            BrowserWebRTCPeer peer = _peers[index];
            peer._timerLocalTimeout = FlexTimer.None;

            string remoteDescription = Browser.WebRTC_GetRemoteDescriptionJson(index);

            SDPParser.ParseSDP(remoteDescription, out string ip, out int port);

            peer._endPoint.Init(ip, port);
        }

        [MonoPInvokeCallback(typeof(OnDataChannelReliableOpen))]
        private static void OnDataChannelReliableOpen(int index)
        {

        }

        [MonoPInvokeCallback(typeof(OnDataChannelCreated))]
        private static void OnDataChannelCreated(int index)
        {
            BrowserWebRTCPeer peer = _peers[index];
            string remoteDescription = Browser.WebRTC_GetRemoteDescriptionJson(index);

            SDPParser.ParseSDP(remoteDescription, out string ip, out int port);

            peer._endPoint.Init(ip, port);
        }

        [MonoPInvokeCallback(typeof(OnMessageCallback))]
        private static void OnMessage(int index, IntPtr ptr, int length)
        {
            BrowserWebRTCPeer peer = _peers[index];
            peer.BroadcastOnMessageUnmanaged(ptr, length);
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
            if (!Browser.WebRTC_HasOpSetLocalDescription(_peerIndex)) return;

            if (Browser.WebRTC_IsOpSetLocalDescriptionDone(_peerIndex))
            {
                Log("Answer has been set to local description!");

                if (_userRTCConfig.IceTricklingConfig.IsManual)
                    _timerIceTrickling = FlexTimer.CreateFromSeconds(_userRTCConfig.IceTricklingConfig.Duration);

                Browser.WebRTC_DisposeOpSetLocalDescription(_peerIndex);
            }
        }

        private void Log(string msg)
        {
            Debug.Log($"[{this}]: {msg}");
        }

        private void PollOpCreateAnswer()
        {
            if (!Browser.WebRTC_HasOpCreateAnswer(_peerIndex)) return;

            if (Browser.WebRTC_GetOpCreateAnswerIsDone(_peerIndex))
            {
                Log("Answer is created. Setting it as local description...");

                WebRTCSessionDescription answer = Browser.WebRTC_GetAnswer(_peerIndex);

                Browser.WebRTC_SetLocalDescription(_peerIndex, answer);

                Browser.WebRTC_DisposeOpCreateAnswer(_peerIndex);
            }
        }

        private void PollOpSetRemoteOffer()
        {
            if (!Browser.WebRTC_HasOpSetRemoteDescription(_peerIndex)) return;

            if (Browser.WebRTC_IsOpSetRemoteDescriptionDone(_peerIndex))
            {
                Log("Offer has been set to remote description. Creating an answer...");

                Browser.WebRTC_CreateAnswer(_peerIndex);

                Browser.WebRTC_DisposeOpSetRemoteDescription(_peerIndex);
            }
        }

        private void PollOpSetLocalOffer()
        {
            if (!Browser.WebRTC_HasOpSetLocalDescription(_peerIndex)) return;

            if (Browser.WebRTC_IsOpSetLocalDescriptionDone(_peerIndex))
            {
                Log("Offer has been set to local!");

                if (_userRTCConfig.IceTricklingConfig.IsManual)
                    _timerIceTrickling = FlexTimer.CreateFromSeconds(_userRTCConfig.IceTricklingConfig.Duration);

                Browser.WebRTC_DisposeOpSetLocalDescription(_peerIndex);
            }
        }

        private void PollOpSetRemoteAnswer()
        {
            if (!Browser.WebRTC_HasOpSetRemoteDescription(_peerIndex)) return;

            if (Browser.WebRTC_IsOpSetRemoteDescriptionDone(_peerIndex))
            {
                Log("Answer has been set to remote description");

                Browser.WebRTC_DisposeOpSetRemoteDescription(_peerIndex);
            }
        }

        private void PollOpCreateOffer()
        {
            if (!Browser.WebRTC_HasOpCreateOffer(_peerIndex)) return;

            if (Browser.WebRTC_GetOpCreateOfferIsDone(_peerIndex))
            {
                Log("Offer Created. Setting it to local...");
                WebRTCSessionDescription offer = Browser.WebRTC_GetOffer(_peerIndex);

                Browser.WebRTC_SetLocalDescription(_peerIndex, offer);

                Browser.WebRTC_DisposeOpCreateOffer(_peerIndex);
            }
        }

        private void PollIceCandidate()
        {
            if (!Browser.WebRTC_GetIsPeerConnectionCreated(_peerIndex)) return;

            if (_hasSentIceGatheringComplete) return;

            if (Browser.WebRTC_GetGatheringState(_peerIndex) == BrowserRTCIceGatheringState.Complete || _timerIceTrickling.IsExpired())
            {
                _timerIceTrickling = FlexTimer.None;
                _hasSentIceGatheringComplete = true;

                if (_peerMode == RunMode.Client)
                {
                    Log("Sending offer to the server...");
                    _offer = Browser.WebRTC_GetLocalDescription(_peerIndex);
                    SendOfferToServer();
                }
                if (_peerMode == RunMode.Server)
                {
                    Log("Sending answer to the client...");
                    _answer = Browser.WebRTC_GetLocalDescription(_peerIndex);
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

            Browser.WebRTC_SetRemoteDescription(_peerIndex, message);
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

            Browser.WebRTC_CreateDataChannel(_peerIndex, rtcDataChannelConfig);
            Browser.WebRTC_CreateDataChannelReliable(_peerIndex);
            Browser.WebRTC_SetCallbackOnMessage(_peerIndex, OnMessage);

            Browser.WebRTC_CreateOffer(_peerIndex);
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

        public override void Send(IntPtr ptr, int length, bool isReliable)
        {
            if (!isReliable)
            {
                Browser.WebRTC_DataChannelSend(_peerIndex, ptr, length);
                return;
            }

            Browser.WebRTC_DataChannelReliableSend(_peerIndex, ptr, length);
        }

        public override void CloseConnection()
        {
            Browser.WebRTC_CloseConnection(_peerIndex);
            Browser.WebRTC_Reset(_peerIndex);
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
