using System;

namespace Ginet.NetPackages
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PackageSerializerAttribute : Attribute
    {
        public Type SerializerType { get; }

        public PackageSerializerAttribute(Type serializerType)
        {
            SerializerType = serializerType;
        }
    }
}
