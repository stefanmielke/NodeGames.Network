using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.BitmapFonts;
using NodeGames.Network.Lidgren;
using NodeGames.Network.Network;
using NodeGames.Network.Network.Implementations;
using Pong.Actors;
using Pong.Network;

namespace Pong
{
    public class PongGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public INetworkImplementation Network { get; private set; }
        public BitmapFont Font { get; private set; }

        public static PongGame Instance { get; private set; }

        public List<Actor> Actors { get; }

        private bool _started;

        public PongGame()
        {
            _started = false;
            Actors = new List<Actor>(3);

            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            Instance = this;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            Font = Content.Load<BitmapFont>("font/Big");
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.Escape))
                Exit();

            if (!_started)
            {
                if (keyboardState.IsKeyDown(Keys.D1))
                {
                    StartGame(true);
                }
                else if (keyboardState.IsKeyDown(Keys.D2))
                {
                    StartGame(false);
                }

                return;
            }

            Network.Update((float)gameTime.TotalGameTime.TotalMilliseconds);

            foreach (var actor in Actors)
            {
                actor.Update(gameTime, IsActive, Actors);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            if (!_started)
            {
                _spriteBatch.DrawString(Font, "Press '1' to create, '2' to join", new Vector2(200,380), Color.White);
            }

            foreach (var actor in Actors)
            {
                actor.Draw(_spriteBatch);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void StartGame(bool isServer)
        {
            _started = true;
            if (isServer)
            {
                // actors controlled by the server
                Actors.Add(new Bar(1, new Vector2(10, 10), false));
                Actors.Add(new Ball(2, new Vector2(400, 200), false));
                Actors.Add(new GameState(4, new Vector2(400, 10), false));

                Network = new NetworkLidgren<NetworkPeerGameServer, NetworkPeerGameClient>(new NetworkConfiguration
                {
                    ClientPort = 8082,
                    ConnectionIp = "127.0.0.1",
                    ExternalIp = false,
                    ServerPort = 8081,
                    ServerTick = 60,
                    Type = NetworkType.Server
                });

                Network.CreateSession("gameSession");

                Network.ServerTravel(1, "default", "default", 800, 600);

                foreach (var actor in Actors)
                {
                    Network.CreateActor(actor);
                }
            }
            else
            {
                Network = new NetworkLidgren<NetworkPeerGameServer, NetworkPeerGameClient>(new NetworkConfiguration
                {
                    ClientPort = 8082,
                    ConnectionIp = "127.0.0.1",
                    ExternalIp = false,
                    ServerPort = 8081,
                    ServerTick = 60,
                    Type = NetworkType.Client
                });

                Network.JoinSession("gameSession");
            }
        }
    }
}
