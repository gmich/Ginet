using System;
using Lidgren.Network;
using Ginet.Logging;
using System.Net;

namespace Ginet
{
    public class NetworkClient : NetworkManager<NetClient>
    {
        public NetworkClient(string serverName, Action<NetPeerConfiguration> configuration, IAppender output = null, bool enableAllIncomingMessages = true)
            : base(serverName, configuration, output, enableAllIncomingMessages)
        { }

        public void Connect<TConnectionApprovalMsg>(string ipOrHost, int port, TConnectionApprovalMsg msg)
            where TConnectionApprovalMsg : class
        {
            try
            {
                Host.Connect(new IPEndPoint(NetUtility.Resolve(ipOrHost), port), ConvertToOutgoingMessage(msg));
            }
            catch (Exception ex)
            {
                Out.Error($"Unable to connect to {ipOrHost}:{port}. {ex.Message}");
                throw ex;
            }
        }
    }

}
