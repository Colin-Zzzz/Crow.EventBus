using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    public class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
    {
        private readonly Dictionary<string,List<Type>> _handles;
        private readonly List<Type> _eventTypes;
        public event EventHandler<string> OnEventRemoved=default!;
        public InMemoryEventBusSubscriptionsManager()
        {
            _handles=new Dictionary<string, List<Type>>();
            _eventTypes=new List<Type>();   
        }
        public void AddSubscription(string eventName, Type handleType)
        {
            if(!HasSubscriptionsForEvent(eventName))
            {
                _handles.Add(eventName, new List<Type>());
            }
            if (_handles[eventName].Contains(handleType)) 
            {
                throw new ArgumentException($"Handler Type {handleType} already registered for '{eventName}'", nameof(handleType));
            }
            _handles[eventName].Add(handleType);
        }
        public bool IsEmpty=>_handles.Keys.Any();
        public void Clear()=>_handles.Clear();
        public IEnumerable<Type> GetHandlersForEvent(string eventName) => _handles[eventName];
        public bool HasSubscriptionsForEvent(string eventName)=>_handles.ContainsKey(eventName);
        public void RemoveSubscription(string eventName, Type handleType)
        {
            _handles[eventName].Remove(handleType);
            if (!_handles.ContainsKey(eventName))
            {
                _handles.Remove(eventName);
            }
            RaiseOnEventRemoved(eventName);
        }
        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this, eventName);
        }
    }
}
