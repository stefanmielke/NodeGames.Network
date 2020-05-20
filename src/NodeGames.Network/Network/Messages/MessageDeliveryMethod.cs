﻿namespace NodeGames.Network.Network.Messages
{
    public enum MessageDeliveryMethod
    {
        /// <summary>
        /// Unreliable, unordered delivery
        /// </summary>
        Unreliable,

        /// <summary>
        /// Unreliable delivery, but automatically dropping late messages
        /// </summary>
        UnreliableSequenced,

        /// <summary>
        /// Reliable delivery, but unordered
        /// </summary>
        ReliableUnordered,

        /// <summary>
        /// Reliable delivery, except for late messages which are dropped
        /// </summary>
        ReliableSequenced,

        /// <summary>
        /// Reliable, ordered delivery
        /// </summary>
        ReliableOrdered
    }
}
