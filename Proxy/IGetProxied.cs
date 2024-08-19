namespace Proxy
{
    public interface IGetProxied<T> where T : new()
    {
        T GetProxiedInstance();
    }
}
