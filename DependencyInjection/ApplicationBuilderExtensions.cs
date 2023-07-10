using Crow.EventBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseEventBus(this IApplicationBuilder app)
        {
            object? eventBus = app.ApplicationServices.GetService(typeof(IEventBus));
            if (eventBus == null)
            {
                throw new ApplicationException("找不到IEventBus实例");
            }
            return app;
        }
    }
}
