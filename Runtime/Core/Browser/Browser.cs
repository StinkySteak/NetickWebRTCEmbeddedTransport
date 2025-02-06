using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace StinkySteak.WebRealtimeCommunication
{
    public static class Browser
    {
        [DllImport("__Internal")]
        public static extern void WebRTC_Unsafe_CreateRTCPeerConnection(string configJson);

        public static void WebRTC_CreateRTCPeerConnection(BrowserRTCConfiguration config)
        {
            string json = JsonConvert.SerializeObject(config);

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

        [DllImport("__Internal")]
        public static extern void WebRTC_CreateDataChannel();

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
}