using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace DupTerminator.ImageHash.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddPHashService(this ServiceCollection services)
        {
            services.AddSingleton<IPHashService, PHashService>();
            services.AddSingleton<IMIHFactory, MIHFactory>();
        }
    }
}
