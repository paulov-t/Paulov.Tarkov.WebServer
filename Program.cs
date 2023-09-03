
using Newtonsoft.Json;
using SIT.WebServer.Providers;

namespace SIT.WebServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var pathToHttpConfig = Path.Combine(AppContext.BaseDirectory, "assets", "configs", "http.json");
            var httpConfigSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(pathToHttpConfig));


            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ConfigureEndpointDefaults(listenOptions =>
                {
                    //listenOptions.IPEndPoint.Address = new System.Net.IPAddress()
                    listenOptions.IPEndPoint.Port = int.Parse(httpConfigSettings["port"].ToString());
                });
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

            var app = builder.Build();
            //app.UseRequestDecompression();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            //app.UseSession(new SessionOptions() { IdleTimeout = new TimeSpan(1,1,1,1) });

            app.MapControllers();

            app.Run();
        }
    }
}