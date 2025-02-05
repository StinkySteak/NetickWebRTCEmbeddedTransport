#if UNITY_WEBGL && !UNITY_EDITOR
#define BROWSER
#endif

using System;
using System.Collections;
using NaughtyAttributes;
using Newtonsoft.Json;
using UnityEngine;

#if !BROWSER
using Unity.WebRTC;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Text;
using System.Runtime.InteropServices;
#endif

public class WebRTCPeerManual : MonoBehaviour
{
#if !BROWSER
    private RTCPeerConnection _peerConnection;
    private RTCDataChannel _channel;

    public bool IsOfferer;

    [TextArea(6, 6)] public string Offer;
    [TextArea(6, 6)] public string Answer;

    public bool HasLocalDescription;
    public bool HasRemoteDescription;

    public event Action OnOfferCreated;
    public event Action OnAnswerCreated;

    [Button]
    public void CreateOffer()
    {
        StartCoroutine(CreateOfferCo());
    }

    [Button]
    public void CreateAnswerFromOffer()
    {
        StartCoroutine(CreateAnswerCo());
    }

    [Button]
    public void ApplyAnswer()
    {
        StartCoroutine(AddAnswerCo());
    }

    private static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };

        return config;
    }

    private void Start()
    {
        ConstructRTCPeerConnection();
    }

    private void ConstructRTCPeerConnection()
    {
        RTCConfiguration configuration = GetSelectedSdpSemantics();
        _peerConnection = new RTCPeerConnection(ref configuration);
        _peerConnection.OnIceCandidate = OnIceCandidate;
        _peerConnection.OnIceConnectionChange = OnIceConnectionChange;
        _peerConnection.OnNegotiationNeeded = OnNegotiationNeeded;
        _peerConnection.OnDataChannel = OnDataChannel;

        if (IsOfferer)
        {
            //RTCDataChannelInit options = new RTCDataChannelInit();
            //options.ordered = true;
            //options.maxRetransmits = 10;
            _channel = _peerConnection.CreateDataChannel("data");

            _channel.OnMessage = OnChannelMessage;
            _channel.OnOpen = OnChannelOpen;
            _channel.OnClose = OnChannelClose;
        }
    }

    private void OnChannelMessage(byte[] bytes)
    {
        //Log($"Message Received: {bytes.Length}");
    }

    private void Update()
    {
        if (_isChannelOpen)
        {
            string message = "Hello World";

            byte[] bytes = Encoding.UTF8.GetBytes(message);
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);

            _channel.Send(ptr, bytes.Length);
        }
    }

    private RTCSessionDescription _sdp;
    private bool _isChannelOpen;

    private IEnumerator CreateOfferCo()
    {
        Log("Creating offer...");

        var opCreateOffer = _peerConnection.CreateOffer();

        yield return opCreateOffer;

        Log("Offer created, applying as local description...");
        _sdp = opCreateOffer.Desc;
        var opSetLocalSDP = _peerConnection.SetLocalDescription(ref _sdp);

        yield return opSetLocalSDP;
        Log("Offer is applied as local description");

        HasLocalDescription = true;
    }

    private IEnumerator CreateAnswerCo()
    {
        Log("Creating answer coroutine...");

        var settings = new StringEnumConverter()
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        _sdp = JsonConvert.DeserializeObject<RTCSessionDescription>(Offer, settings);

        Log("Applying remote offer as remote description...");

        yield return _peerConnection.SetRemoteDescription(ref _sdp);

        Log("Remote Offer has been applied to remote description");
        Log("Creating answer from offer...");
        HasRemoteDescription = true;
        var opCreateAnswer = _peerConnection.CreateAnswer();

        yield return opCreateAnswer;

        _sdp = opCreateAnswer.Desc;
        Log("Answer created, apply as local description...");

        yield return _peerConnection.SetLocalDescription(ref _sdp);
        HasLocalDescription = true;
        Log("Answer is applied as local description");
    }

    private IEnumerator AddAnswerCo()
    {
        var settings = new StringEnumConverter()
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        Log("Applying answer as remote description...");
        _sdp = JsonConvert.DeserializeObject<RTCSessionDescription>(Answer, settings);

        yield return _peerConnection.SetRemoteDescription(ref _sdp);

        HasRemoteDescription = true;
        Log("Answer is applied as remote description");
    }

    private void Log(string msg)
    {
        print($"[{name}]: {msg}");
    }

    private void OnChannelOpen()
    {
        Log($"OnChannelOpen: {Time.time}");
        _isChannelOpen = true;
    }
    private void OnChannelClose()
    {
        Log($"OnChannelClose: {Time.time}");
    }

    private void OnDataChannel(RTCDataChannel dataChannel)
    {
        _channel = dataChannel;
        _channel.OnMessage = OnChannelMessage;
        _isChannelOpen = true;
        Log("OnDataChannel");
    }

    private void OnIceCandidate(RTCIceCandidate iceCandidate)
    {
        var settings = new StringEnumConverter()
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        if (IsOfferer)
        {
            Offer = JsonConvert.SerializeObject(_peerConnection.LocalDescription, Formatting.Indented, settings);
            OnOfferCreated?.Invoke();
        }
        else
        {
            Answer = JsonConvert.SerializeObject(_peerConnection.LocalDescription, Formatting.Indented, settings);
            OnAnswerCreated?.Invoke();
        }

        Log($"On Ice Candidate Added: {iceCandidate.Candidate}");
    }

    public void AddIceCandidate(RTCIceCandidate iceCandidate)
    {
        _peerConnection.AddIceCandidate(iceCandidate);
    }

    private void OnIceConnectionChange(RTCIceConnectionState state)
    {
        Log($"OnIceConnectionChange: {state}");
    }

    private void OnNegotiationNeeded()
    {
        Log("OnNegotiationNeeded");
    }

    private void OnDestroy()
    {
        _channel?.Close();
        _peerConnection?.Close();
    }
#endif
}
