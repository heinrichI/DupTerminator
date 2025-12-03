using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("DupTerminator.Benchmark")]
namespace SevenZipExtractor.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddArchive(this ServiceCollection services)
        {
            services.AddSingleton<IArchiveService, ArchiveService>();
        }
    }
}
