
using Newtonsoft.Json;
using SIT.WebServer.Middleware;
using SIT.WebServer.Providers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace SIT.WebServer
{
    public class Program
    {
        public static string publicIp { get; set; } = new HttpClient().GetStringAsync("https://api.ipify.org").Result;

        public static void Main(string[] args)
        {
            var pathToHttpConfig = Path.Combine(AppContext.BaseDirectory, "assets", "configs", "http.json");
            var httpConfigSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(pathToHttpConfig));

            var dnsHostname = Dns.GetHostName();

            var desiredIPAddress = httpConfigSettings["ipInternal"].ToString();
            var ipAddress = GetIpAddress(dnsHostname, desiredIPAddress);
            var desiredPort = httpConfigSettings["port"].ToString();

            var desiredIPExternalAddress = httpConfigSettings["ipExternal"];
            if(desiredIPExternalAddress != null)
                publicIp = desiredIPExternalAddress.ToString();
            
            Debug.WriteLine("Your public IP is: " + publicIp);

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Listen(ipAddress, int.Parse(desiredPort));
                //serverOptions.ConfigureEndpointDefaults(listenOptions =>
                //{
                //    listenOptions.IPEndPoint.Create()
                //    listenOptions.IPEndPoint.Address = ipAddress;
                //    listenOptions.IPEndPoint.Port = int.Parse(desiredPort);
                //});
            });


            builder.Services.AddRequestDecompression(options =>
            {
                options.DecompressionProviders.Add("zlibdecompressionprovider", new ZLibDecompressionProvider());
            });


            // Add services to the container.
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            //builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();


            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddMvc().AddSessionStateTempDataProvider();
            builder.Services.AddSession();

            var app = builder.Build();

            //app.UseRequestDecompression();
            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseWebSockets(new WebSocketOptions() { KeepAliveInterval = new TimeSpan(0, 0, 1) });
            //app.UseMiddleware<RequestZlibMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.ToString().StartsWith("/notifierServer/getwebsocket"))
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        if(webSocket == null)
                        {
                            await next(context);
                            return;
                        }

                        if(webSocket.State != WebSocketState.Open)
                        {
                            await next(context);
                            return;
                        }

                        try
                        {
                            await webSocket.SendAsync(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("")), System.Net.WebSockets.WebSocketMessageType.Binary, false, CancellationToken.None);
                            await Echo(webSocket);
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                }
                else
                {
                    await next(context);
                }
            });

            app.UseAuthorization();
            app.UseSession(new SessionOptions() { IdleTimeout = new TimeSpan(1, 1, 1, 1) });

            app.MapControllers();

            app.Run();

            SaveProvider saveProvider = new SaveProvider(); 
        }

        public class RequestLoggingMiddleware
        {
            private readonly RequestDelegate _next;

            public RequestLoggingMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            public async Task InvokeAsync(HttpContext context)
            {
                var startTime = DateTime.Now;

                await _next(context);

                var endTime = DateTime.Now;
                var elapsedTime = endTime - startTime;

                var logMessage = $"{context.Request.Method} {context.Request.Path} {context.Response.StatusCode} {elapsedTime.TotalMilliseconds}ms";
                Console.WriteLine(logMessage);
                Debug.WriteLine(logMessage);
            }
        }

        //public class RequestZlibMiddleware
        //{
        //    private readonly RequestDelegate _next;

        //    public RequestZlibMiddleware(RequestDelegate next)
        //    {
        //        _next = next;
        //    }

        //    public async Task InvokeAsync(HttpContext context)
        //    {
        //        var startTime = DateTime.Now;

        //        //var requestBody = await HttpBodyConverters.DecompressRequestBodyToBytes(context.Request);
        //        //context.Request.Body = new MemoryStream(requestBody);

        //        await _next(context);
        //    }
        //}

        private static async Task Echo(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            var receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                    receiveResult.MessageType,
                    receiveResult.EndOfMessage,
                    CancellationToken.None);

                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);
        }

        /**
      * The IPAddresses method obtains the selected server IP address information.
      * It then displays the type of address family supported by the server and its
      * IP address in standard and byte format.
      **/
        private static IPAddress GetIpAddress(string server, string ipAddress)
        {
            try
            {
                System.Text.ASCIIEncoding ASCII = new System.Text.ASCIIEncoding();

                // Get server related information.
                IPHostEntry heserver = Dns.GetHostEntry(server);

                // Loop on the AddressList
                foreach (IPAddress curAdd in heserver.AddressList)
                {


                    // Display the type of address family supported by the server. If the
                    // server is IPv6-enabled this value is: InterNetworkV6. If the server
                    // is also IPv4-enabled there will be an additional value of InterNetwork.
                    Debug.WriteLine("AddressFamily: " + curAdd.AddressFamily.ToString());

                    // Display the ScopeId property in case of IPV6 addresses.
                    if (curAdd.AddressFamily.ToString() == ProtocolFamily.InterNetworkV6.ToString())
                        Debug.WriteLine("Scope Id: " + curAdd.ScopeId.ToString());

                    // Display the server IP address in the standard format. In
                    // IPv4 the format will be dotted-quad notation, in IPv6 it will be
                    // in in colon-hexadecimal notation.
                    Debug.WriteLine("Address: " + curAdd.ToString());

                    if (curAdd.ToString() == ipAddress)
                    {
                        return curAdd;
                    }

                    // Display the server IP address in byte format.
                    Console.Write("AddressBytes: ");

                    Byte[] bytes = curAdd.GetAddressBytes();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        Console.Write(bytes[i]);
                    }

                    Debug.WriteLine("\r\n");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("[DoResolve] Exception: " + e.ToString());
            }

            return null;
        }

        // This IPAddressAdditionalInfo displays additional server address information.
        private static void IPAddressAdditionalInfo()
        {
            try
            {
                // Display the flags that show if the server supports IPv4 or IPv6
                // address schemas.
                Debug.WriteLine("\r\nSupportsIPv4: " + Socket.OSSupportsIPv4);
                Debug.WriteLine("SupportsIPv6: " + Socket.OSSupportsIPv6);

                if (Socket.OSSupportsIPv6)
                {
                    // Display the server Any address. This IP address indicates that the server
                    // should listen for client activity on all network interfaces.
                    Debug.WriteLine("\r\nIPv6Any: " + IPAddress.IPv6Any.ToString());

                    // Display the server loopback address.
                    Debug.WriteLine("IPv6Loopback: " + IPAddress.IPv6Loopback.ToString());

                    // Used during autoconfiguration first phase.
                    Debug.WriteLine("IPv6None: " + IPAddress.IPv6None.ToString());

                    Debug.WriteLine("IsLoopback(IPv6Loopback): " + IPAddress.IsLoopback(IPAddress.IPv6Loopback));
                }
                Debug.WriteLine("IsLoopback(Loopback): " + IPAddress.IsLoopback(IPAddress.Loopback));
            }
            catch (Exception e)
            {
                Debug.WriteLine("[IPAddresses] Exception: " + e.ToString());
            }
        }
    }
}