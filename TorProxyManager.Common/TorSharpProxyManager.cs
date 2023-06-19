using Knapcode.TorSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace TorProxyManager.Common
{
    public class TorSharpProxyManager
    {
        private readonly string _baseDirectory;
        public Dictionary<Guid, TorSharpProxyHandler> ProxyHandlers { get; set; }

        public TorSharpSettings Settings { get; set; }

        public TorSharpProxyManager(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            ProxyHandlers = new Dictionary<Guid, TorSharpProxyHandler>();
        }

        /// <summary>
        /// Creates a new proxy
        /// </summary>
        /// <returns>The GUID identifier of the new proxy</returns>
        public async Task<Guid> CreateNewProxyAsync()
        {
            this.Settings = new TorSharpSettings
            {
                PrivoxySettings = { Disable = true }
            };

            this.Settings.WriteToConsole = true; // todo: disable debug output

            // download Tor
            using (var httpClient = new HttpClient())
            {
                var fetcher = new TorSharpToolFetcher(this.Settings, httpClient);
                await fetcher.FetchAsync();
            }

            var proxyHandler = new TorSharpProxyHandler(this.Settings);
            await proxyHandler.StartAsync();

            var id = Guid.NewGuid();
            ProxyHandlers[id] = proxyHandler;

            return id;
        }

        /// <summary>
        /// Stops and removes a proxy
        /// </summary>
        /// <param name="id">The GUID identifier of the proxy to be stopped and removed</param>
        /// <returns>Boolean indicating success or failure</returns>
        public bool StopAndRemoveProxy(Guid id)
        {
            if (ProxyHandlers.TryGetValue(id, out var proxyHandler))
            {
                proxyHandler.Stop();
                ProxyHandlers.Remove(id);
                return true;
            }

            return false;
        }

        public TorSharpProxyHandler GetProxyHandler(Guid proxyId)
        {
            return this.ProxyHandlers[proxyId];
        }
    }
}
