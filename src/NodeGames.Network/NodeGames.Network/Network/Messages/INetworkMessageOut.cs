namespace NodeGames.Network.Network.Messages
{
    public interface INetworkMessageOut
    {
        void Write(int value);
        void Write(byte value);
        void Write(string value);
        void Write(float value);
        void Write(bool value);

        void Encrypt();
    }

    public interface INetworkMessageOut<out T> : INetworkMessageOut
    {
        T GetInternalMessage();
    }
}
