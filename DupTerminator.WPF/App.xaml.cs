using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Service;
using DupTerminator.DataBase;
using DupTerminator.DataBase.Extensions;
using DupTerminator.ImageHash.Extensions;
using DupTerminator.Pdf.Extensions;
using DupTerminator.WindowsSpecific;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;
using DupTerminator.WPF.Controls;
using DupTerminator.WPF.Service;
using DupTerminator.WPF.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SevenZipExtractor.Extensions;

namespace DupTerminator.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        private void OnExit(object sender, ExitEventArgs e)
        {
            // Dispose of services if needed
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();


            var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();

            var shell = _serviceProvider.GetRequiredService<MainWindow>();
            shell.DataContext = viewModel;

            shell.Closing += new CancelEventHandler(viewModel.OnClosing);
            shell.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Configure Logging
            services.AddLogging(config =>
            {
                config.AddDebug();
                config.SetMinimumLevel(LogLevel.Trace);
            });

            services
                .AddSingleton<MainViewModel>()
                .AddSingleton<MainWindow>()
                .AddTransient<SettingViewModel>();


            //services.AddSingleton<IImageLoadingService, ImageLoadingService>();
            services.AddSingleton<IImageProvider, ImageProvider>();
            services.AddTransient<ImageGroupsViewModel>();
            services.AddTransient<ImageListViewModel>();
            //services.AddTransient<ImageGroupViewerViewModel>();
            //services.AddTransient<ImageGroupViewer>(sp =>
            //{
            //    var window = new ImageGroupViewer();
            //    var vm = sp.GetRequiredService<ImageGroupViewerViewModel>();
            //    window.DataContext = vm;
            //    return window;
            //});

            services.AddSingleton<WpfLoggerProvider>();
            services.AddSingleton<ILoggerProvider>(provider =>
                provider.GetService<WpfLoggerProvider>());
            services.AddSingleton<ILoggerFactory>(provider =>
            {
                var factory = new LoggerFactory();
                factory.AddProvider(provider.GetService<WpfLoggerProvider>());
                return factory;
            });

            services.AddSingleton<ProgressDialogViewModel>();
            services.AddSingleton<IProgressDialogService, ProgressDialogService>();


            services.AddTransient<StartCommand>();


            //services.AddSingleton<UndoRedoEngine>();

            services.AddDataBase();
            services.AddPHashService();

            //services.AddLocalization(o => o.ResourcesPath = "Resources");

            services.AddArchive();
            services.AddPdf();
            services.AddWindowsUtil();

            services.AddSingleton<IMessageService, Service.MessageService>();
            //services.TryAddSingleton<DbArchiveService>();
        }
    }

}
