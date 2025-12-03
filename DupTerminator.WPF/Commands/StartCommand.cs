using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.BusinessLogic.Service;
using DupTerminator.DataBase;
using DupTerminator.Pdf;
using DupTerminator.WPF.Model;
using DupTerminator.WPF.Service;
using DupTerminator.WPF.View;
using DupTerminator.WPF.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig.Tokens;

namespace DupTerminator.WPF.Commands
{
    internal class StartCommand : ICommand
    {
        private readonly SettingViewModel _settingViewModel;

        //private readonly ReadOnlyCollection<SearchPath> _locations;
        //private readonly SearchSetting _searchSetting;
        private readonly IDBManager _dBManager;
        private readonly IPhashRepository _phashRepository;
        private readonly IWindowsUtil _windowsUtil;
        private readonly IArchiveService _archiveService;
        private readonly DbArchiveService _dbArchiveService;
        private readonly IPHashService _pHashService;
        private readonly IMIHFactory _mihFactory;
        private readonly ILogger<Searcher> _serachLogger;
        private readonly IProgressDialogService _progressDlg;
        //private readonly Action<ReadOnlyCollection<ResultBase>?> _updateResults;
        private readonly Action<ResultBase?> _updateResults;

        public StartCommand
            (
            SettingViewModel settingViewModel,
            //ReadOnlyCollection<SearchPath> locations,
            //SearchSetting searchSetting,
            IDBManager dBManager,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            DbArchiveService dbArchiveService,
            IPHashService pHashService,
            IMIHFactory mihFactory,
            ILogger<Searcher> serachLogger,
            IProgressDialogService progressDlg,
            //Action<ReadOnlyCollection<ResultBase>?> updateResults)
            Action<ResultBase?> updateResults)
        {
            _settingViewModel = settingViewModel;
            //_locations = locations;
            //_searchSetting = searchSetting;
            _dBManager = dBManager;
            _phashRepository = phashRepository;
            _windowsUtil = windowsUtil;
            _archiveService = archiveService;
            _dbArchiveService = dbArchiveService;
            _pHashService = pHashService;
            _mihFactory = mihFactory;
            _serachLogger = serachLogger;
            _progressDlg = progressDlg;
            _updateResults = updateResults;
        }

        /// <summary>
        /// Actions to take when CanExecute() changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => true;

        public async void Execute(object? parameter)
        {
            var locations = new ReadOnlyCollection<SearchPath>(_settingViewModel.Locations
               .Select(location => new SearchPath(location.Path, location.IsDirectory, location.SearchInSubfolder))
               .ToList());

            if (_settingViewModel.SelectedMode is MD5ModeSettings)
            {
                var searcher = new Searcher(
                   locations,
                   _settingViewModel.SearchSetting,
                    _dBManager,
                    _windowsUtil,
                   _archiveService,
                   _dbArchiveService,
                   _serachLogger);

                await _progressDlg.RunAsync(async (progress, cancelToken) =>
                {
                    //ReadOnlyCollection<DuplicateGroup>? result = await Task.Run(() => searcher.StartAsync(progress, cancelToken)).ConfigureAwait(false);
                    ReadOnlyCollection<DuplicateGroup>? result = await searcher.StartAsync(progress, cancelToken);
                    //ResultType2 type1 = new ResultType2 { Discount = 4536 };
                    //ResultType2 type2 = new ResultType2 { Discount = 4537 };
                    //var col = new ReadOnlyCollection<ResultBase>(new[] { type1, type2 } );
                    _updateResults(new MD5Result(result));
                });
            }
            else if (_settingViewModel.SelectedMode is DuplicateContainerSettings modeSettings)
            {
                var searcher = new SearcherContainer(
                   locations,
                   _settingViewModel.SearchSetting,
                   modeSettings,
                    _dBManager,
                    _windowsUtil,
                   _archiveService,
                   _dbArchiveService,
                   _serachLogger);

                await _progressDlg.RunAsync(async (progress, cancelToken) =>
                {
                    var result = await searcher.StartAsync(progress, cancelToken);
                    //ResultType1 type1 = new ResultType1 { Property1 = "gffhrftg", Property2 = 2 };
                    //ResultType1 type2 = new ResultType1 { Property1 = "4536", Property2 = 3 };
                    //var col = new ReadOnlyCollection<ResultBase>(new[] { type1, type2 } );
                    _updateResults(new DuplicateContainerResult(result));
                });
            }
            else if (_settingViewModel.SelectedMode is PHashSettings pHashSettings)
            {
                var searcher = new SearcherPhash(
                   locations,
                   _settingViewModel.SearchSetting,
                   pHashSettings,
                    _phashRepository,
                    _windowsUtil,
                   _archiveService,
                   //_dbArchiveService,
                   _pHashService,
                   _mihFactory,
                   _serachLogger);

                await _progressDlg.RunAsync(async (progress, cancelToken) =>
                {
                    var result = await searcher.StartAsync(progress, cancelToken);
                    progress.Report(new ProgressDto { State = "UpdateResultOnGUI"});
                    _updateResults(new PHashResult(result));
                });
            }



            //var activeWindow = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
            //var progressVM = new ProgressDialogViewModel();
            //var progressWindow = new ProgressWindow
            //{
            //    DataContext = progressVM,
            //    //Owner = Application.Current.MainWindow
            //    Owner = activeWindow
            //};

                //// Handle close prevention during operation
                //progressWindow.Closing += (s, e) =>
                //{
                //    if (!progressVM.CancellationToken.IsCancellationRequested)
                //    {
                //        e.Cancel = true; // Prevent closing
                //        progressVM.Cancel();
                //    }
                //};

                //// Create progress reporter with throttling
                //IProgress<ProgressDto> progress = new ProgressWithTimer<ProgressDto>(
                //    TimeSpan.FromMilliseconds(100),
                //    progressVM.UpdateProgress
                //);



                //var searcher = new Searcher(
                //   _locations,
                //    new SearchSetting(),
                //    _dBManager,
                //    _windowsUtil,
                //   progress,
                //    progressVM.CancellationToken, // Pass cancellation token
                //   _archiveService,
                //   _serachLogger);


                //progressVM.CancellationToken.Register(() => searcher.Cancell());
                //progressWindow.Show();

                //try
                //{
                //    await searcher.Start();
                //}
                //catch (OperationCanceledException)
                //{
                //    // Handle cancellation
                //}
                //finally
                //{
                //    // Safely close window
                //    progressWindow.Closing -= (s, e) => { };
                //    progressWindow.Close();
                //}



                ////результаты возврашает через событие DuplicateFileListAvailableDelegate DuplicatFileListAvailableEventHandler(System.Collections.ArrayList duplicateList)


        }
    }
}
