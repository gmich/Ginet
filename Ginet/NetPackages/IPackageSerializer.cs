using Lidgren.Network;
using System;

namespace Ginet.NetPackages
{
    public interface IPackageSerializer
    {
        object Decode(NetIncomingMessage im, Type packageType);

        void Encode<T>(T objectToEncode, NetOutgoingMessage om)
             where T : class;
    }
}
