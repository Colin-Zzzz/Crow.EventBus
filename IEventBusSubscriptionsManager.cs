using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    public interface IEventBusSubscriptionsManager
    {
        bool IsEmpty { get; }

        event EventHandler<string> OnEventRemoved;
        void AddSubscription(string eventName, Type handleType);
        void RemoveSubscription(string eventName, Type handleType);
        bool HasSubscriptionsForEvent(string eventName);
        void Clear();
        IEnumerable<Type> GetHandlersForEvent(string eventName);
    }
}
