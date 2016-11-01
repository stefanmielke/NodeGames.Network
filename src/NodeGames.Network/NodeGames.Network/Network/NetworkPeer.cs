using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NodeGames.Network.Network.Implementations;
using NodeGames.Network.Network.Messages;

namespace NodeGames.Network.Network
{
    public abstract class NetworkPeer
    {
        private readonly Dictionary<int, string> _remoteMethods;

        /// <summary>
        /// Dictionary holding the actorId, hashedMethodName, and the whole call.
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, CallMethod>> _methodCalls;

        public INetworkImplementation NetworkImplementation { protected get; set; }
        protected readonly List<INetworkedActor> Actors;

        public static bool Updated { get; private set; }
        private static double _currentUpdateSkip;
        private static double _startedUpdate;
        private readonly float _tickTimesPerSecond;

        protected NetworkPeer(float tickTimesPerSecond, Assembly actorsAssembly)
        {
            _tickTimesPerSecond = tickTimesPerSecond;
            Actors = new List<INetworkedActor>();
            _remoteMethods = new Dictionary<int, string>();
            _methodCalls = new Dictionary<int, Dictionary<int, CallMethod>>(5);

            LoadRemoteMethods(actorsAssembly);
        }

        private void LoadRemoteMethods(Assembly actorsAssembly)
        {
            var networkedActorType = typeof(INetworkedActor);
            var clientCallableAttributeType = typeof(RemoteCallableAttribute);

            foreach (var actorType in actorsAssembly.GetTypes().Where(t => t.GetInterfaces().Contains(networkedActorType)))
            {
                foreach (var methodInfo in actorType.GetMethods().Where(m => m.GetCustomAttribute(clientCallableAttributeType) != null))
                {
                    var methodName = methodInfo.Name;
                    var methodKey = CompatibilityManager.GetHashCode(methodName);

                    if (!_remoteMethods.ContainsKey(methodKey))
                    {
                        _remoteMethods.Add(methodKey, methodName);
                    }
                }
            }
        }

        public void Update(double totalMilliseconds)
        {
            Updated = false;
            
            // todo: fix calculation here
            _currentUpdateSkip = totalMilliseconds - _startedUpdate;
            if (_currentUpdateSkip < _tickTimesPerSecond)
                return;

            _currentUpdateSkip = 0;
            _startedUpdate = totalMilliseconds;

            Updated = true;

            if (!NetworkImplementation.IsConnected)
            {
                _methodCalls.Clear();
                return;
            }

            var traveled = false;

            Actors.RemoveAll(a => a.IsMarkedToDestroy);

            INetworkMessageIn msg;
            while ((msg = NetworkImplementation.GetNextMessage()) != null)
            {
                switch (msg.GetMessageType())
                {
                    case NetworkMessageType.ConnectionApproval:
                        HandleConnectionApproval(msg);
                        break;
                    case NetworkMessageType.Connected:
                        HandleStatusChangedConnected(msg);
                        break;
                    case NetworkMessageType.ActorReplication:
                        HandleActorReplication(msg);
                        break;
                    case NetworkMessageType.PlayerCreation:
                        HandlePlayerCreation(msg);
                        break;
                    case NetworkMessageType.ActorCreation:
                        HandleActorCreation(msg);
                        break;
                    case NetworkMessageType.ActorRequestPlayerActor:
                        HandleActorRequestPlayerActor(msg);
                        break;
                    case NetworkMessageType.ActorPropertiesReplication:
                        if (traveled) break;
                        HandleActorPropertiesReplication(msg);
                        break;
                    case NetworkMessageType.ActorRemoteMethodCall:
                        if (traveled) break;
                        HandleActorRemoteMethodCall(msg);
                        break;
                    case NetworkMessageType.ActorDestruction:
                        if (traveled) break;
                        HandleActorDestruction(msg);
                        break;
                    case NetworkMessageType.ServerTravel:
                        HandleServerTravel(msg);
                        traveled = true;
                        break;
                    case NetworkMessageType.Disconnected:
                        HandleDisconnected(msg);
                        break;
                    case NetworkMessageType.ClientDisconnected:
                        HandleClientDisconnected(msg);
                        break;
                    case NetworkMessageType.ChatMessage:
                        HandleChatMessage(msg);
                        break;
                }

                msg.Recycle();
            }

            if (NetworkImplementation.HasConnections)
            {
                foreach (var call in _methodCalls.Values.SelectMany(actor => actor.Values))
                {
                    SendActorRemoteMethodCall(call.ActorRemoteId, call.MethodName, call.Reliable, call.Parameters);
                }
            }
            _methodCalls.Clear();

            EndUpdate(NetworkImplementation.HasConnections);
        }

        private void SendActorRemoteMethodCall(int actorRemoteId, int methodName, bool reliable, params object[] parameters)
        {
            var outMessage = CreateMessage(NetworkMessageType.ActorRemoteMethodCall);
            outMessage.Write(actorRemoteId);
            outMessage.Write(methodName);
            outMessage.Write((byte)parameters.Length);

            foreach (var param in parameters)
            {
                var paramType = param.GetType();
                if (paramType == typeof(int))
                {
                    outMessage.Write((byte)NetworkRemoteCallParameterType.Int);
                    outMessage.Write((int)param);
                }
                else if (paramType == typeof(string))
                {
                    outMessage.Write((byte)NetworkRemoteCallParameterType.String);
                    outMessage.Write((string)param);
                }
                else if (paramType == typeof(float))
                {
                    outMessage.Write((byte)NetworkRemoteCallParameterType.Float);
                    outMessage.Write((float)param);
                }
                else if (paramType == typeof(bool))
                {
                    outMessage.Write((byte)NetworkRemoteCallParameterType.Boolean);
                    outMessage.Write((bool)param);
                }
            }

            SendMessageToAll(outMessage, reliable ? MessageDeliveryMethod.ReliableOrdered : MessageDeliveryMethod.Unreliable, 0);
        }

