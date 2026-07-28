using System.Net;
using System.Text;
using ArxLang.VM;

namespace ArxLang.Runtime.Modules;

public static class HttpModule
{
    public static void Register(Dictionary<string, Func<object[], object?>> registry)
    {
        registry["httpServer"] = CreateServer;
        registry["httpGet"] = HttpGet;
        registry["urlStatus"] = UrlStatus;
    }

    private static object? UrlStatus(object[] args)
    {
        try
        {
            string url = args[0].ToString()!;
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = client.GetAsync(url).GetAwaiter().GetResult();
            return (double)(int)response.StatusCode;
        }
        catch
        {
            return -1.0;
        }
    }

    private static object? CreateServer(object[] args)
    {
        int port = Convert.ToInt32(args[0]);
        // Since we don't have true async callbacks in the VM yet, 
        // we'll return a server object that can be "polled" or runs a simple loop
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Console.WriteLine($"[ArxNode] Server started on port {port}");
        return listener;
    }

    private static object? HttpGet(object[] args)
    {
        string url = args[0].ToString()!;
        using var client = new HttpClient();
        return client.GetStringAsync(url).GetAwaiter().GetResult();
    }
}
