using Knapcode.TorSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace TorProxyManager.Common
{
    /// <summary>
    /// Class for handling individual TorSharp proxy
    /// </summary>
    public class TorSharpProxyHandler
    {
        public TorSharpSettings Settings { get; }
        private readonly TorSharpProxy proxy;

        public TorSharpProxyHandler(TorSharpSettings settings)
        {
            this.Settings = settings;
            proxy = new TorSharpProxy(this.Settings);
        }

        public async Task StartAsync()
        {
            await proxy.ConfigureAndStartAsync();
        }

        public void Stop()
        {
            proxy.Stop();
            proxy.Dispose();
        }

        public async Task GetNewIdentityAsync()
        {
            await proxy.GetNewIdentityAsync();
        }

        public async Task SetExitNodeCountryAsync(string countryCode)
        {
            using var client = new TcpClient("localhost", Settings.TorSettings.ControlPort);
            await using var stream = client.GetStream();
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream) { AutoFlush = true };

            // Authenticate
            await writer.WriteLineAsync($"AUTHENTICATE \"{Settings.TorSettings.ControlPassword}\"");

            // Set exit nodes
            await writer.WriteLineAsync($"SETCONF ExitNodes={{{countryCode}}}");
        }

        public async Task<string> GetConnectionInfoAsync()
        {
            using var client = new TcpClient("localhost", Settings.TorSettings.ControlPort);
            await using var stream = client.GetStream();
            var reader = new StreamReader(stream);
            var writer = new StreamWriter(stream) { AutoFlush = true };

            // Authenticate
            await writer.WriteLineAsync($"AUTHENTICATE \"{Settings.TorSettings.ControlPassword}\"");

            // Get connection info
            await writer.WriteLineAsync("GETINFO circuit-status");

            var response = new StringBuilder();
            string line;
            do
            {
                line = await reader.ReadLineAsync();
                response.AppendLine(line);
            } while (!string.IsNullOrEmpty(line));

            return response.ToString();
        }
    }
}
