using System;
using System.Collections.Generic;
using Lidgren.Network;
using NodeGames.Network.Network;
using NodeGames.Network.Network.Implementations;
using NodeGames.Network.Network.Messages;

namespace NodeGames.Network.Lidgren
{
    public class NetworkLidgren<TNetworkPeerServer, TNetworkPeerClient> : INetworkImplementation
        where TNetworkPeerServer : NetworkPeerServer, new()
        where TNetworkPeerClient : NetworkPeerClient, new()
    {
        private NetPeer _netPeer;
        private NetworkConfiguration _configuration;
        private NetworkPeer _networkPeer;

        public NetworkLidgren(NetworkConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsConnected => _netPeer != null;
        public bool HasConnections => IsConnected && (_netPeer.ConnectionsCount > 0 || _netPeer.Connections.Count > 0);

        public NetworkStats Stats
            =>
                _netPeer?.Statistics == null
                    ? new NetworkStats()
                    : new NetworkStats(_netPeer.Statistics.SentBytes, _netPeer.Statistics.SentBytes,
                        _netPeer.Statistics.SentPackets, _netPeer.Statistics.ReceivedPackets,
                        _netPeer.Statistics.SentMessages, _netPeer.Statistics.ReceivedMessages);

        public NetworkConfiguration GetCurrentConfiguration()
        {
            return _configuration;
        }

        public void CreateSession(string sessionName)
        {
            _networkPeer = new TNetworkPeerServer { NetworkImplementation = this };

            var config = new NetPeerConfiguration(sessionName)
            {
                Port = _configuration.ServerPort
            };
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            _netPeer = new NetServer(config);
            _netPeer.Start();
        }

        public void ListSessions()
        {
            //_netPeer.DiscoverLocalPeers(_configuration.ServerPort);
        }

        public void JoinSession(string sessionName)
        {
            _networkPeer = new TNetworkPeerClient { NetworkImplementation = this };

            var config = new NetPeerConfiguration(sessionName)
            {
                Port = _configuration.ClientPort + new Random().Next(0, 50)
            };

            _netPeer = new NetClient(config);
            _netPeer.Start();

            var approval = _netPeer.CreateMessage();
            approval.Write(_networkPeer.GetApprovalString());

            _netPeer.Connect(_configuration.ConnectionIp, _configuration.ServerPort, approval);
        }

        public void LeaveSession()
        {
            _netPeer?.Shutdown("leaving session");
        }

        public void ClientJoinedSession(INetworkMessageIn msg)
        {
        }

        public void ApproveMessage(INetworkMessageIn message)
        {
            var msg = ((NetworkMessageInLidgren)message).GetInternalMessage();
            msg.SenderConnection.Approve();
        }

        public void DisapproveMessage(INetworkMessageIn message)
        {
            var msg = ((NetworkMessageInLidgren)message).GetInternalMessage();
            msg.SenderConnection.Deny();
        }

        public void Update(float tickTime)
        {
            _networkPeer.Update(tickTime);
        }

        public void CreateActor(INetworkedActor actor)
        {
            _networkPeer.CreateActor(actor);
        }

        public void DestroyActor(INetworkedActor actor)
        {
            _networkPeer.DestroyActor(actor);
        }

        public void ServerTravel(byte newGameState, string worldBuilder, string levelName, int width, int height)
        {
            var server = _networkPeer as NetworkPeerServer;
            server?.SendServerTravel(newGameState, worldBuilder, levelName, width, height);
        }

        public void CallMethodOnServer(int actorRemoteId, string methodName, bool reliable, params object[] parameters)
        {
            if (!IsConnected)
                return;

            var client = _networkPeer as NetworkPeerClient;
            client?.AddActorRemoteMethodCall(actorRemoteId, methodName, reliable, parameters);
        }

        public void CallMethodOnClients(int actorRemoteId, string methodName, bool reliable, object[] parameters)
        {
            if (!IsConnected)
                return;

            var server = _networkPeer as NetworkPeerServer;
            server?.AddActorRemoteMethodCall(actorRemoteId, methodName, reliable, parameters);
        }

        public INetworkMessageIn GetNextMessage()
        {
            var message = _netPeer.ReadMessage();
            if (message == null)
            {
                return null;
            }

            var inMessage = new NetworkMessageInLidgren(message, _netPeer);
            inMessage.Decrypt();

            return inMessage;
        }

        public INetworkMessageOut CreateMessage(NetworkMessageType type)
        {
            var message = _netPeer.CreateMessage();
            message.Write((short)type);

            return new NetworkMessageOutLidgren(message, _netPeer);
        }

        public void SendMessageToAll(INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel)
        {
            message.Encrypt();

            var outMessage = ((NetworkMessageOutLidgren)message).GetInternalMessage();
            _netPeer.SendMessage(outMessage, _netPeer.Connections, deliveryMethod.ToDeliveryMethod(), channel);
        }

        public void SendMessageToSender(INetworkMessageIn to, INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel)
        {
            message.Encrypt();
            var outMessage = ((NetworkMessageOutLidgren)message).GetInternalMessage();

            var inMessage = ((NetworkMessageInLidgren)to).GetInternalMessage();

            _netPeer.SendMessage(outMessage, inMessage.SenderConnection, deliveryMethod.ToDeliveryMethod(), channel);
        }

        public void SendMessageToIds(IEnumerable<long> uniqueIds, INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel)
        {
            message.Encrypt();

            var connections = new List<NetConnection>(4);
            foreach (var uniqueId in uniqueIds)
            {
                for (int i = 0; i < _netPeer.Connections.Count; i++)
                {
                    var connection = _netPeer.Connections[i];
                    if (connection.RemoteUniqueIdentifier == uniqueId)
                    {
                        connections.Add(connection);
                        break;
                    }
                }
            }

            var outMessage = ((NetworkMessageOutLidgren)message).GetInternalMessage();

            try
            {
                _netPeer.SendMessage(outMessage, connections, deliveryMethod.ToDeliveryMethod(), channel);
            }
            catch (Exception ex)
            {
                // todo: throw?
            }
        }

        public void SendMessageToId(long uniqueId, INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel)
        {
            message.Encrypt();

            var outMessage = ((NetworkMessageOutLidgren)message).GetInternalMessage();
            for (int i = 0; i < _netPeer.Connections.Count; i++)
            {
                var connection = _netPeer.Connections[i];
                if (connection.RemoteUniqueIdentifier == uniqueId)
                {
                    _netPeer.SendMessage(outMessage, connection, deliveryMethod.ToDeliveryMethod(), channel);
                    break;
                }
            }
        }

        public void SendChatMessage(string text)
        {
            var message = CreateMessage(NetworkMessageType.ChatMessage);
            message.Write(text);

            SendMessageToAll(message, MessageDeliveryMethod.Unreliable, 0);
        }
    }
}
