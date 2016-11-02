using System.Linq;
using System.Reflection;
using NodeGames.Network.Network.Messages;

namespace NodeGames.Network.Network
{
    public abstract class NetworkPeerClient : NetworkPeer
    {
        protected NetworkPeerClient(float tickTimesPerSecond, Assembly actorsAssembly) : base(tickTimesPerSecond, actorsAssembly)
        {
        }

        /// <summary>
        /// Create a remote actor on a client based on its hashed name.
        /// </summary>
        /// <param name="hashedClassName">String hashed with CompatibilityManager.GetHashCode(Actor.GetType().Name)</param>
        /// <param name="id">Remote Id of the actor.</param>
        /// <param name="x">X position of the actor on the server.</param>
        /// <param name="y">Y position of the actor on the server.</param>
        /// <returns></returns>
        protected abstract INetworkedActor CreateRemoteActorByName(int hashedClassName, int id, int x, int y);

        /// <summary>
        /// Create a local player on the client.
        /// </summary>
        /// <param name="id">Remote Id of the actor owned by this client.</param>
        /// <param name="x">X position of the actor on the server.</param>
        /// <param name="y">Y position of the actor on the server.</param>
        /// <returns></returns>
        protected abstract INetworkedActor CreateLocalPlayer(int id, int x, int y);

        /// <summary>
        /// Change the game state to the one sent
        /// </summary>
        protected abstract void ChangeGameState(byte newGameState, string levelName);

        /// <summary>
        /// What happens when this instance disconnects?
        /// </summary>
        protected abstract void HandleDisconnected();

        /// <summary>
        /// What happens when a client disconnects?
        /// </summary>
        /// <param name="id">Id of the disconnected client</param>
        protected abstract void HandlePlayerDisconnected(int id);

        /// <summary>
        /// What happens when this instance receives a text chat?
        /// </summary>
        protected abstract void ReceivedChatMessage(string text);

        internal override void HandleActorReplication(INetworkMessageIn msg)
        {
            int actorQuantity = msg.ReadInt();

            for (int i = 0; i < actorQuantity; i++)
            {
                int id = msg.ReadInt();
                int x = msg.ReadInt();
                int y = msg.ReadInt();

                var actor = Actors.FirstOrDefault(a => a.Id == id);

                actor?.SetLocation(x, y);
            }
        }

        internal override void HandleActorCreation(INetworkMessageIn msg)
        {
            var quantity = msg.ReadInt();

            for (int i = 0; i < quantity; i++)
            {
                var id = msg.ReadInt();
                var x = msg.ReadInt();
                var y = msg.ReadInt();
                var hashedClassName = msg.ReadInt();

                if (Actors.Exists(a => a.Id == id))
                    return;

                var newActor = CreateRemoteActorByName(hashedClassName, id, x, y);
                if (newActor == null)
                    return;

                Actors.Add(newActor);
                if (newActor.ReplicateProperties)
                {
                    newActor.Deserialize(msg);
                }
            }
        }

        internal override void HandleActorRequestPlayerActor(INetworkMessageIn msg)
        {
            var id = msg.ReadInt();
            var x = msg.ReadInt();
            var y = msg.ReadInt();

            var player = CreateLocalPlayer(id, x, y);
            Actors.Add(player);
        }

        internal override void HandleActorPropertiesReplication(INetworkMessageIn msg)
        {
            int actorQuantity = msg.ReadInt();

            for (int i = 0; i < actorQuantity; i++)
            {
                int id = msg.ReadInt();

                var actor = Actors.FirstOrDefault(a => a.Id == id);
                actor?.Deserialize(msg);
            }
        }

        internal override void HandleActorDestruction(INetworkMessageIn msg)
        {
            int id = msg.ReadInt();

            var actor = Actors.FirstOrDefault(a => a.Id == id);
            if (actor == null)
                return;

            actor.IsMarkedToDestroy = true;
            actor.RemoteDestroyed();
        }

        internal override void HandleServerTravel(INetworkMessageIn msg)
        {
            var newGameState = msg.ReadByte();
            var worldBuilderClass = msg.ReadString();
            var levelName = msg.ReadString();

            Actors.Clear();

            ChangeGameState(newGameState, levelName);

            var outMessage = CreateMessage(NetworkMessageType.ServerTravel);

            SendMessageToAll(outMessage, MessageDeliveryMethod.ReliableOrdered, 2);
        }

        internal override void HandleDisconnected(INetworkMessageIn msg)
        {
            HandleDisconnected();
        }

        internal override void HandleClientDisconnected(INetworkMessageIn msg)
        {
            int id = msg.ReadInt();

            Actors.RemoveAll(a => a.Id == id);

            HandlePlayerDisconnected(id);
        }

        internal override void HandleChatMessage(INetworkMessageIn msg)
        {
            var message = msg.ReadString();

            ReceivedChatMessage(message);
        }
    }
}
