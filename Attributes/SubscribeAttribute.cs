using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SubscribeAttribute : Attribute
    {
        public SubscribeAttribute(string eventName)
        {
            EventName = eventName;
        }
        public string EventName { get; init; }
    }
}
