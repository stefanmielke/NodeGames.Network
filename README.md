# NodeGames.Network

Repository for the high-level gaming network library.


### Usage (based on the sample project)
Some of the code below assumes you use MonoGame, but you can change anything specific to any other framework or library.

- install from nuget:

```
dotnet add package NodeGames.Network
dotnet add package NodeGames.Network.Lidgren
dotnet add package Lidgren.Network.Core2
```

- create Server class from ```NetworkPeerServer```

```csharp
public class NetworkPeerGameServer : NetworkPeerServer
{
    // first parameter is how many times per miliseconds it should update (so it can skip if needed)
    // second parameter is the assembly with all the actors in the game
    public NetworkPeerGameServer() : base(1, typeof(NetworkPeerGameServer).Assembly)
    {
    }

    /// <summary>
    /// Use this method to allow a client to connect.
    /// 
    /// Here you should check if the approvalMessage is the same as in the client,
    /// if the room is not full and if the state of the game allows clients to connect (i.e. in a lobby).
    /// </summary>
    /// <param name="approvalMessage">Comes from "NetworkPeerGameClient.GetApprovalString"</param>
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
```

- create Client class from ```NetworkPeerClient```

```csharp
public class NetworkPeerGameClient : NetworkPeerClient
{
    private readonly Dictionary<int, Type> _hashedNames = new Dictionary<int, Type>();

    public NetworkPeerGameClient() : base(1, typeof(NetworkPeerGameClient).Assembly)
    {
    	// this method has to store all types to create later
    	// you can also use Reflection to grab all actors
    
        _hashedNames.Add(CompatibilityManager.GetHashCode(nameof(Bar)), typeof(Bar));
        _hashedNames.Add(CompatibilityManager.GetHashCode(nameof(Ball)), typeof(Ball));
        _hashedNames.Add(CompatibilityManager.GetHashCode(nameof(GameState)), typeof(GameState));
        
        // we use "CompatibilityManager.GetHashCode" because of differences between platforms
        // so we guarantee that it has the same name across clients on different OS
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
        Actor actor = null;

        if (_hashedNames.ContainsKey(hashedClassName))
        {
	        // here we create the object based on the name given, on the position sent

            actor = Activator.CreateInstance(_hashedNames[hashedClassName], id, new Vector2(x, y), true) as Actor;

            if (actor != null)
                PongGame.Instance.Actors.Add(actor);
        }

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
        // go back to menu or show message to the player
    }

    /// <summary>
    /// What happens when another client disconnects?
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
        // show the message to the player on a chat box or as an alert
    }

    /// <summary>
    /// Return a string unique to this game. This string is used to validate the clients, making sure it's from this game.
    /// </summary>
    /// <returns></returns>
    public override string GetApprovalString()
    {
    	// you could also add something unique to this game, and maybe also include the version
		// this will get sent when joining a room on the method "NetworkPeerGameServer.ApproveConnection"
        return "PongGame";
    }
}
```

- create a base actor from ```INetworkedActor```

```csharp
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
            X = (int)Location.X,
            Y = (int)Location.Y
        };
    }

    #endregion
}
```

- use your new classes
 - instantiate your Network:

```csharp
Network = new NetworkLidgren<NetworkPeerGameServer, NetworkPeerGameClient>(
	new NetworkConfiguration
{
    ClientPort = 8082, // port for the client
    ConnectionIp = "127.0.0.1", // change to the server IP address
    ExternalIp = false, // not used by the current implementations
    ServerPort = 8081, // port for the server - use a different port if running on the same machine
    ServerTick = 60, // server ticks per second, the higher the more accurate, but uses more network traffic
    Type = NetworkType.Server // or "NetworkType.Client" if is the client connecting
}
);
```
 - create a session on the server, or join an existing session

```csharp
// on server
Network.CreateSession("gameSessionName"); // on the server
Network.ServerTravel(1, "defaultWorldBuilder", "defaultLevelName", 800, 600); // change the map

// on client
Network.JoinSession("gameSessionName"); // on the client
```
 - update networking every frame
 
```csharp
Network.Update((float)gameTime.TotalGameTime.TotalMilliseconds);
```

- create your actor classes (examples on the sample under "Actors")

- instantiate and destroy objects - only on the server

```csharp
// create a new actor
Network.CreateActor(actor);

// destroy an actor
Network.DestroyActor(actor);
```

- other useful methods

```csharp
// call method from a client to the server
Network.CallMethodOnServer(Id, "MethodName", true /* if we want to make sure the message is sent */, args...);

// call method from the server to all clients
Network.CallMethodOnClients(Id, "MethodName", true /* if we want to make sure the message is sent */, args...);

// send chat messages (or any text message)
Network.SendChatMessage("text here");
```

### Network Stats

You can get network stats from the ```Stats```property that is inside the Network implementation classes.

```csharp
struct NetworkStats
{
    int BytesSent;
    int BytesReceived;
    int PacketsSent;
    int PacketsReceived;
    int MessagesSent;
    int MessagesReceived;
}
```

### Performance Considerations

- Do not serialize movement data. It's already covered on a different path inside the library.
- Setting ```ReplicateProperties``` and ```ReplicateMovement``` correctly may save a lot of bandwidth.
 - If an actor doesn't change movement during its lifetime (eg.: a static object, or a bullet), prefer to push the movement on creation, and do not set ```ReplicateMovement``` to true, relying on the server to destroy the object when needed.
 - Set the ```IsDirty``` and ```IsMovementDirty``` flags only when needed. That is used internally to transmit data across clients.

### Network Flow

TODO
