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
        public static extern int WebRTC_Unsafe_CreateRTCPeerConnection(string configJson);

        public static int WebRTC_CreateRTCPeerConnection(BrowserRTCConfiguration config)
        {
            StringEnumConverter settings = new StringEnumConverter()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            string json = JsonConvert.SerializeObject(config, settings);

            return WebRTC_Unsafe_CreateRTCPeerConnection(json);
        }

        [DllImport("__Internal")]
        public static extern bool WebRTC_GetOpCreateOfferIsDone(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_GetOpCreateAnswerIsDone(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_DisposeOpCreateOffer(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_DisposeOpCreateAnswer(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_CreateOffer(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_CreateAnswer(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpCreateAnswer(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpSetRemoteDescription(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_IsOpSetRemoteDescriptionDone(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpCreateOffer(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_CloseConnection(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_HasOpSetLocalDescription(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_IsOpSetLocalDescriptionDone(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_DisposeOpSetLocalDescription(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_DisposeOpSetRemoteDescription(int index);

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetConnectionState(int index);

        public static BrowserRTCIceGatheringState WebRTC_GetGatheringState(int index)
        {
            return (BrowserRTCIceGatheringState)WebRTC_Unsafe_GetGatheringState(index);
        }

        [DllImport("__Internal")]
        public static extern int WebRTC_Unsafe_GetGatheringState(int index);

        public static string WebRTC_GetConnectionState(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetConnectionState(index);

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static string WebRTC_GetOfferJson(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetOffer(index);

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static string WebRTC_GetAnswerJson(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetAnswer(index);

            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            string offer = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            return offer;
        }

        public static WebRTCSessionDescription WebRTC_GetOffer(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetOffer(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string json = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            WebRTCSessionDescription sdp = JsonConvert.DeserializeObject<WebRTCSessionDescription>(json);

            return sdp;
        }

        public static WebRTCSessionDescription WebRTC_GetAnswer(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetAnswer(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string json = Marshal.PtrToStringAuto(ptr);

            Marshal.FreeHGlobal(ptr);

            WebRTCSessionDescription sdp = JsonConvert.DeserializeObject<WebRTCSessionDescription>(json);

            return sdp;
        }

        public static WebRTCSessionDescription WebRTC_GetLocalDescription(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetLocalDescription(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string sdpJson = Marshal.PtrToStringAuto(ptr);
            Marshal.FreeHGlobal(ptr);

            WebRTCSessionDescription sdp = JsonConvert.DeserializeObject<WebRTCSessionDescription>(sdpJson);

            return sdp;
        }

        public static string WebRTC_GetRemoteDescriptionJson(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetRemoteDescription(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string sdpJson = Marshal.PtrToStringAuto(ptr);

            return sdpJson;
        }

        public static WebRTCSessionDescription WebRTC_GetRemoteDescription(int index)
        {
            IntPtr ptr = WebRTC_Unsafe_GetRemoteDescription(index);

            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            string sdpJson = Marshal.PtrToStringAuto(ptr);
            Marshal.FreeHGlobal(ptr);

            WebRTCSessionDescription sdp = JsonConvert.DeserializeObject<WebRTCSessionDescription>(sdpJson);

            return sdp;
        }

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetOffer(int index);

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetAnswer(int index);

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() } }
        };

        public static void WebRTC_SetLocalDescription(int index, WebRTCSessionDescription sessionDescription)
        {
            string json = JsonConvert.SerializeObject(sessionDescription, _jsonSettings);
            WebRTC_Unsafe_SetLocalDescription(index, json);
        }

        public static void WebRTC_SetRemoteDescription(int index, WebRTCSessionDescription sessionDescription)
        {
            string json = JsonConvert.SerializeObject(sessionDescription, _jsonSettings);
            WebRTC_Unsafe_SetRemoteDescription(index, json);
        }

        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_SetLocalDescription(int index, string sdpJson);

        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_SetRemoteDescription(int index, string sdpJson);

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetLocalDescription(int index);

        [DllImport("__Internal")]
        public static extern IntPtr WebRTC_Unsafe_GetRemoteDescription(int index);

        public static void WebRTC_CreateDataChannel(int index, BrowserRTCDataChannelInit dataChannelConfig)
        {
            string json = JsonConvert.SerializeObject(dataChannelConfig);

            WebRTC_Unsafe_CreateDataChannel(index, json);
        }

        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_CreateDataChannel(int index, string configJson);

        public static void WebRTC_CreateDataChannelReliable(int index)
        {
            WebRTC_Unsafe_CreateDataChannelReliable(index);
        }

        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_CreateDataChannelReliable(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_IsConnectionOpen(int index);

        [DllImport("__Internal")]
        public static extern void WebRTC_DataChannelSend(int index, IntPtr ptr, int length);

        [DllImport("__Internal")]
        public static extern void WebRTC_DataChannelReliableSend(int index, IntPtr ptr, int length);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnMessage(int index, OnMessageCallback callback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnIceConnectionStateChange(int index, OnIceConnectionStateChange callback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnDataChannelCreated(int index, OnDataChannelCreated calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnIceCandidate(int index, OnIceCandidate calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnDataChannelOpen(int index, OnDataChannelOpen calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnDataChannelReliableOpen(int index, OnDataChannelOpen calback);

        [DllImport("__Internal")]
        public static extern void WebRTC_SetCallbackOnIceCandidateGatheringState(int index, OnIceCandidateGatheringState calback);

        [DllImport("__Internal")]
        public static extern bool WebRTC_GetIsPeerConnectionCreated(int index);

        [DllImport("__Internal")]
        public static extern bool WebRTC_Reset(int index);
#else
        public static int WebRTC_Unsafe_CreateRTCPeerConnection(string configJson) { return 0; }

        public static int WebRTC_CreateRTCPeerConnection(BrowserRTCConfiguration config) { return 0; }

        public static bool WebRTC_GetOpCreateOfferIsDone(int index) { return false; }
        public static bool WebRTC_GetOpCreateAnswerIsDone(int index) { return false; }
        public static void WebRTC_DisposeOpCreateOffer(int index) { }
        public static void WebRTC_DisposeOpCreateAnswer(int index) { }
        public static void WebRTC_CreateOffer(int index) { }
        public static void WebRTC_CreateAnswer(int index) { }
        public static bool WebRTC_HasOpCreateAnswer(int index) { return false; }
        public static bool WebRTC_HasOpSetRemoteDescription(int index) { return false; }
        public static bool WebRTC_IsOpSetRemoteDescriptionDone(int index) { return false; }
        public static bool WebRTC_HasOpCreateOffer(int index) { return false; }
        public static bool WebRTC_CloseConnection(int index) { return false; }
        public static bool WebRTC_HasOpSetLocalDescription(int index) { return false; }
        public static bool WebRTC_IsOpSetLocalDescriptionDone(int index) { return false; }
        public static bool WebRTC_DisposeOpSetLocalDescription(int index) { return false; }
        public static bool WebRTC_DisposeOpSetRemoteDescription(int index) { return false; }
        public static IntPtr WebRTC_Unsafe_GetConnectionState(int index) { return IntPtr.Zero; }

        public static BrowserRTCIceGatheringState WebRTC_GetGatheringState(int index)
        {
            return (BrowserRTCIceGatheringState)WebRTC_Unsafe_GetGatheringState(index);
        }

        public static int WebRTC_Unsafe_GetGatheringState(int index) { return 0; }

        public static string WebRTC_GetConnectionState(int index)
        {
            return string.Empty;
        }

        public static WebRTCSessionDescription WebRTC_GetOffer(int index)
        {
            return default;
        }

        public static WebRTCSessionDescription WebRTC_GetAnswer(int index)
        {
            return default;
        }

        public static WebRTCSessionDescription WebRTC_GetLocalDescription(int index)
        {
            return default;
        }

        public static WebRTCSessionDescription WebRTC_GetRemoteDescription(int index)
        {
            return default;
        }

        public static IntPtr WebRTC_Unsafe_GetOffer(int index) { return IntPtr.Zero; }
        public static IntPtr WebRTC_Unsafe_GetAnswer(int index) { return IntPtr.Zero; }
        public static void WebRTC_SetLocalDescription(int index, WebRTCSessionDescription sessionDescription) { }
        public static void WebRTC_SetRemoteDescription(int index, WebRTCSessionDescription sessionDescription) { }
        public static IntPtr WebRTC_Unsafe_GetLocalDescription(int index) { return IntPtr.Zero; }
        public static IntPtr WebRTC_Unsafe_GetRemoteDescription(int index) { return IntPtr.Zero; }

        public static void WebRTC_CreateDataChannel(int index, BrowserRTCDataChannelInit dataChannelConfig)
        {
            string json = JsonConvert.SerializeObject(dataChannelConfig);
            WebRTC_Unsafe_CreateDataChannel(index, json);
        }

        public static void WebRTC_Unsafe_CreateDataChannel(int index, string configJson) { }
        public static void WebRTC_Unsafe_CreateDataChannelReliable(int index) { }
        public static bool WebRTC_IsConnectionOpen(int index) { return false; }
        public static void WebRTC_DataChannelSend(int index, IntPtr ptr, int length) { }
        public static void WebRTC_DataChannelReliableSend(int index, IntPtr ptr, int length) { }
        public static void WebRTC_SetCallbackOnMessage(int index, OnMessageCallback callback) { }
        public static void WebRTC_SetCallbackOnIceConnectionStateChange(int index, OnIceConnectionStateChange callback) { }
        public static void WebRTC_SetCallbackOnDataChannelCreated(int index, OnDataChannelCreated callback) { }
        public static void WebRTC_SetCallbackOnIceCandidate(int index, OnIceCandidate callback) { }
        public static void WebRTC_SetCallbackOnDataChannelOpen(int index, OnDataChannelOpen callback) { }
        public static void WebRTC_SetCallbackOnDataChannelReliableOpen(int index, OnDataChannelOpen callback) { }
        public static void WebRTC_SetCallbackOnDataChanneReliablelOpen(int index, OnDataChannelReliableOpen callback) { }
        public static void WebRTC_SetCallbackOnIceCandidateGatheringState(int index, OnIceCandidateGatheringState callback) { }
        public static bool WebRTC_GetIsPeerConnectionCreated(int index) { return false; }
        public static bool WebRTC_Reset(int index) { return false; }
        public static void WebRTC_CreateDataChannelReliable(int index) { }
        public static string WebRTC_GetRemoteDescriptionJson(int index) { return string.Empty; }

#endif
    }

    public delegate void OnMessageCallback(int index, IntPtr ptr, int length);
    public delegate void OnIceConnectionStateChange(int index);
    public delegate void OnDataChannelCreated(int index);
    public delegate void OnIceCandidate(int index);
    public delegate void OnDataChannelOpen(int index);
    public delegate void OnDataChannelReliableOpen(int index);
    public delegate void OnIceCandidateGatheringState(int index, int state);

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

    public struct WebRTCSessionDescription
    {
        public RTCSdpType type;
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