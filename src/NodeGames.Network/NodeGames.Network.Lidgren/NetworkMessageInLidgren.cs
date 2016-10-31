using Lidgren.Network;
using NodeGames.Network.Network;
using NodeGames.Network.Network.Messages;

namespace NodeGames.Network.Lidgren
{
    internal class NetworkMessageInLidgren : INetworkMessageIn<NetIncomingMessage>
    {
        private readonly NetIncomingMessage _message;
        private readonly NetPeer _netPeer;

        public NetworkMessageInLidgren(NetIncomingMessage message, NetPeer netPeer)
        {
            _message = message;
            _netPeer = netPeer;
        }

        public NetIncomingMessage GetInternalMessage()
        {
            return _message;
        }

        public long UniqueId => _message.SenderConnection.RemoteUniqueIdentifier;
        public string EndPoint => _message.SenderEndPoint.ToString();

        public int ReadInt()
        {
            return _message.ReadInt32();
        }

        public bool ReadBool()
        {
            return _message.ReadBoolean();
        }

        public byte ReadByte()
        {
            return _message.ReadByte();
        }

        public float ReadFloat()
        {
            return _message.ReadFloat();
        }

        public short ReadShort()
        {
            return _message.ReadInt16();
        }

        public string ReadString()
        {
            return _message.ReadString();
        }

        public void Recycle()
        {
            _netPeer.Recycle(_message);
        }

        public void Decrypt()
        {
            NetEncryption algo = new NetXtea(_netPeer, "EoE");
            _message.Decrypt(algo);
        }

        public NetworkMessageType GetMessageType()
        {
            switch (_message.MessageType)
            {
                case NetIncomingMessageType.ConnectionApproval:
                    return NetworkMessageType.ConnectionApproval;
                case NetIncomingMessageType.StatusChanged:
                    switch ((NetConnectionStatus)_message.ReadByte())
                    {
                        case NetConnectionStatus.Connected:
                            return NetworkMessageType.Connected;
                        case NetConnectionStatus.Disconnected:
                            return NetworkMessageType.Disconnected; ;
                    }
                    break;
                case NetIncomingMessageType.Data:
                    return (NetworkMessageType)_message.ReadInt16();
            }

            return NetworkMessageType.None;
        }
    }
}
