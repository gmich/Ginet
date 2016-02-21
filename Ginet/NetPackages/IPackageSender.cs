namespace Ginet.NetPackages
{
    public interface IPackageSender
    {
        void Send<TPackage>(TPackage message)
            where TPackage : class;
    }
}
