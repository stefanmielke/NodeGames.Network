using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NodeGames.Network.Network.Messages;

namespace NodeGames.Network.Network
{
    public abstract class NetworkPeerServer : NetworkPeer
    {
        private readonly List<int> _actorsToDestroy;
        private readonly List<INetworkedActor> _actorsToCreate;
        private LevelChange _levelChange;
        private LevelChange _lastLevelChange;

        private readonly Dictionary<long, ReadyConnection> _readyConnections;

        protected NetworkPeerServer(float tickTimesPerSecond, Assembly actorsAssembly) : base(tickTimesPerSecond, actorsAssembly)
        {
            _actorsToDestroy = new List<int>();
            _actorsToCreate = new List<INetworkedActor>();
            _levelChange = null;
            _lastLevelChange = null;
            _readyConnections = new Dictionary<long, ReadyConnection>(4);
        }
        
        public sealed override string GetApprovalString()
        {
            return string.Empty;
        }

        protected abstract INetworkedActor CreateRemotePlayer(string playerName);
        protected abstract void RemoveRemotePlayer(INetworkedActor player);
        protected abstract void ReceivedChatMessage(string text);

        public void SendServerTravel(byte newGameState, string worldBuilder, string levelName, int width, int height)
        {
            Actors.Clear();

            _levelChange = new LevelChange(newGameState, worldBuilder, levelName, width, height);
            _lastLevelChange = new LevelChange(newGameState, worldBuilder, levelName, width, height);
        }

        internal override void EndUpdate(bool hasConnections)
        {
            if (!hasConnections)
            {
                _actorsToDestroy.Clear();
                _actorsToCreate.Clear();
                _levelChange = null;
                return;
            }

            if (_levelChange != null)
            {
                SendLevelChange(_levelChange);
                _levelChange = null;

                _actorsToDestroy.Clear();
                _actorsToCreate.Clear();
                return;
            }

            SendActorsCreated();

            SendActorDestruction();

            SendLocalActorsReplication();
        }

        private void SendLevelChange(LevelChange levelChange)
        {
            var outMessage = CreateLevelChangeMessage(levelChange);

            SendMessageToIds(GetReadyConnectionIds(), outMessage, MessageDeliveryMethod.ReliableOrdered, 2);

            _readyConnections.Clear();
        }

        private INetworkMessageOut CreateLevelChangeMessage(LevelChange levelChange)
        {
            var outMessage = CreateMessage(NetworkMessageType.ServerTravel);
            outMessage.Write(levelChange.NewGameState);
            outMessage.Write(levelChange.WorldBuilder);
            outMessage.Write(levelChange.LevelName);
            outMessage.Write(levelChange.Width);
            outMessage.Write(levelChange.Height);
            return outMessage;
        }

        public override void CreateActor(INetworkedActor actor)
        {
            Actors.Add(actor);

            _actorsToCreate.Add(actor);
        }

        public override void DestroyActor(INetworkedActor actor)
        {
            actor.IsMarkedToDestroy = true;

            _actorsToDestroy.Add(actor.Id);
        }

        internal override void HandleStatusChangedConnected(INetworkMessageIn msg)
        {
            if (!_readyConnections.ContainsKey(msg.UniqueId))
            {
                _readyConnections.Add(msg.UniqueId, new ReadyConnection());

                NetworkImplementation.ClientJoinedSession(msg);

                var message = CreateLevelChangeMessage(_lastLevelChange);
                SendMessageToId(msg.UniqueId, message, MessageDeliveryMethod.ReliableOrdered, 2);
            }
        }

        internal override void HandleActorRequestPlayerActor(INetworkMessageIn msg)
        {
            if (_readyConnections.ContainsKey(msg.UniqueId) && _readyConnections[msg.UniqueId].PlayerActor != null)
                return;

            var playerName = "default"; //todo: get remote player name

            var newPlayer = CreateRemotePlayer(playerName);

            Actors.Add(newPlayer);

            var outMessage = CreateMessage(NetworkMessageType.ActorRequestPlayerActor);

            AppendActorCreationMessage(outMessage, newPlayer);

            SendMessageToSender(msg, outMessage, MessageDeliveryMethod.ReliableUnordered, 0);

            _readyConnections[msg.UniqueId].PlayerActor = newPlayer;

            SendSyncronizeActors(msg);
        }

        private void SendSyncronizeActors(INetworkMessageIn to)
        {
            var outMessage = GetActorsSendMessage(Actors);

            SendMessageToSender(to, outMessage, MessageDeliveryMethod.ReliableUnordered, 0);
        }

        internal override void HandleServerTravel(INetworkMessageIn msg)
        {
            // do exactly as if the client just connected
            _readyConnections[msg.UniqueId] = new ReadyConnection();

            HandleActorRequestPlayerActor(msg);
        }

