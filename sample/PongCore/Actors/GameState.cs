﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using NodeGames.Network.Network.Messages;

namespace PongCore.Actors
{
    /// <summary>
    /// Class to keep the game state synchronized between machines.
    /// </summary>
    public class GameState : Actor
    {
        private int _pointsLeft;
        private int _pointsRight;
        private string _score;

        public GameState(int id, Vector2 initialLocation, bool isRemote) : base(id, initialLocation, Rectangle.Empty, isRemote)
        {
            _pointsLeft = 0;
            _pointsRight = 0;

            // we'll replicate the points, so we have to mark this as true
            ReplicateProperties = true;

            // we'll NOT replicate the movement (it won't move), so we mark this as false
            ReplicateMovement = false;

            ReconstructScore();
        }

        public override void Update(GameTime gameTime, bool isActive, List<Actor> actors)
        {
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(PongGame.Instance.Font, _score, Location, Color.White);
        }

        public void AddPointLeft()
        {
            _pointsLeft++;
            ReconstructScore();

            IsDirty = true;
        }

        public void AddPointRight()
        {
            _pointsRight++;
            ReconstructScore();

            IsDirty = true;
        }

        /// <summary>
        /// Set the correct score message based on the current score.
        /// </summary>
        private void ReconstructScore()
        {
            _score = $"{_pointsLeft} - {_pointsRight}";
        }

        /// <summary>
        /// Runs on server.
        /// </summary>
        public override void Serialize(INetworkMessageOut message)
        {
            // remember to write on the same sequence that we read
            message.Write(_pointsLeft);
            message.Write(_pointsRight);

            // always call the base Serialize to ensure all properties are replicated
            base.Serialize(message);
        }

        /// <summary>
        /// Runs on client.
        /// </summary>
        public override void Deserialize(INetworkMessageIn message)
        {
            // remember to read on the same sequence that we write
            _pointsLeft = message.ReadInt();
            _pointsRight = message.ReadInt();

            ReconstructScore();

            // always call the base Deserialize to ensure all properties are replicated
            base.Deserialize(message);
        }
    }
}
