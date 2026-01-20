using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.BusinessLogic.Service;
using DupTerminator.DataBase;
using DupTerminator.WindowsSpecific;
using DupTerminator.WPF.Commands;
using DupTerminator.WPF.Controls;
using DupTerminator.WPF.Helper;
using DupTerminator.WPF.Model;
using DupTerminator.WPF.Service;
using Microsoft.Extensions.Logging;
using static System.Formats.Asn1.AsnWriter;

namespace DupTerminator.WPF.ViewModel
{
    internal class SettingViewModel : PropertyChangedBase, IDropable
    {
        private const string SETTINGS_FILE_NAME = "setting.json";
        private readonly IMd5Repository _md5Repository;
        private readonly IPhashRepository _phashRepository;
        private readonly IWindowsUtil _windowsUtil;
        private readonly IArchiveService _archiveService;
        private readonly IArchiveInfoRepository _archiveInfoRepository;
        private readonly IPdfInfoRepository _pdfInfoRepository;
        private readonly IPdfService _pdfService;
        private readonly IPHashService _pHashService;
        private readonly IMIHFactory _mihFactory;
        private readonly ILogger<SearcherMD5> _searchLogger;
        private readonly IProgressDialogService _progressDialogService;
        private readonly ImageListViewModel _imageListViewModel;

        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        public SettingViewModel(
            IMd5Repository md5Repository,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            IArchiveInfoRepository archiveInfoRepository,
            IPdfInfoRepository pdfInfoRepository,
            IPdfService pdfService,
            IPHashService pHashService,
            IMIHFactory mihFactory,
            ILogger<SearcherMD5> searchLogger,
            IProgressDialogService progressDialogService,
            ImageListViewModel imageListViewModel)
        {
            _md5Repository = md5Repository;
            _phashRepository = phashRepository;
            _windowsUtil = windowsUtil;
            _archiveService = archiveService;
            _archiveInfoRepository = archiveInfoRepository;
            _pdfInfoRepository = pdfInfoRepository;
            _pdfService = pdfService;
            _pHashService = pHashService;
            _mihFactory = mihFactory;
            _searchLogger = searchLogger;
            _progressDialogService = progressDialogService;
            _imageListViewModel = imageListViewModel;
            LoadSettings();
        }

        private void LoadSettings()
        {
            SettingsSerializable? settingsSerializable = SerializeHelper<SettingsSerializable>.Load(SETTINGS_FILE_NAME) ?? new SettingsSerializable();
            _locationsObservable = new ObservableCollection<SearchPathViewModel>(settingsSerializable.Locations);
            SearchSetting = settingsSerializable.SearchSetting;
            SelectedMode = Modes.SingleOrDefault(m => m.Name == settingsSerializable.SelectedMode);
            //Modes.Curr
        }

        //private void OnSearchCompleted(ReadOnlyCollection<ResultBase> results)
        //{
        //    SearchCompleted?.Invoke(this, new SearchCompletedEventArgs(results));
        //}
        private void OnSearchCompleted(ResultBase results)
        {
            SearchCompleted?.Invoke(this, new SearchCompletedEventArgs(results));
        }

        // Initialize Modes collection ONCE with concrete instances
        private readonly ObservableCollection<SettingsBase> _modes = new ObservableCollection<SettingsBase>(new List<SettingsBase>
        {
            new MD5ModeSettings(),
            new MD5ContainerSettings(),
            new PHashSettings(),
            new PHashContainerSettings(),
            new PHashSearchImageSettings(),
            new PHashSearchContainerSettings()
        });

        public ObservableCollection<SettingsBase> Modes => _modes;


        private SettingsBase _selectedMode;
        public SettingsBase SelectedMode
        {
            get { return _selectedMode; }
            set
            {
                _selectedMode = value;
                this.RaisePropertyChangedEvent();
            }
        }


        ObservableCollection <SearchPathViewModel> _locationsObservable;
        public ObservableCollection<SearchPathViewModel> Locations
        {
            get { return _locationsObservable; }
        }

        public SearchSetting SearchSetting { get; set; }



        public bool UseDB
        {
            get { return SearchSetting.UseDB; }
            set
            {
                SearchSetting.UseDB = value;
                this.RaisePropertyChangedEvent();
            }
        }

        public ulong? SkipLessThan
        {
            get { return SearchSetting.SkipLessThan; }
            set
            {
                SearchSetting.SkipLessThan = value;
                this.RaisePropertyChangedEvent();
            }
        }

        public ObservableCollection<string> IncludePattern
        {
            get { return SearchSetting.IncludePattern; }
            set
            {
                SearchSetting.IncludePattern = value;
                this.RaisePropertyChangedEvent();
            }
        }

        #region IDropable Members

        void IDropable.Drop(object dropData)
        {
            var filepaths = dropData as string[];
            if (filepaths != null)
            {
                foreach (string path in filepaths)
                {
                    if (!String.IsNullOrEmpty(path))
                    {
                        if (IOHelper.IsDirectory(path))
                        {
                            _locationsObservable.Add(new SearchPathViewModel
                            {
                                Path = path,
                                IsDirectory = true,
                                SearchInSubfolder = true
                                //Image = IconReader.GetIcon(path, true);
                            });
                        }
                        else if (System.IO.File.Exists(path))
                        {
                            _locationsObservable.Add(new SearchPathViewModel
                            {
                                Path = path,
                                IsDirectory = false
                            });
                        }
                    }
                }
            }
        }


        #endregion

        internal void Save()
        {
            SettingsSerializable settingsSerializable = new SettingsSerializable();
            settingsSerializable.Locations = _locationsObservable.ToArray();
            settingsSerializable.SearchSetting = SearchSetting;
            settingsSerializable.SelectedMode = SelectedMode.Name;
            SerializeHelper<SettingsSerializable>.Save(settingsSerializable, SETTINGS_FILE_NAME);
        }

        ICommand _startCommand;
        public ICommand StartCommand
        {
            get
            {
                //var locations = new ReadOnlyCollection<SearchPath>(
                //    Locations
                //    .Select(location => new SearchPath(location.Path, location.IsDirectory, location.SearchInSubFolder))
                //    .ToList());
                return _startCommand ?? (_startCommand = new StartCommand(
                    //locations,
                    //searchSetting,
                    this,
                    _imageListViewModel,
                    _md5Repository,
                    _phashRepository,
                    _archiveInfoRepository,
                    _pdfInfoRepository,
                    _windowsUtil,
                    _archiveService,
                    _pdfService,
                    _pHashService,
                    _mihFactory,
                    _searchLogger,
                    _progressDialogService,
                    OnSearchCompleted));
            }
        }

        private string _totalInfo;
        public string TotalInfo
        {
            get { return _totalInfo; }
            set
            {
                _totalInfo = value;
                RaisePropertyChangedEvent();
            }
        }
    }
}
