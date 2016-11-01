using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Shapes;

namespace Pong.Actors
{
    public class Ball : Actor
    {
        private Vector2 _speed;
        private readonly Random _random;

        public Ball(int id, Vector2 initialLocation, bool isRemote) : base(id, initialLocation, new Rectangle(0, 0, 20, 20), isRemote)
        {
            _random = new Random();

            var xSpeed = _random.Next(0, 2) == 0 ? -.4f : .4f;
            var ySpeed = GetRandomNumber(-.4f, .4f);

            _speed = new Vector2(xSpeed, ySpeed);
        }

        public override void Update(GameTime gameTime, bool isActive, List<Actor> actors)
        {
            // only update the ball on the server
            // you should update only the speed vector and let the client calculate as an optimization, but we'll keep it simple for now.
            if (!IsRemote)
            {
                IsMovementDirty = true;

                Location += _speed * gameTime.ElapsedGameTime.Milliseconds;

                if (Location.X > 800 - BoundBox.Width || Location.X < 0 || actors.OfType<Bar>().Any(CollidesWith))
                {
                    _speed.X *= -1;
                }
                if (Location.Y > 500 - BoundBox.Height || Location.Y < 0)
                {
                    _speed.Y *= -1;
                }
            }
        }

        private bool CollidesWith(Actor bar)
        {
            var barBox = bar.BoundBox;
            barBox.Offset(bar.Location);

            var selfBox = BoundBox;
            selfBox.Offset(Location);

            return selfBox.Intersects(barBox);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var box = BoundBox;
            box.Offset(Location);

            spriteBatch.DrawCircle(box.Center.ToVector2(), 10, 10, Color.White);
        }

        private float GetRandomNumber(float minimum, float maximum)
        {
            return (float)(_random.NextDouble() * (maximum - minimum) + minimum);
        }
    }
}
