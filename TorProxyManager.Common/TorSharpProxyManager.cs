using Knapcode.TorSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TorProxyManager.Common
{
    public class TorSharpProxyManager
    {
        private string BaseDirectory { get; set; }

        private int StartingPort = 3500;

        private TorSharpSettings BaseSettings = new();

        public Dictionary<Guid, TorSharpProxyHandler> ProxyHandlers { get; set; }

        public TorSharpSettings Settings { get; set; }

        public TorSharpProxyManager(string baseDirectory)
        {
            this.BaseDirectory = baseDirectory;
            this.ProxyHandlers = new Dictionary<Guid, TorSharpProxyHandler>();

            this.BaseSettings.ZippedToolsDirectory = Path.Combine(this.BaseDirectory, "zipped");
            this.BaseSettings.ExtractedToolsDirectory = Path.Combine(this.BaseDirectory, "extracted");

            // download Tor
            using (var httpClient = new HttpClient())
            {
                var fetcher = new TorSharpToolFetcher(this.BaseSettings, httpClient);
                fetcher.FetchAsync().Wait();
            }
        }

        ~TorSharpProxyManager()
        {
            foreach (var proxyHandler in this.ProxyHandlers.Values)
            {
                proxyHandler.Stop();
            }
            
            // Remove extracted tools directory
            foreach(var id in this.ProxyHandlers.Keys)
            {
                try
                {
                    Directory.Delete(Path.Combine(this.BaseSettings.ExtractedToolsDirectory, $"tor_{id}"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Creates a new proxy
        /// </summary>
        /// <returns>The GUID identifier of the new proxy</returns>
        public async Task<Guid> CreateNewProxyAsync()
        {
            var id = Guid.NewGuid();

            this.Settings = new TorSharpSettings
            {
                PrivoxySettings = { Disable = true },
                TorSettings = { ControlPassword = "foobar", SocksPort = this.StartingPort, ControlPort = this.StartingPort + 1 },
                
                // The extracted tools directory must not be shared.
                ExtractedToolsDirectory = Path.Combine(this.BaseSettings.ExtractedToolsDirectory, $"tor_{id}"),

                // The zipped tools directory can be shared, as long as the tool fetcher does not run in parallel.
                ZippedToolsDirectory = this.BaseSettings.ZippedToolsDirectory,
            };

            // Increment port numbers for next proxy
            this.StartingPort += 2;

            this.Settings.WriteToConsole = true; // todo: disable debug output

            // Start proxy server
            var proxyHandler = new TorSharpProxyHandler(this.Settings);
            await proxyHandler.StartAsync();

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
