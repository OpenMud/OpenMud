using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace OpenMud.Debug.Core;

public static class ServerApplication
{
    public static WebApplication Create(string[] args, IDebugRuntimeService runtimService)
    {

        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        // Add services to the container
        builder.Services.AddSingleton<IDebugRuntimeService>(runtimService);

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSignalR()
            .AddJsonProtocol(options => { options.PayloadSerializerOptions.IncludeFields = true; });

        /*
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                builder =>
                {
                    builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .AllowAnyMethod()
                        .WithOrigins("http://localhost:1234");
                });
        })
        */;

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<DebugHub>("/debugHub");
        app.UseRouting();
/*        app.UseCors(builder =>
        {
            builder.WithOrigins("http://localhost:1234")
                .AllowAnyHeader()
                .WithMethods("GET", "POST")
                .AllowCredentials();
        });
*/
        return app;
    }
}