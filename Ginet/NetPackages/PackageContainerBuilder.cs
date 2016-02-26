using Ginet.Logging;
using Ginet.Infrastructure;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Ginet.NetPackages
{

    public class PackageContainerBuilder
    {
        private readonly IAppender appender;
        private readonly Dictionary<string, Type> packages = new Dictionary<string, Type>();

        private IPackageSerializer DefaultSerializer => new DefaultSerializer();
        private string GetIdFromType(Type type) => type.FullName;

        internal PackageContainerBuilder()
        {
            appender = GinetOut.Appender[GetType().FullName];
            Register<Terminal.Command>();
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

        public PackageContainerBuilder Register<TPackage>()
            where TPackage : class
        {
            return Register(typeof(TPackage));
        }

        public PackageContainerBuilder Register(Type packageType)
        {
            if (packages.ContainsValue(packageType))
            {
                throw new Exception($"The package {packageType} was already registered");
            }
            packages.Add(GetIdFromType(packageType), packageType);
            return this;
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

        internal PackageContainer Build()
        {
            packages.OrderBy(p => p.Key);
            var packageRepo = new ConcurrentRepository<byte, PackageInfo>();
            var idMatcher = new ConcurrentRepository<string, byte>();

            byte packageId = 0;
            foreach (var package in packages)
            {
                var entry = new PackageInfo
                {
                    Type = package.Value,
                    Serializer = DefaultSerializer
                };

                ConfigureCustomAttribute<PackageSerializerAttribute>(package.Value, attr =>
                {
                    Contract.Requires(attr.SerializerType.IsAssignableFrom(typeof(IPackageSerializer)));
                    entry.Serializer = (IPackageSerializer)Activator.CreateInstance(attr.SerializerType);
                });
                packageRepo.Add(packageId, entry);
                idMatcher.Add(package.Key, packageId);
                packageId++;
            }
            return new PackageContainer(GetIdFromType, idMatcher, packageRepo);
        }


    }
}
