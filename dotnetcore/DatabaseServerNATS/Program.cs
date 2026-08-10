using Database;
using Database.SqlServer;
using DatabaseServerNATS.Application;
using DatabaseServerNATS.Extensions;
using DatabaseServerNATS.HostedServices;
using DatabaseServerNATS.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Hosting;
using Serilog;

//
// Infantry Database Server that communicates over NATS (finally!).
//
// See README.md for details.
//

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
);

// Add NATS as our message bus/broker, used for Request-Reply (RPC) messages
// that zone servers and others send to us. Read the handlers and documentation
// to see how messages are handled.

builder.Services.AddNats(
    poolSize: 1,
    configureOpts: opts => opts with {
        Url = builder.Configuration["Nats:Url"]!
    }
);

// Register configuration sections.

builder.Services.AddOptions<ZoneServerOptions>()
    .Bind(builder.Configuration.GetSection(ZoneServerOptions.SectionName))
    .ValidateOnStart();

// Register back-end database (SQL Server for now, SQLite to come).

var sqlServerSection = builder.Configuration.GetSection(SqlServerDatabaseOptions.SectionName);

if (sqlServerSection.Exists())
{
    var sqlOpts = sqlServerSection.Get<SqlServerDatabaseOptions>()!;

    builder.Services.AddPooledDbContextFactory<SqlServerDbContext>(opts => {
        opts.UseSqlServer(sqlOpts.ConnectionString);

        if (sqlOpts.UseLazyLoading)
        {
            opts.UseLazyLoadingProxies();
        }
    });

    builder.Services.AddScoped<InfantryDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SqlServerDbContext>>().CreateDbContext());
}

// Register all the services we are going to use.

builder.Services.AddNatsRpcHandlers(typeof(Program).Assembly);
builder.Services.AddHostedService<NatsRpcServerHostedService>();
builder.Services.AddHostedService<HeartbeatCheckBackgroundService>();

// Lastly, add the actual application that contains our long-running state.

builder.Services.AddSingleton<ServerStore>();

using var host = builder.Build();

// Go ahead and start the server and listen for CTRL+C to terminate.
await host.RunAsync();
