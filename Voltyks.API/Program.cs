
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

            var app = builder.Build();

            await app.ConfigurMiddleWares();

            app.Run();
        }
    }
}
