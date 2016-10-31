using NodeGames.Network.Network.Messages;

namespace NodeGames.Network.Network
{
    public interface INetworkedActor
    {
        int Id { get; }
        bool ReplicateProperties { get; }
        bool ReplicateMovement { get; }
        bool IsDirty { get; set; }
        bool IsMovementDirty { get; set; }
        bool IsMarkedToDestroy { get; set; }

        void Serialize(INetworkMessageOut message);
        void Deserialize(INetworkMessageIn message);

        void SetLocation(int x, int y);
        Point GetLocation();
        Rectangle GetBoundBox();

        void RemoteDestroyed();
    }
}