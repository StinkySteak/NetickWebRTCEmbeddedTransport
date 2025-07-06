using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace StinkySteak.WebRealtimeCommunication
{
    public static class Browser
    {
#if UNITY_WEBGL
        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_CreateRTCPeerConnection(string configJson);

        public static void WebRTC_CreateRTCPeerConnection(BrowserRTCConfiguration config)
        {
            StringEnumConverter settings = new StringEnumConverter()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            string json = JsonConvert.SerializeObject(config, settings);

            WebRTC_Unsafe_CreateRTCPeerConnection(json);
        }

        [DllImport("__Internal")]
        public static extern bool WebRTC_GetOpCreateOfferIsDone();

        [DllImport("__Internal")]
        public static extern bool WebRTC_GetOpCreateAnswerIsDone();

        [DllImport("__Internal")]
        public static extern void WebRTC_DisposeOpCreateOffer();

        [DllImport("__Internal")]
        public static extern void WebRTC_DisposeOpCreateAnswer();

        [DllImport("__Internal")]
        public static extern void WebRTC_CreateOffer();

        [DllImport("__Internal")]
        public static extern void WebRTC_CreateAnswer();

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpCreateAnswer();

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpSetRemoteDescription();

        [DllImport("__Internal")]
        public static extern bool WebRTC_IsOpSetRemoteDescriptionDone();

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpCreateOffer();

        [DllImport("__Internal")]
        public static extern bool WebRTC_CloseConnection();

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpSetLocalDescription();

        [DllImport("__Internal")]
        public static extern bool WebRTC_IsOpSetLocalDescriptionDone();

        [DllImport("__Internal")]
        public static extern bool WebRTC_DisposeOpSetLocalDescription();

        [DllImport("__Internal")]
        public static extern bool WebRTC_DisposeOpSetRemoteDescription();

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetConnectionState();

        public static BrowserRTCIceGatheringState WebRTC_GetGatheringState()
        {
            return (BrowserRTCIceGatheringState)WebRTC_Unsafe_GetGatheringState();
        }

        [DllImport("__Internal")]
        public static extern int WebRTC_Unsafe_GetGatheringState();

        public static string WebRTC_GetConnectionState()
        {
            IntPtr ptr = WebRTC_Unsafe_GetConnectionState();

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static string WebRTC_GetOffer()
        {
            IntPtr ptr = WebRTC_Unsafe_GetOffer();

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static string WebRTC_GetAnswer()
        {
            IntPtr ptr = WebRTC_Unsafe_GetAnswer();

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static string WebRTC_GetLocalDescription()
        {
            IntPtr ptr = WebRTC_Unsafe_GetLocalDescription();

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static string WebRTC_GetRemoteDescription()
        {
            IntPtr ptr = WebRTC_Unsafe_GetRemoteDescription();

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetOffer();

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetAnswer();

        [DllImport("__Internal")]
        public static extern void WebRTC_SetLocalDescription(string sdp);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetRemoteDescription(string sdp);

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetLocalDescription();

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetRemoteDescription();

        public static void WebRTC_CreateDataChannel(BrowserRTCDataChannelInit dataChannelConfig)
        {
            string json = JsonConvert.SerializeObject(dataChannelConfig);

            WebRTC_Unsafe_CreateDataChannel(json);
        }

        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_CreateDataChannel(string configJson);

        [DllImport("__Internal")]
        public static extern bool WebRTC_IsConnectionOpen();

        [DllImport("__Internal")]
        public static extern void WebRTC_DataChannelSend(IntPtr ptr, int length);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnMessage(OnMessageCallback callback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnIceConnectionStateChange(OnIceConnectionStateChange callback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnDataChannel(OnDataChannel calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnIceCandidate(OnIceCandidate calback);
        
        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnChannelOpen(OnChannelOpen calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnIceCandidateGatheringState(OnIceCandidateGatheringState calback);
        
        [DllImport("__Internal")]
        public static extern bool WebRTC_GetIsPeerConnectionCreated();

        [DllImport("__Internal")]
        public static extern bool WebRTC_Reset();
#else
        public static void WebRTC_Unsafe_CreateRTCPeerConnection(string configJson) { }

        public static void WebRTC_CreateRTCPeerConnection(BrowserRTCConfiguration config)
        {
            string json = JsonConvert.SerializeObject(config);
            WebRTC_Unsafe_CreateRTCPeerConnection(json);
        }

        public static bool WebRTC_GetOpCreateOfferIsDone() { return false; }
        public static bool WebRTC_GetOpCreateAnswerIsDone() { return false; }
        public static void WebRTC_DisposeOpCreateOffer() { }
        public static void WebRTC_DisposeOpCreateAnswer() { }
        public static void WebRTC_CreateOffer() { }
        public static void WebRTC_CreateAnswer() { }
        public static bool WebRTC_HasOpCreateAnswer() { return false; }
        public static bool WebRTC_HasOpSetRemoteDescription() { return false; }
        public static bool WebRTC_IsOpSetRemoteDescriptionDone() { return false; }
        public static bool WebRTC_HasOpCreateOffer() { return false; }
        public static bool WebRTC_CloseConnection() { return false; }
        public static bool WebRTC_HasOpSetLocalDescription() { return false; }
        public static bool WebRTC_IsOpSetLocalDescriptionDone() { return false; }
        public static bool WebRTC_DisposeOpSetLocalDescription() { return false; }
        public static bool WebRTC_DisposeOpSetRemoteDescription() { return false; }
        public static IntPtr WebRTC_Unsafe_GetConnectionState() { return IntPtr.Zero; }

        public static BrowserRTCIceGatheringState WebRTC_GetGatheringState()
        {
            return (BrowserRTCIceGatheringState)WebRTC_Unsafe_GetGatheringState();
        }

        public static int WebRTC_Unsafe_GetGatheringState() { return 0; }

        public static string WebRTC_GetConnectionState()
        {
            return string.Empty;
        }

        public static string WebRTC_GetOffer()
        {
            return string.Empty;
        }

        public static string WebRTC_GetAnswer()
        {
            return string.Empty;
        }

        public static string WebRTC_GetLocalDescription()
        {
            return string.Empty;
        }

        public static string WebRTC_GetRemoteDescription()
        {
            return string.Empty;
        }

        public static IntPtr WebRTC_Unsafe_GetOffer() { return IntPtr.Zero; }
        public static IntPtr WebRTC_Unsafe_GetAnswer() { return IntPtr.Zero; }
        public static void WebRTC_SetLocalDescription(string sdp) { }
        public static void WebRTC_SetRemoteDescription(string sdp) { }
        public static IntPtr WebRTC_Unsafe_GetLocalDescription() { return IntPtr.Zero; }
        public static IntPtr WebRTC_Unsafe_GetRemoteDescription() { return IntPtr.Zero; }

        public static void WebRTC_CreateDataChannel(BrowserRTCDataChannelInit dataChannelConfig)
        {
            string json = JsonConvert.SerializeObject(dataChannelConfig);
            WebRTC_Unsafe_CreateDataChannel(json);
        }

        public static void WebRTC_Unsafe_CreateDataChannel(string configJson) { }
        public static bool WebRTC_IsConnectionOpen() { return false; }
        public static void WebRTC_DataChannelSend(IntPtr ptr, int length) { }
        public static void WebRTC_SetCallbackOnMessage(OnMessageCallback callback) { }
        public static void WebRTC_SetCallbackOnIceConnectionStateChange(OnIceConnectionStateChange callback) { }
        public static void WebRTC_SetCallbackOnDataChannel(OnDataChannel callback) { }
        public static void WebRTC_SetCallbackOnIceCandidate(OnIceCandidate callback) { }
        public static void WebRTC_SetCallbackOnChannelOpen(OnChannelOpen callback) { }
        public static void WebRTC_SetCallbackOnIceCandidateGatheringState(OnIceCandidateGatheringState callback) { }
        public static bool WebRTC_GetIsPeerConnectionCreated() { return false; }
        public static bool WebRTC_Reset() { return false; }

#endif
    }

    public delegate void OnMessageCallback(IntPtr ptr, int length);
    public delegate void OnIceConnectionStateChange();
    public delegate void OnDataChannel();
    public delegate void OnIceCandidate();
    public delegate void OnChannelOpen();
    public delegate void OnIceCandidateGatheringState(int state);

    public enum BrowserRTCIceGatheringState : int
    {
        New = 0,
        Gathering = 1,
        Complete = 2
    }

    public struct BrowserRTCConfiguration
    {
        public BrowserRTCIceServer[] iceServers;
    }

    public struct BrowserRTCIceServer
    {
        public string[] urls;
        public BrowserRTCIceCredentialType credentialType;
        public string username;
        public string credential;
    }

    public struct BrowserRTCSessionDescription
    {
        public string type;
        public string sdp;
    }

    public enum RTCSdpType
    {
        Offer,
        Pranswer,
        Answer,
        Rollback
    }

    public enum BrowserRTCIceCredentialType
    {
        Password,
        OAuth
    }

    public struct BrowserRTCDataChannelInit
    {
        public bool ordered;
        public int maxRetransmits;
    }
}