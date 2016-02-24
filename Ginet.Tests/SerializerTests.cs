using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ginet.NetPackages;
using Lidgren.Network;

namespace Ginet.Tests
{
    [TestClass]
    public class SerializerTests
    {
        public class TestSerializer : IPackageSerializer
        {
            public object Decode(NetIncomingMessage im, Type decodedType)
            {
                return new TestPackage { Message = im.ReadString() };
            }

            public void Encode<T>(T objectToEncode, NetOutgoingMessage om) where T : class
            {
                om.Write("Success");
                return;
            }
        }

        [GinetPackage]
        [PackageSerializer(typeof(TestSerializer))]
        public class TestPackage
        {
            public string Message { get; set; }
        }

        [TestMethod]
        public void CustomSerializer_Resolves_Successfuly()
        {
            var server = new NetworkServer("Test", cfg => cfg.Port = 1234);

            server.Start(NetDeliveryMethod.ReliableOrdered, 0);
            server.PackageConfigurator.Register<TestPackage>();
            server.Send(new TestPackage { Message = "test" }, (om, svr) =>
            {
                svr.SendToAll(om, server.DeliveryMethod);
            });
        }
    }
}
