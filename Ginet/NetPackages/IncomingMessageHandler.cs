using Ginet.Logging;
using Ginet.Infrastructure;
using Lidgren.Network;
using System;
using System.Collections.Generic;
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

        private readonly PackageContainer packageContainer;
        private string SenderInfo(NetIncomingMessage im) =>
            $"{im.SenderEndPoint?.ToString()} - {im.SenderConnection?.Tag?.ToString()}";

        internal IncomingMessageHandler(PackageContainer packageContainer)
        {
            appender = GinetOut.Appender[GetType().FullName];
            this.packageContainer = packageContainer;

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
                var package = packageContainer.GetPackageInfoFromByte(im.ReadByte());
                if (package != null)
                {
                    var message = package.Serializer.Decode(im, package.Type);
                    if (package.Handler == null) return;
                    await package.Handler.Invoke(message, im);
                }
            });

        }

        public IDisposable OnPackage<TPackage>(Action<TPackage, NetIncomingMessage> handler)
            where TPackage : class
        {
            return RegisterPackageHandler<TPackage>((obj, im) =>
            {
                handler((TPackage)obj, im);
                return Task.FromResult(0);
            });
        }
        
        public IDisposable OnPackage<TPackage>(Func<TPackage, NetIncomingMessage, Task> handler)
            where TPackage : class
        {
            return RegisterPackageHandler<TPackage>((obj, im) => handler((TPackage)obj, im));
        }

        private IDisposable RegisterPackageHandler<TPackage>(Func<object, NetIncomingMessage, Task> packageHandler)
            where TPackage : class
        {
            var entry = packageContainer.GetPackageInfoFromType(typeof(TPackage));
            entry.Handler += packageHandler;
            return new DelegateDisposable(() => entry.Handler -= packageHandler);
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

        public IDisposable OnConnection(NetConnection connection, Func<NetIncomingMessageType, NetIncomingMessage, Task> handler)
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

        public IDisposable OnConnection(NetConnection connection, Action<NetIncomingMessageType, NetIncomingMessage> handler)
        {
            return connectionHandler.Add(connection, (msgType, im) =>
            {
                handler(msgType, im);
                return Task.FromResult(0);
            });
        }

        public void LogTraffic()
        {
            LogMessageType(new Dictionary<NetIncomingMessageType, Action<string>>
            {
                [NetIncomingMessageType.WarningMessage] = appender.Warn,
                [NetIncomingMessageType.ErrorMessage] = appender.Error,
                [NetIncomingMessageType.Error] = appender.Error,
                [NetIncomingMessageType.DebugMessage] = appender.Debug,
                [NetIncomingMessageType.VerboseDebugMessage] = appender.Debug
            });

            globalHandler.Add(connectionHandler.Count + 1,
                (msgType, msg) => Task.Run(() =>
                    appender.Info($"{msgType} - {SenderInfo(msg)}")));

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

        private void LogMessageType(IDictionary<NetIncomingMessageType, Action<string>> msgTypeAndLogger)
        {
            foreach (var typeAndLogger in msgTypeAndLogger)
            {
                messageHandlers.Add(
                    typeAndLogger.Key,
                    msg => Task.Run(() =>
                        typeAndLogger.Value($"{msg.ReadString()} - {SenderInfo(msg)}.")));
            }
        }

        private void LogConnectionChange(NetConnectionStatus[] netStatus)
        {
            foreach (var status in netStatus)
            {
                connectionChange.Add(status, msg => Task.Run(() =>
                appender.Info($"Status Changed: {status} - {SenderInfo(msg)}")));
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
                    if (im.SenderConnection != null)
                    {
                        if (connectionHandler.HasKey(im.SenderConnection))
                        {
                            await connectionHandler[im.SenderConnection].Invoke(im.MessageType, im);
                        }
                    }
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