        public void AddActorRemoteMethodCall(int actorRemoteId, string methodName, bool reliable, object[] parameters)
        {
            var hashedMethodName = CompatibilityManager.GetHashCode(methodName);

            var methodCall = new CallMethod(actorRemoteId, hashedMethodName, reliable, parameters);

            if (!_methodCalls.ContainsKey(actorRemoteId))
            {
                _methodCalls.Add(actorRemoteId, new Dictionary<int, CallMethod>(1));
            }

            if (_methodCalls[actorRemoteId].ContainsKey(hashedMethodName))
            {
                _methodCalls[actorRemoteId][hashedMethodName] = methodCall;
            }
            else
            {
                _methodCalls[actorRemoteId].Add(hashedMethodName, methodCall);
            }
        }

        internal virtual void HandleChatMessage(INetworkMessageIn msg)
        {
        }

        protected virtual bool ApproveConnection(string approvalMessage)
        {
            return true;
        }

        public virtual string GetApprovalString()
        {
            return string.Empty;
        }

        internal virtual void HandleConnectionApproval(INetworkMessageIn msg)
        {
            var approvalMessage = msg.ReadString();

            if (ApproveConnection(approvalMessage))
            {
                NetworkImplementation.ApproveMessage(msg);
            }
            else
            {
                NetworkImplementation.DisapproveMessage(msg);
            }
        }

        internal virtual void EndUpdate(bool hasConnections)
        {
        }

        public virtual void CreateActor(INetworkedActor actor)
        {
        }

        public virtual void DestroyActor(INetworkedActor actor)
        {
        }

        internal virtual void HandleActorDestruction(INetworkMessageIn msg)
        {
        }

        internal virtual void HandleStatusChangedConnected(INetworkMessageIn msg)
        {
        }

        internal virtual void HandleActorReplication(INetworkMessageIn msg)
        {
        }

        internal virtual void HandlePlayerCreation(INetworkMessageIn msg)
        {
        }

        internal virtual void HandleActorCreation(INetworkMessageIn msg)
        {
        }

        internal virtual void HandleActorRequestPlayerActor(INetworkMessageIn msg)
        {
        }

        internal virtual void HandleActorPropertiesReplication(INetworkMessageIn msg)
        {
        }

        internal virtual void HandleActorRemoteMethodCall(INetworkMessageIn msg)
        {
            int id = msg.ReadInt();
            int methodKey = msg.ReadInt();
            byte parameterCount = msg.ReadByte();

            var parameters = new object[parameterCount];
            for (byte i = 0; i < parameterCount; i++)
            {
                var type = (NetworkRemoteCallParameterType)msg.ReadByte();
                switch (type)
                {
                    case NetworkRemoteCallParameterType.Int:
                        parameters[i] = msg.ReadInt();
                        break;
                    case NetworkRemoteCallParameterType.Float:
                        parameters[i] = msg.ReadFloat();
                        break;
                    case NetworkRemoteCallParameterType.String:
                        parameters[i] = msg.ReadString();
                        break;
                    case NetworkRemoteCallParameterType.Boolean:
                        parameters[i] = msg.ReadBool();
                        break;
                }
            }

            try
            {
                if (_remoteMethods.ContainsKey(methodKey))
                {
                    var actor = Actors.FirstOrDefault(a => a.Id == id);
                    actor?.GetType().InvokeMember(_remoteMethods[methodKey], BindingFlags.InvokeMethod, null, actor, parameters);
                }
            }
            catch (Exception ex)
            {
                // todo: throw?
            }
        }

        internal virtual void HandleServerTravel(INetworkMessageIn msg)
        {
        }

        internal virtual void HandleDisconnected(INetworkMessageIn msg)
        {
        }

        internal virtual void HandleClientDisconnected(INetworkMessageIn msg)
        {
        }

        protected INetworkMessageOut CreateMessage(NetworkMessageType type)
        {
            return NetworkImplementation.CreateMessage(type);
        }

        protected void SendMessageToAll(INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel)
        {
            NetworkImplementation.SendMessageToAll(message, deliveryMethod, channel);
        }

        protected void SendMessageToSender(INetworkMessageIn to, INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel)
        {
            NetworkImplementation.SendMessageToSender(to, message, deliveryMethod, channel);
        }

        protected void SendMessageToId(long uniqueId, INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel)
        {
            NetworkImplementation.SendMessageToId(uniqueId, message, deliveryMethod, channel);
        }

        protected void SendMessageToIds(IEnumerable<long> uniqueIds, INetworkMessageOut message, MessageDeliveryMethod deliveryMethod, int channel)
        {
            NetworkImplementation.SendMessageToIds(uniqueIds, message, deliveryMethod, channel);
        }

        private class CallMethod
        {
            public readonly int ActorRemoteId;
            public readonly int MethodName;
            public readonly bool Reliable;
            public readonly object[] Parameters;

            public CallMethod(int actorRemoteId, int methodName, bool reliable, object[] parameters)
            {
                ActorRemoteId = actorRemoteId;
                MethodName = methodName;
                Reliable = reliable;
                Parameters = parameters;
            }
        }
    }
}
