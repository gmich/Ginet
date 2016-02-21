using Ginet.Logging;
using Ginet.Repositories;
using Lidgren.Network;
using System;
using System.Threading.Tasks;

namespace Ginet.NetPackages
{
    public class IncomingMessageHandler
    {
        private readonly IAppender appender;

        private readonly ConcurrentRepository<NetIncomingMessageType, Func<NetIncomingMessage, Task>> messageHandlers =
             new ConcurrentRepository<NetIncomingMessageType, Func<NetIncomingMessage, Task>>();

        private readonly ConcurrentRepository<NetConnectionStatus, Func<NetIncomingMessage, Task>> connectionChange =
            new ConcurrentRepository<NetConnectionStatus, Func<NetIncomingMessage, Task>>();

        private readonly ConcurrentRepository<long, Func<NetIncomingMessageType, NetIncomingMessage, Task>> globalHandler =
            new ConcurrentRepository<long, Func<NetIncomingMessageType, NetIncomingMessage, Task>>();

        private readonly ConcurrentRepository<NetConnection, Func<NetIncomingMessageType, NetIncomingMessage, Task>> connectionHandler =
            new ConcurrentRepository<NetConnection, Func<NetIncomingMessageType, NetIncomingMessage, Task>>();

        private readonly Func<string, PackageInfo> packageRetriever;

        internal IncomingMessageHandler(Func<string, PackageInfo> packageRetriever)
        {
            appender = GinetOut.Appender[GetType().FullName];
            this.packageRetriever = packageRetriever;

            messageHandlers.Add(NetIncomingMessageType.StatusChanged, async msg =>
             {
                 var key = (NetConnectionStatus)msg.ReadByte();
                 if (connectionChange.HasKey(key))
                 {
                     await connectionChange[key](msg);
                 }
             });

            messageHandlers.Add(NetIncomingMessageType.Data, async im =>
            {
                var package = packageRetriever(im.ReadString());
                if (package != null)
                {
                    var message = package.Serializer.Decode(im, package.Type);
                    if (package.Handler == null) return;
                    await package.Handler.Invoke(message, im);
                }
            });

            Log(new[] {
                NetIncomingMessageType.WarningMessage,
                NetIncomingMessageType.ErrorMessage,
                NetIncomingMessageType.Error,
                NetIncomingMessageType.DebugMessage,
                NetIncomingMessageType.VerboseDebugMessage
            });
        }

        private void Log(NetIncomingMessageType[] msgTypes)
        {
            foreach (var msgType in msgTypes)
            {
                messageHandlers.Add(
                    msgType,
                    msg => Task.Run(() =>
                        appender.Debug(msg.ReadString())));
            }
        }

        public void OnPackage<TPackage>(Action<TPackage, NetIncomingMessage> handler)
            where TPackage : class
        {
            var entry = packageRetriever(typeof(TPackage).FullName);
            entry.Handler = (obj, im) =>
            {
                handler((TPackage)obj, im);
                return Task.FromResult(0);
            };
        }

        public void Handle<TPackage>(Func<TPackage, NetIncomingMessage, Task> handler)
            where TPackage : class
        {
            var entry = packageRetriever(typeof(TPackage).FullName);
            entry.Handler = (obj, im) => handler((TPackage)obj, im);
        }

        public IDisposable OnMessage(NetIncomingMessageType type, Func<NetIncomingMessage, Task> handler)
        {
            return messageHandlers.Add(type, handler);
        }

        public IDisposable OnConnectionChange(NetConnectionStatus type, Func<NetIncomingMessage, Task> handler)
        {
            return connectionChange.Add(type, handler);
        }

        public IDisposable OnAllMessages(Func<NetIncomingMessageType, NetIncomingMessage, Task> handler)
        {
            return globalHandler.Add(connectionHandler.Count + 1, handler);
        }

        public IDisposable OnSpecificConnection(NetConnection connection, Func<NetIncomingMessageType, NetIncomingMessage, Task> handler)
        {
            return connectionHandler.Add(connection, handler);
        }

        public IDisposable OnMessage(NetIncomingMessageType type, Action<NetIncomingMessage> handler)
        {
            return messageHandlers.Add(type, im =>
            {
                handler(im);
                return Task.FromResult(0);
            });
        }

        public IDisposable OnConnectionChange(NetConnectionStatus type, Action<NetIncomingMessage> handler)
        {
            return connectionChange.Add(type, im =>
            {
                handler(im);
                return Task.FromResult(0);
            });
        }

        public IDisposable OnAllMessages(Action<NetIncomingMessageType, NetIncomingMessage> handler)
        {
            return globalHandler.Add(connectionHandler.Count + 1, (msgType, im) =>
            {
                handler(msgType, im);
                return Task.FromResult(0);
            });
        }

        public IDisposable OnSpecificConnection(NetConnection connection, Action<NetIncomingMessageType, NetIncomingMessage> handler)
        {
            return connectionHandler.Add(connection, (msgType, im) =>
            {
                handler(msgType, im);
                return Task.FromResult(0);
            });
        }

        public void LogTraffic()
        {
            globalHandler.Add(connectionHandler.Count + 1,
                (msgType, msg) => Task.Run(() =>
                    appender.Info($"{msgType} from {msg.SenderConnection}")));

            LogConnectionChange(new[]
            {
               NetConnectionStatus.Connected,
               NetConnectionStatus.Disconnected,
               NetConnectionStatus.Disconnecting,
               NetConnectionStatus.InitiatedConnect,
               NetConnectionStatus.ReceivedInitiation,
               NetConnectionStatus.None,
               NetConnectionStatus.RespondedAwaitingApproval,
               NetConnectionStatus.RespondedConnect,
            });
        }

        private void LogConnectionChange(NetConnectionStatus[] netStatus)
        {
            foreach (var status in netStatus)
            {
                connectionChange.Add(status, msg => Task.Run(() =>
                appender.Info($"Status Changed: {status} from {msg.SenderConnection}")));
            }
        }

        internal Task ProcessNetworkMessages(NetPeer peer)
        {
            return Task.Run(async () =>
            {
                NetIncomingMessage im;
                peer.MessageReceivedEvent.WaitOne();

                while ((im = peer.ReadMessage()) != null)
                {
                    foreach (var handler in globalHandler.GetAll)
                    {
                        await handler(im.MessageType, im);
                    }
                    if (messageHandlers.HasKey(im.MessageType))
                    {
                        await messageHandlers[im.MessageType].Invoke(im);
                    }
                    peer.Recycle(im);
                }
            });
        }


    }
}
