using Lidgren.Network;
using System;
using System.Threading.Tasks;

namespace Ginet.NetPackages
{
    internal class PackageInfo
    {
        public IPackageSerializer Serializer { get; set; }
        public Func<object,NetIncomingMessage, Task> Handler { get; set; }
        public Type Type { get; set; }
    }
}
