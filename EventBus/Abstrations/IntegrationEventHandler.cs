using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    public abstract class IntegrationEventHandler<T> : IBaseIntegrationEventHandler
    {
        public abstract Task Handle( T eventData);
        public Task Handle(string eventData)
        {
            var data = JsonConvert.DeserializeObject<T>(eventData);
            return Handle(data!);
        }
    }
}
