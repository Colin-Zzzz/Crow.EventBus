using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    public static class EventBusOptionExtensions
    {
        public static EventBusOptions UseRabbitMQ(this EventBusOptions options,Action<RabbitMQOptions> configure)
        {
            options.RegisterExtension(new RabbitMQOptionExtensions(configure));
            return options;
        }
    }
}
