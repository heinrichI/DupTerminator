using DupTerminator.BusinessLogic;
using DupTerminator.DataBase;
using DupTerminator.View;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using SevenZipExtractor.Extensions;

namespace DupTerminator
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            var services = new ServiceCollection();

            ConfigureServices(services);

            var cultureInfo = new CultureInfo("ru-ru");
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // Add event handler for thread exceptions
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
          

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                var view = serviceProvider.GetRequiredService<MainForm>();
                Application.Run(view);
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            services
                .AddScoped<MainViewModel>()
                .AddScoped<MainPresenter>()
                .AddScoped<MainForm>();

            services.AddSingleton<UndoRedoEngine>();

            var path = Path.Combine(System.Windows.Forms.Application.StartupPath, "database.db3");
            services.AddSingleton<IDBManager>(new DBManager(path, new MessageService()));

            services.AddLogging(config =>
            {
                //config.();
                //config.AddConsole();
            });

            services.AddLocalization(o => o.ResourcesPath = "Resources");

            services.AddArchive();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            new CrashReport("UnhandledException", (Exception)e.ExceptionObject).ShowDialog();
        }
    }
}