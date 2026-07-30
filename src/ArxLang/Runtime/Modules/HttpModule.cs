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
        registry["httpPost"] = HttpPost;
        registry["httpRequest"] = HttpRequest;
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

    // httpServer(port, handler) — handler(path, method) повертає рядок
    // тіла відповіді. Раніше створювала HttpListener і одразу повертала
    // його в ArxLang-код, але не було жодного способу прийняти запит чи
    // відповісти — кожен клієнт просто зависав би назавжди. Тепер блокує
    // й сама веде цикл прийому запитів, викликаючи ArxLang-колбек на
    // кожен запит — той самий підхід, що й guiOnAction для кліків.
    private static object? CreateServer(object[] args)
    {
        int port = Convert.ToInt32(args[0]);
        var handlerRef = (ArxFunctionRef)args[1];
        var vm = VirtualMachine.Current!;

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        Console.WriteLine($"[ArxNode] Сервер запущено на порту {port}");

        while (listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = listener.GetContext();
            }
            catch (HttpListenerException)
            {
                break;
            }

            string path = context.Request.Url?.AbsolutePath ?? "/";
            string method = context.Request.HttpMethod;

            string body;
            try
            {
                var result = vm.InvokeFunctionValue(handlerRef, new object[] { path, method });
                body = result?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                body = "Internal Server Error: " + ex.Message;
            }

            var buffer = Encoding.UTF8.GetBytes(body);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();
        }

        return null;
    }

    private static object? HttpGet(object[] args)
    {
        string url = args[0].ToString()!;
        using var client = new HttpClient();
        return client.GetStringAsync(url).GetAwaiter().GetResult();
    }

    // httpPost(url, body) — тіло POST-запиту як рядок, за замовчуванням
    // application/json (найчастіший випадок - надіслати JSON у REST API/
    // вебхук). Повертає лише тіло відповіді, як httpGet, для найпростішого
    // "надіслати й прочитати" сценарію без потреби перевіряти статус-код.
    private static object? HttpPost(object[] args)
    {
        string url = args[0].ToString()!;
        string body = args.Length > 1 ? args[1]?.ToString() ?? "" : "";
        using var client = new HttpClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = client.PostAsync(url, content).GetAwaiter().GetResult();
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    // httpRequest(url, method, body?) — довільний HTTP-метод (PUT/DELETE/
    // PATCH тощо), на відміну від httpGet/httpPost, повертає МАПУ
    // {status, body}, бо для довільних методів код відповіді зазвичай
    // важливіший за саме тіло (204 No Content, 404 тощо).
    private static object? HttpRequest(object[] args)
    {
        string url = args[0].ToString()!;
        string method = args.Length > 1 ? args[1]?.ToString()?.ToUpperInvariant() ?? "GET" : "GET";
        string body = args.Length > 2 ? args[2]?.ToString() ?? "" : "";

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (!string.IsNullOrEmpty(body))
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = client.SendAsync(request).GetAwaiter().GetResult();
        string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        var map = new ArxMap();
        map.Entries["status"] = (double)(int)response.StatusCode;
        map.Entries["body"] = responseBody;
        return map;
    }
}
