﻿using Microsoft.Xna.Framework;
using NodeGames.Network.Network;
using Pong.Actors;

namespace Pong.Network
{
    public class NetworkPeerGameClient : NetworkPeerClient
    {
        private readonly int _barHashedName;

        public NetworkPeerGameClient() : base(1, typeof(NetworkPeerGameClient).Assembly)
        {
            _barHashedName = CompatibilityManager.GetHashCode(typeof(Bar).Name);
        }

        /// <summary>
        /// Create a remote actor on a client based on its hashed name.
        /// </summary>
        /// <param name="hashedClassName">String hashed with CompatibilityManager.GetHashCode(Actor.GetType().Name)</param>
        /// <param name="id">Remote Id of the actor.</param>
        /// <param name="x">X position of the actor on the server.</param>
        /// <param name="y">Y position of the actor on the server.</param>
        /// <returns></returns>
        protected override INetworkedActor CreateRemoteActorByName(int hashedClassName, int id, int x, int y)
        {
            Actor actor;
            if (hashedClassName == _barHashedName)
                actor = new Bar(id, new Vector2(x, y), true);
            else
                actor = new Ball(id, new Vector2(x, y), true);

            PongGame.Instance.Actors.Add(actor);
            return actor;
        }

        /// <summary>
        /// Create a local player on the client.
        /// </summary>
        /// <param name="id">Remote Id of the actor owned by this client.</param>
        /// <param name="x">X position of the actor on the server.</param>
        /// <param name="y">Y position of the actor on the server.</param>
        /// <returns></returns>
        protected override INetworkedActor CreateLocalPlayer(int id, int x, int y)
        {
            var playerBar = new Bar(id, new Vector2(x, y), false);

            PongGame.Instance.Actors.Add(playerBar);

            return playerBar;
        }

        /// <summary>
        /// Change the game state to the one sent
        /// </summary>
        protected override void ChangeGameState(byte newGameState, string levelName)
        {
            // we don't do anything on pong example, but you should load a new map based on the parameters sent
        }

        /// <summary>
        /// What happens when this instance disconnects?
        /// </summary>
        protected override void HandleDisconnected()
        {
            // go back to menu?
        }

        /// <summary>
        /// What happens when a client disconnects?
        /// </summary>
        /// <param name="id">Id of the disconnected client</param>
        protected override void HandlePlayerDisconnected(int id)
        {
            // probably remove the player actor from the game
        }

        /// <summary>
        /// What happens when this instance receives a text chat?
        /// </summary>
        protected override void ReceivedChatMessage(string text)
        {
            // show the message to the player?
        }

        /// <summary>
        /// Return a string unique to this game. This string is used to validate the clients, making sure it's from this game.
        /// </summary>
        /// <returns></returns>
        public override string GetApprovalString()
        {
            return "PongGame";
        }
    }

    public static class CompatibilityManager
    {
        public static int GetHashCode(string name)
        {
            if (string.IsNullOrEmpty(name))
                return 0;

            unchecked
            {
                int hash = 23;
                foreach (char c in name)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }
    }
}
