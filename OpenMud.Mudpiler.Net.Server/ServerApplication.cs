using System.Collections.Immutable;
using DefaultEcs;
using Microsoft.Extensions.FileProviders;
using OpenMud.Mudpiler.Core.Components;
using OpenMud.Mudpiler.Core.RuntimeTypes;
using OpenMud.Mudpiler.Framework;
using OpenMud.Mudpiler.Net.Core;
using OpenMud.Mudpiler.Net.Core.Components;
using OpenMud.Mudpiler.Net.Core.Encoding;
using OpenMud.Mudpiler.Net.Core.Encoding.Components;
using OpenMud.Mudpiler.Net.Core.Hubs;

namespace OpenMud.Mudpiler.Net.Server;

public static class ServerApplication
{
    public static WebApplication Create(string workingDir, string[] args, string? staticAssets = null)
    {

        var builder = WebApplication.CreateBuilder(args);
        // Add services to the container

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSignalR()
            .AddJsonProtocol(options => { options.PayloadSerializerOptions.IncludeFields = true; });

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
        });

        InitGameBasics(builder, workingDir);
        InitGameWorldStateEncoders(builder);
        InitGameNetworkServices(builder);

        builder.Services.AddTransient<IImmutableList<ServiceDescriptor>>(s => builder.Services.ToImmutableList());

        builder.Services.AddHostedService<GameService>();

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<WorldHub>("/worldHub");
        app.UseRouting();
        app.UseCors(builder =>
        {
            builder.WithOrigins("http://localhost:1234")
                .AllowAnyHeader()
                .WithMethods("GET", "POST")
                .AllowCredentials();
        });

        if (staticAssets != null)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    context.Response.Redirect("/index.html");
                    await context.Response.CompleteAsync();
                });
            });

            app.UseStaticFiles(new StaticFileOptions()
            {
                FileProvider = new PhysicalFileProvider(staticAssets)
            });
        }

        return app;
    }

    private static void InitGameBasics(WebApplicationBuilder builder, string projectDirectory)
    {
        builder.Services.AddTransient<IDmlFrameworkFactory, BaseDmlFrameworkFactory>();

        builder.Services.AddTransient<IGameSimulationFactory>(sp =>
            new DmeProjectGameSimulationFactory(
                projectDirectory,
                sp.GetRequiredService<IWorldStateEncoderFactory>(),
                sp.GetRequiredService<IClientDispatcher>(),
                sp.GetRequiredService<IClientReceiver>(),
                sp.GetRequiredService<IDmlFrameworkFactory>()
            )
        );
    }

    private static void InitGameWorldStateEncoders(WebApplicationBuilder builder)
    {
        builder.Services.AddTransient(
            CascadeEncoder<IdentifierComponent>.Factory(new[]
            {
                typeof(PositionComponent),
                typeof(ActionableCommandsComponent),
                typeof(DirectionComponent),
                typeof(IconComponent),
                typeof(EntityVisibilityComponent),
                typeof(PlayerSessionOwnedComponent)
            })
        );

        bool isEntityTangible(in Entity entity)
        {
            return entity.Has<TangibleComponent>();
        }

        builder.Services.AddTransient(SimpleBroadcastEncoder<PositionComponent>.Factory("SetPosition"));
        builder.Services.AddTransient(SimpleBroadcastEncoder<ActionableCommandsComponent>.Factory("SetCommands"));
        builder.Services.AddTransient(SimpleBroadcastEncoder<DirectionComponent>.Factory("SetDirection"));
        builder.Services.AddTransient(
            SimpleBroadcastEncoder<IconComponent>.Factory("SetIcon", filter: isEntityTangible));

        //When owner is re-assigned, the client's needs the scoped information
        //TODO: Can this just be automated, since we can derive this reasoning for all scoped component encoders?
        builder.Services.AddTransient(SimpleScopedEncoder<PlayerSessionOwnedComponent>.Factory(
            "SetOwnership",
            new[]
            {
                typeof(EntityVisibilityEncoder)
            }
        ));

        builder.Services.AddTransient(EntityVisibilityEncoder.Factory());

        builder.Services.AddTransient<IWorldStateEncoderFactory, WorldEntityComponentEncoderFactory>();
    }

    private static void InitGameNetworkServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ClientStateService>();
        builder.Services.AddSingleton<IClientConnectionManager>(svc => svc.GetRequiredService<ClientStateService>());
        builder.Services.AddSingleton<IClientDispatcher>(svc => svc.GetRequiredService<ClientStateService>());
        builder.Services.AddSingleton<IClientReceiver>(svc => svc.GetRequiredService<ClientStateService>());
    }
}