namespace NodeGames.Network.Network.Messages
{
    public interface INetworkMessageIn
    {
        long UniqueId { get; }
        string EndPoint { get; }

        int ReadInt();
        bool ReadBool();
        byte ReadByte();
        float ReadFloat();
        short ReadShort();
        string ReadString();

        void Recycle();
        void Decrypt();
        
        NetworkMessageType GetMessageType();
    }

    public interface INetworkMessageIn<out T> : INetworkMessageIn
    {
        T GetInternalMessage();
    }
}
