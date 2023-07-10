

using Crow.EventBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services,Action<EventBusOptions> configure)
        {
            EventBusOptions options= new EventBusOptions();
            configure.Invoke(options);

            foreach (var item in options.Extensions)
            {
                item.AddServices(services);
            }
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Type> eventHandlers = new List<Type>();
            //用GetTypes()，这样非public类也能注册
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes().Where(t => t.IsAbstract == false && t.IsAssignableTo(typeof(IBaseIntegrationEventHandler)));
                eventHandlers.AddRange(types);
            }

            services.AddEventBus(options, eventHandlers);
            return services;
        }
        public static IServiceCollection AddEventBus(this IServiceCollection services, EventBusOptions eventBusOptions, IEnumerable<Type> eventHandlerTypes)
        {
            foreach (var type in eventHandlerTypes)
            {
                services.AddScoped(type, type);
            }
            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

            services.AddSingleton<IRabbitMQPersistentConnection>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<DefaultRabbitMQPersistentConnection>>();
                var rabbitMQOptions = provider.GetRequiredService<IOptions<RabbitMQOptions>>().Value;
                var factory = new ConnectionFactory()
                {
                    HostName = rabbitMQOptions.HostName,
                    UserName = rabbitMQOptions.UserName,
                    Password = rabbitMQOptions.Password,
                    DispatchConsumersAsync = true
                };
                return new DefaultRabbitMQPersistentConnection(factory, logger, eventBusOptions.RetryCount);
            });
            services.AddSingleton<IEventBus, EventBusRabbitMQ>(sp =>
            {
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
                var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var logger = sp.GetRequiredService<ILogger<EventBusRabbitMQ>>();

                var retryCount = 5;
                if (eventBusOptions.RetryCount > 0)
                    retryCount = eventBusOptions.RetryCount;

                var eventBus= new EventBusRabbitMQ(serviceScopeFactory, rabbitMQPersistentConnection, eventBusSubcriptionsManager, eventBusOptions.ExchangeName, eventBusOptions.QueueName, logger, retryCount);
                foreach (var handlerType in eventHandlerTypes)
                {
                    var attrs = handlerType.GetCustomAttributes<SubscribeAttribute>();
                    if(!attrs.Any())
                        throw new ApplicationException($"There shoule be at least one EventNameAttribute on {handlerType}");

                    foreach (var attr in attrs)
                    {
                        eventBus.Subscribe(attr.EventName, handlerType);
                    }
                }
                return eventBus;
            });

            return services;
        }
    }
}
