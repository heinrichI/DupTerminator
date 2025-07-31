using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace DupTerminator.DataBase.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddDataBase(this ServiceCollection services)
        {
            services.AddSingleton<IDBManager, DBManager>();
            services.AddSingleton<IExtendedFileInfoRepository, ExtendedFileInfoRepository>();
        }
    }
}
