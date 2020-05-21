﻿using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using NodeGames.Network;

namespace PongCore.Actors
{
    public class Bar : Actor
    {
        private readonly float _speed;

        public Bar(int id, Vector2 initialLocation, bool isRemote) : base(id, initialLocation, new Rectangle(0, 0, 20, 100), isRemote)
        {
            _speed = .5f;
        }

        [RemoteCallable]
        public void SetLocationRemote(int y)
        {
            // if it is not controlled by the current client, we set the location (only Y for the bar)
            if (IsRemote)
            {
                Location.Y = y;
            }
        }

        public override void Update(GameTime gameTime, bool isActive, List<Actor> actors)
        {
            // the keyboard will only work on the owner and on an active window (so you can test both on the same machine)
            // and we should only update the position if we are the owner of the Bar
            if (!IsRemote && isActive)
            {
                var keyboardState = Keyboard.GetState();

                if (keyboardState.IsKeyDown(Keys.Up))
                {
                    // we have to set the flag so this client will know that we have to update the others
                    IsMovementDirty = true;
                    Location.Y -= _speed * gameTime.ElapsedGameTime.Milliseconds;
                }
                if (keyboardState.IsKeyDown(Keys.Down))
                {
                    // we have to set the flag so this client will know that we have to update the others
                    IsMovementDirty = true;
                    Location.Y += _speed * gameTime.ElapsedGameTime.Milliseconds;
                }

                // we call the method on the server to update the other clients (and the server)
                PongGame.Instance.Network.CallMethodOnServer(Id, "SetLocationRemote", false, (int)Location.Y);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawRectangle(new RectangleF(Location.X, Location.Y, BoundBox.Width, BoundBox.Height), Color.White);
        }
    }
}
