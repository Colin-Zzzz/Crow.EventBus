using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    public interface IBaseIntegrationEventHandler
    {
        Task Handle(string eventData);
    }
}
