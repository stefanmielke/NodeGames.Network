namespace NodeGames.Network.Network
{
    public struct NetworkStats
    {
        public int BytesSent;
        public int BytesReceived;
        public int PacketsSent;
        public int PacketsReceived;
        public int MessagesSent;
        public int MessagesReceived;

        public NetworkStats(int bytesSent, int bytesReceived, int packetsSent, int packetsReceived, int messagesSent, int messagesReceived)
        {
            BytesSent = bytesSent;
            BytesReceived = bytesReceived;
            PacketsSent = packetsSent;
            PacketsReceived = packetsReceived;
            MessagesSent = messagesSent;
            MessagesReceived = messagesReceived;
        }
    }
}
