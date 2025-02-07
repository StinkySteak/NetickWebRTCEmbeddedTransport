namespace Netick.Transport.WebRTC
{
    [System.Serializable]
    public struct UserRTCConfig
    {
        public string[] IceServers;
        public float TimeoutDuration;
        public IceTricklingConfig IceTricklingConfig;
    }

    [System.Serializable]
    public struct IceTricklingConfig
    {
        public bool IsManual;
        public float Duration;
    }
}