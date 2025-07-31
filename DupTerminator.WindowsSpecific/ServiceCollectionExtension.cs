using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace DupTerminator.WindowsSpecific
{
    public static class ServiceCollectionExtension
    {
        public static void AddWindowsUtil(this ServiceCollection services)
        {
            services.AddSingleton<IWindowsUtil, WindowsUtil>();
        }
    }
}
