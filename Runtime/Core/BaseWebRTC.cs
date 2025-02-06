using Netick;
using System;

namespace Netick.Transport.WebRTC
{
    public abstract class BaseWebRTCPeer
    {
        public event Action<BaseWebRTCPeer, byte[]> OnMessageReceived;
        public event Action<BaseWebRTCPeer, IntPtr, int> OnMessageReceivedUnmanaged;

        public event Action<BaseWebRTCPeer> OnConnectionClosed;
        public event Action<BaseWebRTCPeer> OnTimeout;

        public abstract void SetConfig(string[] iceServers, float timeoutDuration);

        public abstract void Send(IntPtr ptr, int length);

        public abstract void Connect(string address, int port);

        public abstract void CloseConnection();

        public abstract void Start(RunMode runMode);

        public abstract void PollUpdate();

        public abstract void SetSignalingServer(WebSocketSignalingServer signalingServer);

        public abstract void OnReceivedOfferFromClient(string offer);

        public abstract void SetConnectionId(int id);


        public abstract IEndPoint EndPoint { get; }
        public abstract bool IsConnectionOpen { get; }
        public abstract bool IsTimedOut { get; }

        protected void BroadcastOnMessage(byte[] bytes)
        {
            OnMessageReceived?.Invoke(this, bytes);
        }

        protected void BroadcastOnMessageUnmanaged(IntPtr ptr, int length)
        {
            OnMessageReceivedUnmanaged?.Invoke(this, ptr, length);
        }

        protected void BroadcastOnTimeout()
        {
            OnTimeout?.Invoke(this);
        }

        protected void BroadcastOnConnectionClosed()
        {
            OnConnectionClosed?.Invoke(this);
        }
    }
}
