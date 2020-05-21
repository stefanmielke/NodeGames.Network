using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NodeGames.Network.Network;
using NodeGames.Network.Network.Messages;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace PongCore.Actors
{
    public abstract class Actor : INetworkedActor
    {
        // current location - created for the sample
        public Vector2 Location;

        // bounding box of the Bars - created for the sample
        public Rectangle BoundBox;

        // if this actor is controlled remotely (eg.: this client doesn't own it)
        protected bool IsRemote { get; }

        protected Actor(int id, Vector2 initialLocation, Rectangle boundBox, bool isRemote)
        {
            // set to true if you want to replicate this actor movement
            ReplicateMovement = true;

            // set to true if any property will be serialized/deserialized
            ReplicateProperties = false;

            Id = id;

            Location = initialLocation;
            BoundBox = boundBox;
            IsRemote = isRemote;
        }

        public abstract void Update(GameTime gameTime, bool isActive, List<Actor> actors);

        public abstract void Draw(SpriteBatch spriteBatch);

        #region INetworkedActor

        // unique Id of this actor, is unique across all instances
        public int Id { get; }

        // if it will replicate any property
        public bool ReplicateProperties { get; protected set; }

        // if it will replicate its movement (based on the Location property)
        public bool ReplicateMovement { get; protected set; }

        // if any property needs to be replicated
        public bool IsDirty { get; set; }

        // if the movement needs to be replicated
        public bool IsMovementDirty { get; set; }

        // if this actor should be destroyed
        public bool IsMarkedToDestroy { get; set; }

        /// <summary>
        /// This code is called by the server to set the location on clients.
        /// </summary>
        public void SetLocation(int x, int y)
        {
            Location.X = x;
            Location.Y = y;
        }

        /// <summary>
        /// This code only runs on clients, never on the server, and is used to destroy the current actor
        /// </summary>
        public void RemoteDestroyed()
        {
        }

        /// <summary>
        /// This code prepares the message on the server to send properties to all the clients when IsDirty is true.
        /// </summary>
        public virtual void Serialize(INetworkMessageOut message)
        {
        }

        /// <summary>
        /// This code gets the values in the same order sent by the server on all clients.
        /// 
        /// Remember to keep the exact same type serialized and in the same order.
        /// </summary>
        /// <param name="message"></param>
        public virtual void Deserialize(INetworkMessageIn message)
        {
        }

        /// <summary>
        /// Used by the server to know where the actor is.
        /// </summary>
        NodeGames.Network.Network.Point INetworkedActor.GetLocation()
        {
            return new NodeGames.Network.Network.Point
            {
                X = (int) Location.X,
                Y = (int) Location.Y
            };
        }

        #endregion
    }
}