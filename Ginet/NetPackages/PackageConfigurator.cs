using Ginet.Logging;
using Ginet.Repositories;
using Lidgren.Network;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Ginet.NetPackages
{
    public class PackageConfigurator
    {
        private readonly IAppender appender;
        internal IPackageSerializer DefaultSerializer => new DefaultSerializer();
        internal ConcurrentRepository<string, PackageInfo> Packages { get; }
        internal PackageInfo this[string id] => Packages[id];

        internal PackageConfigurator()
        {
            Packages = new ConcurrentRepository<string, PackageInfo>();
            appender = GinetOut.Appender[GetType().FullName];
        }
        
        private PackageInfo RegisterOrRetrievePackage(Type packageType)
        {
            if (Packages.HasKey(packageType.FullName))
            {
                return Packages[packageType.FullName];
            }

            var entry = new PackageInfo
            {
                Type = packageType,
                Serializer = DefaultSerializer
            };

            ConfigureCustomAttribute<PackageSerializerAttribute>(packageType, attr =>
            {
                Contract.Requires(attr.SerializerType.IsAssignableFrom(typeof(IPackageSerializer)));
                entry.Serializer = (IPackageSerializer)Activator.CreateInstance(attr.SerializerType);
            });

            Packages.Add(packageType.FullName, entry);
            return entry;
        }

        private void ConfigureCustomAttribute<TAttribute>(Type type, Action<TAttribute> action)
            where TAttribute : Attribute
        {
            var attr = type.GetCustomAttributes(typeof(TAttribute), false).FirstOrDefault() as TAttribute;
            if (attr != null)
            {
                action(attr);
            }
        }
                
        public void Register<TPackage>()
            where TPackage : class
        {
            RegisterOrRetrievePackage(typeof(TPackage));
        }

        public void Register(Type packageType)
        {
            RegisterOrRetrievePackage(packageType);
        }

        public void RegisterPackages(Assembly assembly)
        {
            var packages = from type in assembly.GetTypes()
                           where Attribute.IsDefined(type, typeof(GinetPackageAttribute))
                           select type;

            foreach (var package in packages)
            {
                Register(package);
            }
        }

    }
}
