using System;
using Lidgren.Network;
using Ginet.Logging;
using System.Net;
using Ginet.NetPackages;

namespace Ginet
{
    public class NetworkClient : NetworkManager<NetClient>
    {
        public NetworkClient(string name, Action<GinetConfig> configuration, Action<PackageContainerBuilder> containerBuilder) 
            : base(name, configuration, containerBuilder)
        {
        }

        public void Connect<TConnectionApprovalMsg>(string ipOrHost, int port, TConnectionApprovalMsg msg)
            where TConnectionApprovalMsg : class
        {
            StartHost();
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
