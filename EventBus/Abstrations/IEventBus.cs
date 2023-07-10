using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Crow.EventBus
{
    /// <summary>
    /// 事件总线接口
    /// </summary>
    public interface IEventBus
    {
        void Publish(string eventName,object data);
        void Subscribe(string eventName, Type handlerType);
        void Unsubscribe(string eventName, Type handlerType);
    }
}
