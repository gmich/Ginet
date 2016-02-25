using Ginet.Infrastructure;
using System;

namespace Ginet.NetPackages
{
    internal class PackageContainer
    {
        private readonly ConcurrentRepository<byte, PackageInfo> packages;
        private readonly ConcurrentRepository<string, byte> idMatcher;
        private readonly Func<Type, string> IdRetriever;

        internal PackageInfo GetPackageInfoFromType(Type type)
        {
            return packages[idMatcher[IdRetriever(type)]];
        }

        internal byte GetIdFromType(Type type)
        {
            return idMatcher[IdRetriever(type)];
        }

        internal PackageInfo GetPackageInfoFromByte(byte id)
        {
            return packages[id];
        }

        internal PackageContainer(Func<Type, string> idRetriever, ConcurrentRepository<string, byte> idMatcher, ConcurrentRepository<byte, PackageInfo> packages)
        {
            IdRetriever = idRetriever;
            this.packages = packages;
            this.idMatcher = idMatcher;
        }
    }
}
