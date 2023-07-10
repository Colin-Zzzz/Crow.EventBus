using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    public class EventBusOptions
    {
        public EventBusOptions() 
        {
            Extensions = new List<RabbitMQOptionExtensions>();
        }
        public string ExchangeName { get; set; } = default!;
        public string QueueName { get; set; } = default!;
        public int RetryCount { get; set; }
        internal List<RabbitMQOptionExtensions> Extensions { get; }
        public void RegisterExtension(RabbitMQOptionExtensions extension)
        {
            if (extension == null) throw new ArgumentNullException(nameof(extension));

            Extensions.Add(extension);
        }
    }
}
