namespace Proxy
{
    public interface IGetProxied<T> where T : new()
    {
        /// <summary>
        /// Returns the proxied object instance.
        /// </summary>
        /// <returns></returns>
        T GetProxied();
    }
}
