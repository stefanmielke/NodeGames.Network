﻿using System.Collections.Generic;
using NodeGames.Network.Network.Messages;

namespace NodeGames.Network.Network.Implementations
{
    public interface INetworkImplementation
    {
        bool IsConnected { get; }
        bool HasConnections { get; }
        NetworkStats Stats { get; }

        NetworkConfiguration GetCurrentConfiguration();

        void CreateSession(string sessionName);
        void ListSessions();
        void JoinSession(string sessionName);
        void LeaveSession();

        void ClientJoinedSession(INetworkMessageIn msg);

        void ApproveMessage(INetworkMessageIn message);
        void DisapproveMessage(INetworkMessageIn message);

        void Update(float tickTime);

        void CreateActor(INetworkedActor actor);
        void DestroyActor(INetworkedActor actor);
        void ServerTravel(byte newGameState, string worldBuilder, string levelName, int width, int height);
        void CallMethodOnServer(int actorRemoteId, string methodName, bool reliable, params object[] parameters);

        INetworkMessageIn? GetNextMessage();
        INetworkMessageOut CreateMessage(NetworkMessageType type);
        void SendMessageToAll(INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel);
        void SendMessageToSender(INetworkMessageIn to, INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel);
        void SendMessageToIds(IEnumerable<long> uniqueIds, INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel);
        void SendMessageToId(long uniqueId, INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel);
        void SendChatMessage(string text);
        void CallMethodOnClients(int actorRemoteId, string methodName, bool reliable, object[] parameters);
    }
}