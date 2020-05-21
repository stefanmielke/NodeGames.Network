﻿using System.Linq;
using Microsoft.Xna.Framework;
using NodeGames.Network.Network;
using PongCore.Actors;

namespace PongCore.Network
{
    public class NetworkPeerGameServer : NetworkPeerServer
    {
        public NetworkPeerGameServer() : base(1, typeof(NetworkPeerGameServer).Assembly)
        {
        }

        /// <summary>
        /// Use this method to allow a client to connect.
        /// 
        /// Here you should check if the approvalMessage is the same as in the client ("PongGame" in this game), if the room is not full
        /// and if the state of the game allows clients to connect (i.e. in a lobby).
        /// </summary>
        /// <param name="approvalMessage"></param>
        protected override bool ApproveConnection(string approvalMessage)
        {
            return approvalMessage == "PongGame" && PongGame.Instance.Actors.OfType<Bar>().Count() == 1;
        }

        /// <summary>
        /// Create a remote player on the server.
        /// </summary>
        /// <param name="playerName">Name of the player on the client (not implemented yet)</param>
        protected override INetworkedActor CreateRemotePlayer(string playerName)
        {
            var remoteBar = new Bar(3, new Vector2(770, 10), true);

            PongGame.Instance.Actors.Add(remoteBar);

            return remoteBar;
        }

        /// <summary>
        /// Remove a remote player from the game.
        /// </summary>
        /// <param name="player"></param>
        protected override void RemoveRemotePlayer(INetworkedActor player)
        {
            // do nothing in this example
        }

        /// <summary>
        /// What happens when this instance receives a text chat?
        /// </summary>
        protected override void ReceivedChatMessage(string text)
        {
            // show the message to the player?
        }
    }
}
