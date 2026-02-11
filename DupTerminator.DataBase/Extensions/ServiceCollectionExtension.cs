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
            services.AddSingleton<IMd5Repository, Md5Repository>();
            //services.AddSingleton<IArchiveInfoRepository, ArchiveInfoRepository>();
            //services.AddSingleton<IArchiveInfoRepository, ArchiveInfoRepositoryMesP>();
            services.AddSingleton<IArchiveInfoRepository, ArchiveInfoRepositoryMemP>();
            services.AddSingleton<IPdfInfoRepository, PdfInfoRepository>();
            services.AddSingleton<IPhashRepository, PhashRepository>();
        }
    }
}
