
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Voltyks.API.Extentions;
using Voltyks.Infrastructure.UnitOfWork;
using Voltyks.Persistence.Data;


namespace Voltyks.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.RegisterAllServices(builder.Configuration);

            var app = builder.Build();

            // Migrations are applied separately via CLI with admin connection:
            // dotnet ef database update --connection "Server=...;User ID=voltyksadmin;..."

            await app.ConfigurMiddleWares();

            app.Run();
        }
    }
}
