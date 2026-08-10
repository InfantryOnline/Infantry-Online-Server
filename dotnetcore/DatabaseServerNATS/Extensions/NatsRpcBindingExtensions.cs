using DatabaseServerNATS.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Net;
using System.Reflection;

namespace DatabaseServerNATS.Extensions
{
    public static class NatsRpcBindingExtensions
    {
        /// <summary>
        /// Starts a long-running background task that listens for requests on a subject,
        /// executes the handler in a fresh DI scope, and sends a reply using msg.ReplyAsync.
        /// </summary>
        public static Task RegisterRpcHandlerAsync<TRequest, TResponse>(
            this INatsConnection nats,
            string subject,
            IServiceProvider serviceProvider,
            CancellationToken ct = default)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await foreach (NatsMsg<TRequest> msg in nats.SubscribeAsync<TRequest>(subject, cancellationToken: ct))
                    {
                        if (msg.Data is null) continue;

                        // Execute each request in a background Task so slow handlers don't block the stream
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // 1. Create a fresh DI scope per request
                                using var scope = serviceProvider.CreateScope();
                                var handler = scope.ServiceProvider.GetRequiredService<INatsRpcHandler<TRequest, TResponse>>();

                                var context = new NatsRequestContext<TRequest>(
                                    Payload: msg.Data,
                                    Subject: msg.Subject,
                                    ReplyTo: msg.ReplyTo,
                                    Headers: msg.Headers
                                );

                                TResponse response = await handler.HandleAsync(context, ct);

                                await msg.ReplyAsync(response, cancellationToken: ct);
                            }
                            catch (Exception ex)
                            {
                                // Isolate handler failures so loop stays alive
                                var logger = serviceProvider.GetService<ILogger<INatsClient>>();
                                logger?.LogError(ex, "Error processing RPC request on subject {Subject}", subject);
                            }
                        }, ct);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Graceful shutdown
                }
            }, ct);
        }

        /// <summary>
        /// Finds all INatsRpcHandler implementations decorated with [NatsSubject] and registers listening loops.
        /// </summary>
        public static void BindAllRpcHandlers(
            this INatsConnection nats,
            IServiceProvider provider,
            Assembly assembly,
            CancellationToken ct = default)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.GetCustomAttribute<NatsSubjectAttribute>() != null)
                .Select(type => new
                {
                    Type = type,
                    Subject = type.GetCustomAttribute<NatsSubjectAttribute>()!.Subject,
                    Interface = type.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INatsRpcHandler<,>))
                });

            var registerMethod = typeof(NatsRpcBindingExtensions)
                .GetMethod(nameof(NatsRpcBindingExtensions.RegisterRpcHandlerAsync));

            foreach (var handler in handlerTypes)
            {
                var reqType = handler.Interface.GetGenericArguments()[0];
                var resType = handler.Interface.GetGenericArguments()[1];

                // Construct generic method: RegisterRpcHandlerAsync<TRequest, TResponse>
                var genericRegister = registerMethod!.MakeGenericMethod(reqType, resType);

                // Execute method to start the background subscription loop
                genericRegister.Invoke(null, new object[] { nats, handler.Subject, provider, ct });
            }
        }
    }
}
