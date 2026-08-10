using System.Reflection;
using DatabaseServerNATS.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace DatabaseServerNATS.Extensions
{
    public static class NatsServiceRegistrationExtensions
    {
        public static IServiceCollection AddNatsRpcHandlers(this IServiceCollection services, Assembly assembly)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces(), (type, iFace) => new { type, iFace })
                .Where(row => row.iFace.IsGenericType && row.iFace.GetGenericTypeDefinition() == typeof(INatsRpcHandler<,>));

            foreach (var item in handlerTypes)
            {
                // Registers e.g. INatsRpcHandler<GetUserRequest, GetUserResponse> -> GetUserHandler
                services.AddScoped(item.iFace, item.type);
            }

            return services;
        }
    }
}
