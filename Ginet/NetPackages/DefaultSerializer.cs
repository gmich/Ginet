using Ginet.Logging;
using Lidgren.Network;
using System;

namespace Ginet.NetPackages
{

    internal class DefaultSerializer : IPackageSerializer
    {
        private readonly IAppender appender;

        internal DefaultSerializer()
        {
            appender = GinetOut.Appender[GetType().FullName];
        }

        public object Decode(NetIncomingMessage im, Type decodedType)
        {
            var obj = Activator.CreateInstance(decodedType);
            im.ReadAllProperties(obj);

            return obj;
        }

        public void Encode<T>(T objectToEncode, NetOutgoingMessage om)
                  where T : class
        {
            om.WriteAllProperties(objectToEncode);
        }
    }
}
