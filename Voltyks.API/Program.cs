
using StackExchange.Redis;
using Voltyks.API.Extentions;
using Voltyks.Infrastructure.UnitOfWork;


namespace Voltyks.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.RegisterAllServices(builder.Configuration);


            var connectionString = builder.Configuration.GetConnectionString("Redis");
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectionString);
            builder.Services.AddSingleton<IConnectionMultiplexer>(redis);


            var app = builder.Build();

            await app.ConfigurMiddleWares();

            app.Run();
        }
    }
}
