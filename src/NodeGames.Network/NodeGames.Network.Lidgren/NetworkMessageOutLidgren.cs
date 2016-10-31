using Lidgren.Network;
using NodeGames.Network.Network.Messages;

namespace NodeGames.Network.Lidgren
{
    internal class NetworkMessageOutLidgren : INetworkMessageOut<NetOutgoingMessage>
    {
        private readonly NetOutgoingMessage _message;
        private readonly NetPeer _netPeer;

        public NetworkMessageOutLidgren(NetOutgoingMessage message, NetPeer netPeer)
        {
            _message = message;
            _netPeer = netPeer;
        }

        public NetOutgoingMessage GetInternalMessage()
        {
            return _message;
        }

        public void Write(int value)
        {
            _message.Write(value);
        }

        public void Write(byte value)
        {
            _message.Write(value);
        }

        public void Write(string value)
        {
            _message.Write(value);
        }

        public void Write(float value)
        {
            _message.Write(value);
        }

        public void Write(bool value)
        {
            _message.Write(value);
        }

        public void Encrypt()
        {
            NetEncryption algo = new NetXtea(_netPeer, "EoE");
            _message.Encrypt(algo);
        }
    }
}
