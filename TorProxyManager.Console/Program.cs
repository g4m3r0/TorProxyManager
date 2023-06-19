using TorProxyManager.Common;

namespace TorProxyManager.Console
{
    using Knapcode.TorSharp;
    using System;
    using System.Net;

    public class Program
    {
        static async Task Main(string[] args)
        {
            //var settings = new TorSharpSettings
            //{
            //    PrivoxySettings = { Disable = true }
            //};

            //// download Tor
            //using (var httpClient = new HttpClient())
            //{
            //    var fetcher = new TorSharpToolFetcher(settings, httpClient);
            //    await fetcher.FetchAsync();
            //}

            //// execute
            //using (var proxy = new TorSharpProxy(settings))
            //{
            //    await proxy.ConfigureAndStartAsync();

            //    var handler = new HttpClientHandler
            //    {
            //        Proxy = new WebProxy(new Uri("socks5://localhost:" + settings.TorSettings.SocksPort))
            //    };

            //    using (handler)
            //    using (var httpClient = new HttpClient(handler))
            //    {
            //        var result = await httpClient.GetStringAsync("https://check.torproject.org/api/ip");

            //        Console.WriteLine();
            //        Console.WriteLine("Are we using Tor?");
            //        Console.WriteLine(result);
            //    }

            //    proxy.Stop();
            //}

            var torSharpProxyManager = new TorSharpProxyManager(@"C:\test");

            // Create new proxies with different port settings
            Guid proxy1 = await torSharpProxyManager.CreateNewProxyAsync();
            //Guid proxy2 = await torSharpProxyManager.CreateNewProxyAsync(2337, 2338, 2339);

            // Test the proxies
            await TestProxy(torSharpProxyManager, proxy1);
            //await TestProxy(torSharpProxyManager, proxy2);

            // Stop and remove the proxies
            torSharpProxyManager.StopAndRemoveProxy(proxy1);
            //torSharpProxyManager.StopAndRemoveProxy(proxy2);

            Console.ReadLine();
        }

        private static async Task TestProxy(TorSharpProxyManager manager, Guid proxyId)
        {
            try
            {
                // Get the proxy handler
                var handler = manager.GetProxyHandler(proxyId);
                await Console.Out.WriteLineAsync("Starting first proxy request...");
                await TestProxyServer(handler);

                // Test getting new identity
                await Console.Out.WriteLineAsync("Getting new identity...");
                await handler.GetNewIdentityAsync();
                await TestProxyServer(handler);

                // Test setting exit node country
                await Console.Out.WriteLineAsync("Changing exit node country...");
                await handler.SetExitNodeCountryAsync("us");
                await TestProxyServer(handler);

                await PrintConnectionInfoAsync(handler, proxyId.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to test proxy {proxyId}: {ex.Message}");
            }
        }

        private static async Task TestProxyServer(TorSharpProxyHandler handler)
        {
            var client = new HttpClientHandler
            {
                Proxy = new WebProxy(new Uri("socks5://localhost:" + handler.Settings.TorSettings.SocksPort))
            };

            using (client)
            using (var httpClient = new HttpClient(client))
            {
                var result = await httpClient.GetStringAsync("https://check.torproject.org/api/ip");
                Console.WriteLine(result);
            }
        }

        private static async Task PrintConnectionInfoAsync(TorSharpProxyHandler handler, string proxyId)
        {
            // Test getting connection info
            string info = await handler.GetConnectionInfoAsync();
            Console.WriteLine($"Connection info for proxy {proxyId}: {info}");
        }
    }
}