        internal override void HandleDisconnected(INetworkMessageIn msg)
        {
            var player = _readyConnections[msg.UniqueId].PlayerActor;

            RemoveRemotePlayer(player);

            _readyConnections.Remove(msg.UniqueId);

            var message = CreateMessage(NetworkMessageType.ClientDisconnected);
            message.Write(player.Id);

            SendMessageToIds(GetReadyConnectionIds(), message, MessageDeliveryMethod.ReliableUnordered, 0);
        }

        internal override void HandleChatMessage(INetworkMessageIn msg)
        {
            var message = msg.ReadString();

            ReceivedChatMessage(message);

            NetworkImplementation.SendChatMessage(message);
        }

        private void SendLocalActorsReplication()
        {
            var localActors = Actors;

            var sendingMovementActors = new List<INetworkedActor>(localActors.Count);
            var sendingPropertiesActors = new List<INetworkedActor>(localActors.Count);

            foreach (var connection in GetReadyConnectionIds())
            {
                var connectionPlayerActor = _readyConnections[connection].PlayerActor;
                if (connectionPlayerActor == null)
                {
                    continue;
                }

                sendingMovementActors.Clear();
                sendingPropertiesActors.Clear();

                foreach (var actor in localActors)
                {
                    if (actor.ReplicateMovement && actor.IsMovementDirty && actor != connectionPlayerActor)
                    {
                        sendingMovementActors.Add(actor);
                    }
                    if (actor.ReplicateProperties && actor.IsDirty)
                    {
                        sendingPropertiesActors.Add(actor);
                    }
                }

                SendMovements(sendingMovementActors, connection);

                SendProperties(sendingPropertiesActors, connection);
            }

            foreach (var actor in localActors)
            {
                actor.IsDirty = false;
                actor.IsMovementDirty = false;
            }
        }

        private void SendProperties(ICollection<INetworkedActor> actors, long connection)
        {
            if (actors.Count == 0)
                return;

            var outPropertiesMessage = CreateMessage(NetworkMessageType.ActorPropertiesReplication);
            outPropertiesMessage.Write(actors.Count);

            foreach (var actor in actors)
            {
                outPropertiesMessage.Write(actor.Id);
                actor.Serialize(outPropertiesMessage);
            }

            SendMessageToId(connection, outPropertiesMessage, MessageDeliveryMethod.ReliableSequenced, 6);
        }

        private void SendMovements(ICollection<INetworkedActor> actors, long connection)
        {
            if (actors.Count == 0)
                return;

            var outMovementMessage = CreateMessage(NetworkMessageType.ActorReplication);
            outMovementMessage.Write(actors.Count);

            foreach (var actor in actors)
            {
                var actorLocation = actor.GetLocation();

                outMovementMessage.Write(actor.Id);
                outMovementMessage.Write(actorLocation.X);
                outMovementMessage.Write(actorLocation.Y);
            }

            SendMessageToId(connection, outMovementMessage, MessageDeliveryMethod.UnreliableSequenced, 5);
        }

        private static void AppendActorCreationMessage(INetworkMessageOut message, INetworkedActor localActor)
        {
            var actorLocation = localActor.GetLocation();

            message.Write(localActor.Id);
            message.Write(actorLocation.X);
            message.Write(actorLocation.Y);
            message.Write(CompatibilityManager.GetHashCode(localActor.GetType().Name));

            if (localActor.ReplicateProperties)
            {
                localActor.Serialize(message);
            }
        }

        private void SendActorDestruction()
        {
            var connectionIds = GetReadyConnectionIds().ToList();

            foreach (var actorId in _actorsToDestroy)
            {
                var outMessage = CreateMessage(NetworkMessageType.ActorDestruction);
                outMessage.Write(actorId);
                SendMessageToIds(connectionIds, outMessage, MessageDeliveryMethod.ReliableUnordered, 0);
            }

            _actorsToDestroy.Clear();
        }

        private void SendActorsCreated()
        {
            if (_actorsToCreate.Count <= 0)
                return;

            var outMessage = GetActorsSendMessage(_actorsToCreate);

            var connectionIds = GetReadyConnectionIds().ToList();

            SendMessageToIds(connectionIds, outMessage, MessageDeliveryMethod.ReliableUnordered, 0);

            _actorsToCreate.Clear();
        }

        private INetworkMessageOut GetActorsSendMessage(List<INetworkedActor> actors)
        {
            var outMessage = CreateMessage(NetworkMessageType.ActorCreation);

            outMessage.Write(actors.Count);
            foreach (var actor in actors)
            {
                AppendActorCreationMessage(outMessage, actor);
            }

            return outMessage;
        }

        private IEnumerable<long> GetReadyConnectionIds()
        {
            return _readyConnections.Keys;
        }

        private class LevelChange
        {
            public byte NewGameState { get; }
            public string WorldBuilder { get; }
            public string LevelName { get; }
            public int Width { get; }
            public int Height { get; }

            public LevelChange(byte newGameState, string worldBuilder, string levelName, int width, int height)
            {
                NewGameState = newGameState;
                WorldBuilder = worldBuilder;
                LevelName = levelName;
                Width = width;
                Height = height;
            }
        }

        private class ReadyConnection
        {
            public INetworkedActor PlayerActor;
        }
    }
}
