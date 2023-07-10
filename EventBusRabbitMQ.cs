using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    public class EventBusRabbitMQ : IEventBus, IDisposable
    {
        private readonly IServiceScope _serviceScope;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRabbitMQPersistentConnection _persistentConnection;
        private readonly IEventBusSubscriptionsManager _eventBusSubscriptionsManager;
        private readonly ILogger<EventBusRabbitMQ> _logger;
        private readonly int _retryCount;

        private IModel _consumerChannel;
        private readonly string _exchangeName;
        private string _queueName;

        public EventBusRabbitMQ(
            IServiceScopeFactory serviceProviderFactory,
            IRabbitMQPersistentConnection persistentConnection, 
            IEventBusSubscriptionsManager eventBusSubscriptionsManager,
            string exchangeName,
            string queueName,
            ILogger<EventBusRabbitMQ> logger,
            int retryCount=5
            )
        {
            _serviceScope = serviceProviderFactory.CreateScope();
            _serviceProvider = _serviceScope.ServiceProvider;
            _persistentConnection =persistentConnection;
            _eventBusSubscriptionsManager= eventBusSubscriptionsManager;
            _exchangeName = exchangeName;
            _queueName=queueName;
            _logger = logger;
            _retryCount=retryCount;
            _consumerChannel = CreateConsumerChannel();
            _eventBusSubscriptionsManager.OnEventRemoved += OnEventRemoved;
        }

        

        public void Publish(string eventName, object data)
        {
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {Timeout}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                });

            using var channel = _persistentConnection.CreateModel();
            channel.ExchangeDeclare(_exchangeName, "direct");

            var message = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2;

                channel.BasicPublish(_exchangeName, eventName, mandatory: true, properties, body);
            });
        }

        public void Subscribe(string eventName, Type handlerType)
        {
            CheckHandleType(handlerType);
            //绑定队列到交换机
            DoInternalSubscription(eventName);
            //添加订阅
            _eventBusSubscriptionsManager.AddSubscription(eventName, handlerType);

            StartBasicConsume();
        }

        public void Unsubscribe(string eventName, Type handlerType)
        {
            CheckHandleType(handlerType);
            _eventBusSubscriptionsManager.RemoveSubscription(eventName, handlerType);
        }
        public void Dispose()
        {
            _consumerChannel?.Dispose();
            _eventBusSubscriptionsManager.Clear();
        }
        private IModel CreateConsumerChannel()
        {
            if(!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            _logger.LogTrace("Creating RabbitMQ consumer channel");

            var channel=_persistentConnection.CreateModel();
            //创建交换机
            channel.ExchangeDeclare(_exchangeName, "direct");
            //创建队列
            channel.QueueDeclare(_queueName, true, false, false, null);

            channel.CallbackException += (sender, e) =>
            {
                _logger.LogWarning(e.Exception, "Recreating RabbitMQ consumer channel");
                _consumerChannel?.Dispose();
                _consumerChannel=CreateConsumerChannel();
                StartBasicConsume();
            };
            return channel;
        }

        private void StartBasicConsume()
        {
            _logger.LogTrace("Starting RabbitMQ basic consume");
            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
                consumer.Received += OnConsumerReceived;
                _consumerChannel.BasicConsume(_queueName, false, consumer);
            }
            else
            {
                _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs @event)
        {
            var eventName = @event.RoutingKey;
            var message = Encoding.UTF8.GetString(@event.Body.Span);

            try
            {
                if (message.ToLowerInvariant().Contains("throw-fake-exception"))
                {
                    throw new InvalidOperationException($"Fake exception requested: \"{message}\"");
                }

                await ProcessEvent(eventName,message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "----- ERROR Processing message \"{Message}\"", message);
                //如果发生异常，将消息进入到死信队列，通过死信队列消费
                _consumerChannel.BasicNack(@event.DeliveryTag, false, false);
            }
            //确认已消费
            _consumerChannel.BasicAck(@event.DeliveryTag, false);
        }

        private async Task ProcessEvent(string eventName,string message)
        {
            _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);
            if(_eventBusSubscriptionsManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions=_eventBusSubscriptionsManager.GetHandlersForEvent(eventName);
                foreach ( var subscription in subscriptions)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler=scope.ServiceProvider.GetService(subscription) as IBaseIntegrationEventHandler;
                    if(handler is not null)
                    {
                        await Task.Yield();
                        await handler.Handle(message);
                    }
                }
            }
            else
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
            }
        }
        private void OnEventRemoved(object? sender, string eventName)
        {
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            using var channel = _persistentConnection.CreateModel();
            channel.QueueUnbind(_queueName, _exchangeName, eventName);

            if(_eventBusSubscriptionsManager.IsEmpty)
            {
                _queueName = string.Empty;
                _consumerChannel.Close();
            }
        }
        private void DoInternalSubscription(string eventName)
        {
            var hasKey = _eventBusSubscriptionsManager.HasSubscriptionsForEvent(eventName);
            if(!hasKey)
            {
                if (!_persistentConnection.IsConnected)
                    _persistentConnection.TryConnect();

                _consumerChannel.QueueBind(_queueName, _exchangeName, eventName);
            }
        }
        private void CheckHandleType(Type handlerType)
        {
            if (!typeof(IBaseIntegrationEventHandler).IsAssignableFrom(handlerType))
                throw new ArgumentException($"{handlerType} doesn't inherit from IIntegrationEventHandler", nameof(handlerType));
        }
    }
}
