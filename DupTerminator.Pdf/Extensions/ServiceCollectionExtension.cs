using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace DupTerminator.Pdf.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddPdf(this ServiceCollection services)
        {
            services.AddSingleton<IPdfService, PdfService>();
        }
    }
}
