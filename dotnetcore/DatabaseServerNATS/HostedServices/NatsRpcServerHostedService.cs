using DatabaseServerNATS.Extensions;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DatabaseServerNATS.HostedServices
{
    public class NatsRpcServerHostedService : IHostedService
    {
        private readonly INatsConnection _nats;
        private readonly IServiceProvider _serviceProvider;
        private readonly CancellationTokenSource _cts = new();

        public NatsRpcServerHostedService(INatsConnection nats, IServiceProvider serviceProvider)
        {
            _nats = nats;
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Auto-discover and bind all 30 handlers across the executing assembly
            _nats.BindAllRpcHandlers(_serviceProvider, Assembly.GetExecutingAssembly(), _cts.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Cancel subscription loops on graceful shutdown
            _cts.Cancel();
            return Task.CompletedTask;
        }
    }
}
