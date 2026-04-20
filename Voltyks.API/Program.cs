
using System.Globalization;
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
            // Default the app's culture to Egypt — affects DateTime formatting,
            // number formatting, and anything that calls DateTime.Now.
            var egyptCulture = new CultureInfo("ar-EG");
            CultureInfo.DefaultThreadCurrentCulture = egyptCulture;
            CultureInfo.DefaultThreadCurrentUICulture = egyptCulture;

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
