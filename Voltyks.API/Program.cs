
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

            // Auto-apply pending migrations
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<VoltyksDbContext>();
                db.Database.Migrate();
            }

            await app.ConfigurMiddleWares();

            app.Run();
        }
    }
}
