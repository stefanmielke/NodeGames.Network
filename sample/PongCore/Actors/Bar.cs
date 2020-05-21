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
            if (IsRemote)
            {
                Location.Y = y;
            }
        }

        public override void Update(GameTime gameTime, bool isActive, List<Actor> actors)
        {
            // the keyboard will only work on the owner and on an active window (so you can test both on the same machine)
            if (!IsRemote && isActive)
            {
                var keyboardState = Keyboard.GetState();

                if (keyboardState.IsKeyDown(Keys.Up))
                {
                    IsMovementDirty = true;
                    Location.Y -= _speed * gameTime.ElapsedGameTime.Milliseconds;
                }
                if (keyboardState.IsKeyDown(Keys.Down))
                {
                    IsMovementDirty = true;
                    Location.Y += _speed * gameTime.ElapsedGameTime.Milliseconds;
                }

                PongGame.Instance.Network.CallMethodOnServer(Id, "SetLocationRemote", false, (int)Location.Y);
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawRectangle(new RectangleF(Location.X, Location.Y, BoundBox.Width, BoundBox.Height), Color.White);
        }
    }
}
