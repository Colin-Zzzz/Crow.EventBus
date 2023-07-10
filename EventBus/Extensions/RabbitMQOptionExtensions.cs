using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    public class RabbitMQOptionExtensions
    {
        private readonly Action<RabbitMQOptions> _configure;

        public RabbitMQOptionExtensions(Action<RabbitMQOptions> configure)
        {
            _configure = configure;
        }

        public void AddServices(IServiceCollection services)
        {
            services.Configure(_configure);
        }
    }
}
