﻿namespace NodeGames.Network.Network
{
    public enum NetworkType
    {
        None,
        Server,
        Client
    }

    public enum NetworkMessageType : short
    {
        None,
        ConnectionApproval,
        Connected,
        Disconnected,

        /// <summary>
        /// id;x;y
        /// </summary>
        ActorReplication,

        /// <summary>
        /// id;x;y
        /// </summary>
        PlayerCreation,

        /// <summary>
        /// id;x;y;classname;serializedProperties
        /// </summary>
        ActorCreation,

        /// <summary>
        /// from server: id;x;y;classname;serializedProperties
        /// from client: name
        /// </summary>
        ActorRequestPlayerActor,

        /// <summary>
        /// id;properties
        /// </summary>
        ActorPropertiesReplication,

        /// <summary>
        /// id;method;parameterCount;params
        /// </summary>
        ActorRemoteMethodCall,

        /// <summary>
        /// id
        /// </summary>
        ActorDestruction,

        /// <summary>
        /// from server: gameState;worldBuilder;levelName;width;height
        /// from client: "empty message"
        /// </summary>
        ServerTravel,

        /// <summary>
        /// id
        /// </summary>
        ClientDisconnected,

        /// <summary>
        /// id;text
        /// </summary>
        ChatMessage
    }

    public enum NetworkRemoteCallParameterType : byte
    {
        Int,
        Float,
        String,
        Boolean
    }
}
