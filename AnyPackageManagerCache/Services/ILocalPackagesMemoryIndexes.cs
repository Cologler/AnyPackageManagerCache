namespace AnyPackageManagerCache.Services
{
    public interface ILocalPackagesMemoryIndexes
    {
        void Add(string packageName);

        void Remove(string packageName);

        bool Contains(string packageName);
    }
}
