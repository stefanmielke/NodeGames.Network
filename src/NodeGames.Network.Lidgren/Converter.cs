﻿using Lidgren.Network;
using NodeGames.Network.Network.Messages;

namespace NodeGames.Network.Lidgren
{
    internal static class Converter
    {
        public static NetDeliveryMethod ToDeliveryMethod(this MessageDeliveryMethod deliveryMethod)
        {
            switch (deliveryMethod)
            {
                case MessageDeliveryMethod.Unreliable:
                    return NetDeliveryMethod.Unreliable;
                case MessageDeliveryMethod.UnreliableSequenced:
                    return NetDeliveryMethod.UnreliableSequenced;
                case MessageDeliveryMethod.ReliableUnordered:
                    return NetDeliveryMethod.ReliableUnordered;
                case MessageDeliveryMethod.ReliableSequenced:
                    return NetDeliveryMethod.ReliableSequenced;
                case MessageDeliveryMethod.ReliableOrdered:
                    return NetDeliveryMethod.ReliableOrdered;
                default:
                    return NetDeliveryMethod.Unknown;
            }
        }
    }
}
