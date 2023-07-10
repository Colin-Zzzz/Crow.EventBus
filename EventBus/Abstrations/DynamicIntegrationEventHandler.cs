using Dynamic.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    public abstract class DynamicIntegrationEventHandler : IBaseIntegrationEventHandler
    {
        public abstract Task Handle(dynamic eventData);
        public Task Handle(string eventData)
        {
            var data = DJson.Parse(eventData);
            return Handle(data);
        }
    }
}
