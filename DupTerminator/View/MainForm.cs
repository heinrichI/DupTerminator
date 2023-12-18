//#define ExtLang  //извлечь языки в xml

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.IO;
using DupTerminator.Native;
using DupTerminator.Util;
using System.Globalization;
using System.Xml;
using System.Collections;
using System.Threading;
using DupTerminator.Localize;
using DupTerminator.BusinessLogic.ObjectModel;
using DupTerminator.Native.Taskbar;
using Microsoft.Extensions.DependencyInjection;
using DupTerminator.BusinessLogic;
using System.Collections.ObjectModel;
using DupTerminator.WindowsSpecific;
using Microsoft.Extensions.Localization;
using System.Diagnostics;
using System.Text.Json;

namespace DupTerminator.View
{
    // In Supervising Controller, the view interacts directly with the model 
    // to perform simple data-binding that can be defined declaratively, 
    // without presenter intervention. 

    internal partial class MainForm : BaseForm, IMainView
    {
        private const int MARGIN_BETWEEN_PB = 3;

        public event EventHandler<AddFolderEventArgs> AddFolderEvent;

        private ToolTip _ttMainForm;
        private readonly IServiceProvider _serviceProvider;
        private readonly MainViewModel _model;

        private FileFunctions _fFunctions;
        private Searcher _searcher;

        //Properties.Settings mySettings = new Properties.Settings();
        private Settings _settings; //= new Settings(); //экземпляр класса с настройками 
        private DateTime _timeStart;
        //private int _lastCount;
        //private bool _cancell = false;

        //обработчки сортировки колонок
        //private ListViewGroupSorter lvwGroupSorter;
        private ListViewSaveGroupSorter _lvwGroupSorter;

        //public const string defaultDirectory = "default";
        private VersionManager.UpdateChecker _updateChecker = null;
        private VersionManager.VersionInfo _versionInfo = null;

        // The smaller the number the less sensitive
        const uint SHOW_SENSITIVITY = 5;

        private ITaskbarList3 _taskbarProgress;

        private UndoRedoEngine _undoRedoEngine;
        private readonly IDBManager _dbManager;
        private readonly IStringLocalizer<MainForm> _stringLocalizer;
        private readonly IArchiveService _archiveService;
        private bool _beginUpdate = false;

        private enum StatusState
        {
            Search,
            Duplicate
        }

        private Font _fontRegular;
        private Font _fontStrikeout;

        /*private PictureBox pictureBoxNext;
        private PictureBox pictureBoxPrev;
        private PictureBox pictureBox1;*/
        private FileViewer _pictureBox1;
        private FileViewer _pictureBoxNext;
        private FileViewer _pictureBoxPrev;

        public event EventHandler AboutClick;

        public MainForm(
            IServiceProvider serviceProvider,
            MainViewModel model,
            UndoRedoEngine undoRedoEngine,
            IDBManager dbManager,
            IStringLocalizer<MainForm> stringLocalizer,
            IArchiveService archiveService)
        {
            InitializeComponent();
            //dataGridView1.DataBindingComplete += (o, _) =>
            //{
            //    var dataGridView = o as DataGridView;
            //    if (dataGridView != null)
            //    {
            //        dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            //        dataGridView.Columns[dataGridView.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            //    }
            //};

            InitializePreviewBox();



            _serviceProvider = serviceProvider;
            _model = model;

            _undoRedoEngine = undoRedoEngine ?? throw new ArgumentNullException(nameof(undoRedoEngine));
            _dbManager = dbManager;
            _stringLocalizer = stringLocalizer;
            _archiveService = archiveService;
            if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1)
                || Environment.OSVersion.Version.Major > 6)
                _taskbarProgress = (ITaskbarList3)new ProgressTaskbar();

            // Create an instance of a ListView column sorter and assign it 
            // to the ListView control.
            //lvwGroupSorter = new ListViewGroupSorter();
            _lvwGroupSorter = new ListViewSaveGroupSorter();
            //_listDuplicates = new ListViewSave();
            _settings = Settings.GetInstance();
            //_listDuplicates = new ListViewSave();


            toolStripMenuItem_About.Click += (s, e) => AboutClick?.Invoke(s, e);

            SetBinding();

            _fFunctions = new FileFunctions(dbManager);
        }

        private void SetBinding()
        {
            Binding bindingImageSeacrhState = new Binding("Image", _model, nameof(MainViewModel.SeacrhState));
            bindingImageSeacrhState.Format += (s, e) =>
            {
                if (e.Value is SeacrhState state)
                {
                    if (state == SeacrhState.Search)
                    {
                        e.Value = Properties.Resources.pause_32;
                    }
                    else if (state == SeacrhState.Pause)
                    {
                        e.Value = Properties.Resources.play_32;
                    }
                    else if (state == SeacrhState.ShowDuplicate)
                    {
                        e.Value = Properties.Resources.play_32;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(state));
                    }
                }
            };
            toolStripButtonStart.DataBindings.Add(bindingImageSeacrhState);


            Binding bindingToolTipSeacrhState = new Binding("ToolTipText", _model, nameof(MainViewModel.SeacrhState));
            bindingToolTipSeacrhState.Format += (s, e) =>
            {
                if (e.Value is SeacrhState state)
                {
                    if (state == SeacrhState.Search)
                    {
                        e.Value = LanguageManager.GetString("toolTip_PauseSearch");
                    }
                    else if (state == SeacrhState.ShowDuplicate)
                    {
                        e.Value = LanguageManager.GetString("toolTip_buttonStart");
                    }
                    else if (state == SeacrhState.Pause)
                    {
                        e.Value = LanguageManager.GetString("toolTip_ResumeSearch");
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(state));
                    }
                }
            };
            toolStripButtonStart.DataBindings.Add(bindingToolTipSeacrhState);

            toolStripButtonCancel.DataBindings.Add(CreateBindingEnableToSeacrhState());

            Binding CreateBindingEnableToSeacrhState()
            {
                Binding bindingEnableToSeacrhState = new Binding("Enabled", _model, nameof(MainViewModel.SeacrhState));
                bindingEnableToSeacrhState.Format += (s, e) =>
                {
                    if (e.Value is SeacrhState state)
                    {
                        if (state == SeacrhState.Search || state == SeacrhState.Pause)
                        {
                            e.Value = true;
                        }
                        else
                        {
                            e.Value = false;
                        }
                    }
                };
                return bindingEnableToSeacrhState;
            }

            //dataGridView1.DataBindings.Add(nameof(dataGridView1.DataSource), _model, nameof(MainViewModel.Dublicates));
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            if (_settings.Fields.IsSaveLoadListDub)
            {
                Application.DoEvents();
                Load_ListDuplicate(_settings.Fields.LastJob);
                //Thread tLoad = new Thread(new ThreadStart(Load_ListDuplicate));
                //tLoad.Start();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            readSetting();

#if ExtLang
            SaveLanguages();
#else
            InitLanguage();
#endif

            if (_settings.Fields.IsCheckUpdate)
            {
                toolStripMenuItem_CheckForUpdate.Enabled = false;
                _updateChecker = new VersionManager.UpdateChecker(false);
                _updateChecker.VersionChecked += new VersionManager.UpdateChecker.NewVersionCheckedHandler(versionChecker_NewVersionChecked);
            }

            SetStatusState(StatusState.Search);

            _ttMainForm = new ToolTip();
            toolStripButtonCancel.ToolTipText = LanguageManager.GetString("toolTip_buttonCancel");
            toolStripButtonUndo.ToolTipText = LanguageManager.GetString("toolTip_buttonUndo");
            toolStripButtonRedo.ToolTipText = LanguageManager.GetString("toolTip_buttonRedo");
            toolStripButtonDeleteSelectedFiles.ToolTipText = LanguageManager.GetString("toolTip_buttonDeleteSelectedFiles");
            toolStripButtonMove.ToolTipText = LanguageManager.GetString("toolTip_buttonMove");
            toolStripButtonSelectBy.ToolTipText = LanguageManager.GetString("toolTip_buttonSelectBy");
            toolStripButtonSettings.ToolTipText = LanguageManager.GetString("toolTip_buttonSettings");
            toolStripButtonRefresh.ToolTipText = LanguageManager.GetString("toolTip_buttonRefresh");
            _ttMainForm.SetToolTip(buttonAddDirectory, LanguageManager.GetString("toolTip_buttonAddDirectory"));
            _ttMainForm.SetToolTip(comboBoxIncludeExtension, LanguageManager.GetString("toolTip_comboBoxIncludeExtension"));
            _ttMainForm.SetToolTip(comboBoxExcludeExtension, LanguageManager.GetString("toolTip_comboBoxExcludeExtension"));


            toolStripMenuItem_Current.Text = _settings.Fields.LastJob;
            Text = string.Format("{0} - {1}", AssemblyHelper.AssemblyTitle, _settings.Fields.LastJob);

            Set_ListViewItemDupl(lvDuplicates);
            Set_ListViewItemDirectorySearch(lvDirectorySearch);

            Load_listDirectorySearch(_settings.Fields.LastJob);
            Load_listDirectorySkipped(_settings.Fields.LastJob);

            //System.Diagnostics.Debug.WriteLine("Form1_Load _dbManager.Active=" + _dbManager.Active);
            _fFunctions.settings = _settings;
            //fFunctions.dbManager = _dbManager;
            //  событие                     подписчик   экземпляр делегата
            //DCSearch.FolderChangedEvent += new EventHandler(DCSearch_FolderChanged);
            //          event                   delegate FileCountAvailableDelegate(double Number)  private void FileCountCompleteEventHandler(double Number)
            //public delegate void FileCheckInProgressDelegate(string fileNameOfListDupl, int currentCount);
            //public event FileCheckInProgressDelegate FileCheckInProgressEvent;
            //FileCheckInProgressEvent(efiGroup.fileInfo.FullName, currentFileCount);
            //private delegate void FileCheckUpdateDelegate(string fileNameOfListDupl, int currentCount);
            //private void FileUpdateEventHandler(string fileNameOfListDupl, int currentCount);
            //fFunctions.FileCheckInProgressEvent += new FileFunctions.FileCheckInProgressDelegate(FileUpdateEventHandler);
            //событие вызывающего += new делегат вызывающего(собыите принимающего)
            _fFunctions.FolderChangedEvent += new FileFunctions.FolderChangedDelegate(FolderChangedEventHandler);
            _fFunctions.FileCountAvailableEvent += new FileFunctions.FileCountAvailableDelegate(FileCountCompleteEventHandler);
            _fFunctions.FileListAvailableEvent += new FileFunctions.FileListAvailableDelegate(CompleteFileListAvailableEventHandler);
            _fFunctions.DuplicateFileListAvailableEvent += new FileFunctions.DuplicateFileListAvailableDelegate(DuplicatFileListAvailableEventHandler);
            _fFunctions.FileCheckInProgressEvent += new FileFunctions.FileCheckInProgressDelegate(FileUpdateEventHandler);
            _fFunctions.SearchCancelledEvent += new FileFunctions.SearchCancelledDelegate(SearchCancelledEventHandler);
            _undoRedoEngine.OnActoinAppledEvent += new UndoRedoEngine.ActoinAppledHandler(OnAction);
        }

        private void InitializePreviewBox()
        {
            // 
            // pictureBoxPrev
            // 
            //pictureBoxPrev = new FileViewer();
            /*pictureBoxPrev = new System.Windows.Forms.PictureBox();
            this.pictureBoxPrev.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxPrev.Name = "pictureBoxPrev";
            this.pictureBoxPrev.Size = new System.Drawing.Size(153, 160);
            this.pictureBoxPrev.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxPrev.TabIndex = 8;
            this.pictureBoxPrev.TabStop = false;
            this.pictureBoxPrev.DoubleClick += new System.EventHandler(this.pictureBox_DoubleClick);*/
            // 
            // pictureBox1
            // 
            //pictureBox1 = new System.Windows.Forms.PictureBox();
            _pictureBox1 = new FileViewer();
            /*this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pictureBox1.Location = new System.Drawing.Point(1, 168);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(146, 145);
            //this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;*/
            //this.pictureBox1.DoubleClick += new System.EventHandler(this.pictureBox_DoubleClick);
            this._pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Generic_MouseMove);
            _pictureBox1.Dock = DockStyle.Fill;
            //pictureBox1.Parent = this;
            //this.pictureBox1.AutoScroll = true;
            // 
            // pictureBoxNext
            // 
            //pictureBoxNext = new FileViewer();
            /*pictureBoxNext = new System.Windows.Forms.PictureBox();
            this.pictureBoxNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.pictureBoxNext.Location = new System.Drawing.Point(1, 320);
            this.pictureBoxNext.Name = "pictureBoxNext";
            this.pictureBoxNext.Size = new System.Drawing.Size(146, 159);
            this.pictureBoxNext.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxNext.TabIndex = 9;
            this.pictureBoxNext.TabStop = false;
            this.pictureBoxNext.DoubleClick += new System.EventHandler(this.pictureBox_DoubleClick);*/

            //this.splitContainer1.Panel1.Controls.Add(this.pictureBoxPrev);
            this.splitContainer1.Panel1.Controls.Add(this._pictureBox1);
            //this.splitContainer1.Panel1.Controls.Add(this.pictureBoxNext);
            this.splitContainer1.Panel1.Resize += new System.EventHandler(this.splitContainer1_Panel1_Resize);
            //this.splitContainer1.Panel1 = pictureBox1;*/
        }

        private void UpdatePreviewBox()
        {
            splitContainer1.Panel1.Controls.Clear();

            _pictureBox1 = new FileViewer();
            this._pictureBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Generic_MouseMove);

            if (_settings.Fields.ShowNeighboringFiles)
            {
                _pictureBoxPrev = new FileViewer();
                _pictureBoxNext = new FileViewer();
                this.splitContainer1.Panel1.Controls.Add(this._pictureBoxPrev);
                this.splitContainer1.Panel1.Controls.Add(this._pictureBoxNext);
            }
            else
            {
                _pictureBox1.Dock = DockStyle.Fill;
            }
            this.splitContainer1.Panel1.Controls.Add(this._pictureBox1);

            splitContainer1_Panel1_Resize(splitContainer1.Panel1, new EventArgs());
        }

        /// <summary>
        /// Загрузить список директорий поиска
        /// </summary>
        private void Load_listDirectorySearch(string directory)
        {
            int i = 0;

            string filePath = CreatePathForDirectory(ref directory, Const.fileNameDirectorySearch);
            if (File.Exists(filePath))
            {
                try
                {
                    string jsonString = File.ReadAllText(filePath);
                    List<ListViewItemSearchDir> listDir = JsonSerializer.Deserialize<List<ListViewItemSearchDir>>(jsonString)!;
                    lvDirectorySearch.Items.Clear();
                    foreach (ListViewItemSearchDir item in listDir)
                    {
                        try
                        {
                            ListViewItem.ListViewSubItem lvsi;
                            ListViewItem lvi = new ListViewItem();
                            lvi.Text = item.Path;
                            lvi.Tag = item.Path;
                            lvi.Name = "Directory";
                            lvi.Checked = item.IsChecked;

                            lvsi = new ListViewItem.ListViewSubItem();
                            if (item.IsSearchInSubDir)
                            {
                                lvsi.Text = LanguageManager.GetString("Yes");
                                lvsi.Tag = 1;
                            }
                            else
                            {
                                lvsi.Text = LanguageManager.GetString("No");
                                lvsi.Tag = 0;
                            }
                            lvsi.Name = "SubDir";
                            lvi.SubItems.Add(lvsi);
                            lvDirectorySearch.Items.Add(lvi);

                            lvi = null;
                            lvsi = null;

                            i++;
                        }
                        catch (InvalidCastException ice)
                        {
                            //MessageBox.Show(ice.Message);
                            new CrashReport(ice, _settings, lvDirectorySearch).ShowDialog();
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void Load_listDirectorySkipped(string directory)
        {
            CheckedItemList cil;
            int i = 0;

            string filePath = CreatePathForDirectory(ref directory, Const.fileNameDirectorySkipped);
            if (File.Exists(filePath))
            {
                try
                {
                    System.Collections.ArrayList al;
                    string jsonString = File.ReadAllText(filePath);
                    al = JsonSerializer.Deserialize<System.Collections.ArrayList>(jsonString)!;
                    checkedListBoxSkipFolder.Items.Clear();
                    foreach (object o in al)
                    {
                        try
                        {
                            cil = (CheckedItemList)o;
                            checkedListBoxSkipFolder.Items.Add(cil.Directory);
                            checkedListBoxSkipFolder.SetItemChecked(i, cil.IsChecked);
                            i++;
                        }
                        catch (InvalidCastException ice)
                        {
                            MessageBox.Show(ice.Message);
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
                AddDefaultDirectory(checkedListBoxSkipFolder);
        }

        private void AddDefaultDirectory(CheckedListBox listBox)
        {
            listBox.Items.Add(Environment.SystemDirectory);
            listBox.SetItemChecked(0, true);
            listBox.Items.Add(System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            listBox.SetItemChecked(1, true);
        }

        private void Load_ListDuplicate(string directory)
        {
            ulong size = 0;
            string fileNameOfListDupl;
            if (directory == string.Empty)
                fileNameOfListDupl = Path.Combine(Const.defaultDirectory, Const.fileNameListDuplicate);
            else
                fileNameOfListDupl = Path.Combine(directory, Const.fileNameListDuplicate);

            if (File.Exists(fileNameOfListDupl))
            {
                try
                {
                    SetStatusState(StatusState.Duplicate); ;
                    SetStatusDuplicate(LanguageManager.GetString("LoadListLoad") + Const.fileNameListDuplicate);
                    Application.DoEvents();

                    lvDuplicates.BeginUpdate();

                    string jsonString = File.ReadAllText(fileNameOfListDupl);
                    ListViewSave saved = JsonSerializer.Deserialize<ListViewSave>(jsonString)!;

                    if (saved.Items != null)
                    {
                        _undoRedoEngine.ListDuplicates = saved;

                        if (_settings.Fields.IsCheckNonExistentOnLoad)
                        {
                            SetStatusDuplicate(LanguageManager.GetString("LoadListRemove"));
                            Application.DoEvents();
                            _undoRedoEngine.ListDuplicates.RemoveMissingFilesFromList();
                        }

                        lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;

                        if (_settings.Fields.IsAllowDelAllFiles)
                            _undoRedoEngine.ListDuplicates.ColoringOfGroups();
                        else
                            _undoRedoEngine.ListDuplicates.ColoringAllCheckedGroups();
                        SetWidthOfListView();
                        SetStatusDuplicate(_undoRedoEngine.ListDuplicates.Items.Count, size, true);
                        tabControl1.SelectedTab = tabPageDuplicate;

                    }
                    lvDuplicates.EndUpdate();
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message);
                }
                catch (System.Runtime.Serialization.SerializationException ex)
                {
                    MessageBox.Show(ex.Message, fileNameOfListDupl);
                    //new CrashReport(ex, _settings).ShowDialog();
                    try
                    {
                        File.Delete(fileNameOfListDupl);
                    }
                    catch (System.IO.IOException)
                    {
                        /*File.SetAttributes(fileNameOfListDupl, File.GetAttributes(fileNameOfListDupl) & ~(FileAttributes.Hidden |
    FileAttributes.ReadOnly | FileAttributes.System));
                        File.Delete(fileNameOfListDupl);*/
                    }
                }
            }
        }

        /// <summary>
        /// Чтение настроек
        /// </summary>
        private void readSetting()
        {
            _settings.ReadXml();

            try
            {
                //this.WindowState = _settings.Fields.Win_State;
                //this.Left = _settings.Fields.Win_Left;
                //this.Top = _settings.Fields.Win_Top;
                splitContainer1.SplitterDistance = _settings.Fields.SplitDistance;

                menuMain.Font = cmsDuplicates.Font = cmsDirectorySearch.Font = cmsSelectBy.Font = statusStrip1.Font = this.Font = _settings.Fields.ProgramFont.ToFont();
                lvDuplicates.Font = _settings.Fields.ListRowFont.ToFont();
                _fontRegular = new Font(lvDuplicates.Font, FontStyle.Regular);
                _fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
                checkBoxSameName.Checked = _settings.Fields.IsSameFileName;
                comboBoxIncludeExtension.Text = _settings.Fields.IncludePattern;
                comboBoxExcludeExtension.Text = _settings.Fields.ExcludePattern;
                if (_settings.Fields.IsOrientationVert)
                    ChangeOrientation(Orientation.Vertical);
                else
                    ChangeOrientation(Orientation.Horizontal);
                ParseMinMaxSizesToForm(_settings.Fields.limits);
                UpdatePreviewBox();
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings).ShowDialog();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //fFunctions.CancelSearch();
            _fFunctions.FolderChangedEvent -= new FileFunctions.FolderChangedDelegate(FolderChangedEventHandler);
            _fFunctions.FileCountAvailableEvent -= new FileFunctions.FileCountAvailableDelegate(FileCountCompleteEventHandler);
            _fFunctions.FileListAvailableEvent -= new FileFunctions.FileListAvailableDelegate(CompleteFileListAvailableEventHandler);
            _fFunctions.DuplicateFileListAvailableEvent -= new FileFunctions.DuplicateFileListAvailableDelegate(DuplicatFileListAvailableEventHandler);
            _fFunctions.FileCheckInProgressEvent -= new FileFunctions.FileCheckInProgressDelegate(FileUpdateEventHandler);
            _fFunctions.SearchCancelledEvent -= new FileFunctions.SearchCancelledDelegate(SearchCancelledEventHandler);
            _undoRedoEngine.OnActoinAppledEvent -= new UndoRedoEngine.ActoinAppledHandler(OnAction);

            if (_updateChecker != null)
            {
                _updateChecker.VersionChecked -= new VersionManager.UpdateChecker.NewVersionCheckedHandler(versionChecker_NewVersionChecked);
            }

            Save_ListDirectorySearch(_settings.Fields.LastJob);
            Save_ListDirectorySkipped(_settings.Fields.LastJob);

            if (_settings.Fields.IsSaveLoadListDub)
            {
                if (_undoRedoEngine.ListDuplicates.Items.Count > 0)
                    Save_ListDuplicate(_settings.Fields.LastJob);
                else if (File.Exists(Const.fileNameListDuplicate))
                {
                    File.Delete(Const.fileNameListDuplicate);
                }
            }

            writeSetting();
        }

        private void Save_ListDirectorySearch(string directory)
        {
            List<ListViewItemSearchDir> listDir = new List<ListViewItemSearchDir>();

            for (int i = 0; i < lvDirectorySearch.Items.Count; i++)
            {
                ListViewItemSearchDir lvisd = new ListViewItemSearchDir();
                ListViewItem lvi = lvDirectorySearch.Items[i];
                string dir = lvi.Text;
                bool isSubDir = false;
                if (lvi.SubItems["SubDir"].Tag != null)
                {
                    if (Convert.ToInt32(lvi.SubItems["SubDir"].Tag) == 1)
                        isSubDir = true;
                    else if (Convert.ToInt32(lvi.SubItems["SubDir"].Tag) == 0)
                        isSubDir = false;
                }
                else
                    new CrashReport("Save_ListDirectorySearch lv.SubItems[SubDir].Tag == null", _settings, lvDirectorySearch).ShowDialog();

                lvisd.Path = dir;
                lvisd.IsSearchInSubDir = isSubDir;
                lvisd.IsChecked = lvi.Checked;
                listDir.Add(lvisd);
            }

            string filePath = CreatePathForDirectory(ref directory, Const.fileNameDirectorySearch);
            try
            {
                string jsonString = JsonSerializer.Serialize(listDir);
                File.WriteAllText(filePath, jsonString);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Save_ListDirectorySkipped(string directory)
        {
            CheckedItemList cil;
            int i = 0;

            System.Collections.ArrayList al = new System.Collections.ArrayList();

            foreach (object o in checkedListBoxSkipFolder.Items)
            {
                cil = new CheckedItemList(o.ToString(), checkedListBoxSkipFolder.GetItemChecked(i));
                al.Add(cil);
                i++;
                cil = null;
            }

            string filePathOfListSkipped = CreatePathForDirectory(ref directory, Const.fileNameDirectorySkipped);

            try
            {
                string jsonString = JsonSerializer.Serialize(al);
                File.WriteAllText(filePathOfListSkipped, jsonString);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Save listForCompare founded duplicate in file.
        /// </summary>
        private void Save_ListDuplicate(string directory)
        {
            SetStatusDuplicate(LanguageManager.GetString("SaveList"));
            Application.DoEvents();

            try
            {
                string filePathOfListDupl = CreatePathForDirectory(ref directory, Const.fileNameListDuplicate);

                string jsonString = JsonSerializer.Serialize(_undoRedoEngine.ListDuplicates);
                File.WriteAllText(filePathOfListDupl, jsonString);
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings, lvDuplicates).ShowDialog();
            }
        }

        private string CreatePathForDirectory(ref string directory, string fileName)
        {
            string filePath;
            if (directory == string.Empty)
            {
                directory = Path.Combine(Application.StartupPath, Const.defaultDirectory);
                filePath = Path.Combine(directory, fileName);
            }
            else if (!FileUtils.IsDirectory(directory))
            {
                directory = Path.Combine(Application.StartupPath, directory);
                filePath = Path.Combine(directory, fileName);
            }
            else
                filePath = fileName;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            return filePath;
        }

        /// <summary>
        /// Запись настроек
        /// </summary>
        private void writeSetting()
        {
            /*_settings.Fields.Win_State = this.WindowState;
            _settings.Fields.Win_Left = this.Left;
            _settings.Fields.Win_Top = this.Top;*/
            _settings.Fields.SplitDistance = splitContainer1.SplitterDistance;
            _settings.Fields.Column0Width = lvDuplicates.Columns[0].Width;
            _settings.Fields.Column1Width = lvDuplicates.Columns[1].Width;
            _settings.Fields.Column2Width = lvDuplicates.Columns[2].Width;
            _settings.Fields.Column3Width = lvDuplicates.Columns[3].Width;
            _settings.Fields.Column4Width = lvDuplicates.Columns[4].Width;
            _settings.Fields.Column5Width = lvDuplicates.Columns[5].Width;

            _settings.Fields.limits = ParseMinMaxSizesFromForm();
            _settings.Fields.IncludePattern = comboBoxIncludeExtension.Text;
            _settings.Fields.ExcludePattern = comboBoxExcludeExtension.Text;

            _settings.WriteXml();
        }


        #region EventHandler
        /// <summary>
        /// Show current adding folder and count of files
        /// поиск файлов для сравнения (прогресс)
        /// </summary>
        /// <param name="dir">Current directory</param>
        /// <param name="count">Count files in directory</param>
        private delegate void FolderChangedDelegate(int count, string folder);
        private void FolderChangedEventHandler(int count, string folder)
        {
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                object[] eventArgs = { count, folder };
                Invoke(new FolderChangedDelegate(FolderChangedEventHandler), eventArgs);
                return;
            }//*/

            //SetStatus("Adding files: " + dir + " files:" + count);
            //SetStatus(dir + " @ " + count);
            //statusStrip1.Items[0].Text = string.Format("File found: {0}", count);
            //statusStrip1.Items[0].Text = statusStripSearch1 + count.ToString();
            statusStrip1.Items[0].Text = LanguageManager.GetString("statusStripSearch1") + count.ToString();
            statusStrip1.Items[4].Text = folder;
        }

        /// <summary>
        /// Current file being processed 
        /// </summary>
        //delegate in fileFunction public delegate void FileCheckInProgressDelegate(string fileNameOfListDupl, int currentCount);
        private delegate void FileCheckUpdateDelegate(string fileName, int currentCount);
        private void FileUpdateEventHandler(string fileName, int currentCount)
        {
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                object[] eventArgs = { fileName, currentCount };
                Invoke(new FileCheckUpdateDelegate(FileUpdateEventHandler), eventArgs);
                return;
            }

            //SetStatus("Checking file: '" + fileNameOfListDupl + "'");
            statusStrip1.Items[4].Text = LanguageManager.GetString("statusStripSearch5") + fileName;
            progressBar1.Value = currentCount;
            SetVistaProgressValue(Convert.ToUInt64(progressBar1.Value), Convert.ToUInt64(progressBar1.Maximum));

            /*TimeSpan timeDiff = DateTime.UtcNow - _timeStart;
            double speed = currentCount - _lastCount / timeDiff.TotalSeconds;
            double remain = (progressBar1.Maximum - currentCount) / speed;//*/
            TimeSpan timeDiff = DateTime.UtcNow - _timeStart;
            double speed = currentCount / timeDiff.TotalSeconds;
            double remain = (progressBar1.Maximum - currentCount) / speed;
            //if (remain > 0)
            TimeSpan timeRemain = TimeSpan.FromSeconds(remain);
            StringBuilder rem = new StringBuilder();
            rem.AppendFormat("{0}{1}:{2}:{3}.{4}", LanguageManager.GetString("statusStripSearch4"), timeRemain.Hours.ToString("00"), timeRemain.Minutes.ToString("00"), timeRemain.Seconds.ToString("00"), timeRemain.Milliseconds.ToString("000"));
            statusStrip1.Items[3].Text = rem.ToString();
            statusStrip1.Items[0].Text = LanguageManager.GetString("statusStripSearch1") + _fFunctions.TotalFiles.ToString();
            statusStrip1.Items[1].Text = LanguageManager.GetString("statusStripSearch2") + _fFunctions.DuplicateFileCount.ToString();
            //Duplicate group
            statusStrip1.Items[2].Text = LanguageManager.GetString("statusStripSearch3") + _fFunctions.DuplicateFileSize.ToString("###,###,###,###,###");

            /*if (currentCount - _lastCount > 10000)
            {
                _lastCount = currentCount;
                _timeStart = DateTime.UtcNow;
            }//*/

            /*var timer = Stopwatch.StartNew();
            SomeCodeToTime();
            timer.Stop();
            Console.WriteLine("Выполнение метода заняло {0} мс", timer.ElapsedMilliseconds);*/
        }

        /// <summary>
        /// All of the files have been counted.
        /// </summary>
        /// <param name="Count">Number of files to be processed.</param>
        private delegate void FileCountCompleteDelegate(int Count);
        private void FileCountCompleteEventHandler(int Count)
        {
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                object[] eventArgs = { Count };
                Invoke(new FileCountCompleteDelegate(FileCountCompleteEventHandler), eventArgs);
                return;
            }

            progressBar1.Maximum = Count;
            //System.Diagnostics.Debug.WriteLine("Maximum=" + Count);

            //SetStatus("Finished Counting Files");
            return;
        }

        /// <summary>
        /// All files have been collected. Add them to the all files listForCompare.
        /// </summary>
        /// <param name="fl">Arraylist collection of files.</param>
        private delegate void CompleteFileListAvailableDelegate(System.Collections.ArrayList fl);
        private void CompleteFileListAvailableEventHandler(System.Collections.ArrayList fl)
        {
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                object[] eventArgs = { fl };
                Invoke(new CompleteFileListAvailableDelegate(CompleteFileListAvailableEventHandler), eventArgs);
                return;
            }

            SetStatusSearch(LanguageManager.GetString("ComputationChecksum"));

            _timeStart = DateTime.UtcNow;
            // _lastCount = 0;
            return;
        }


        /// <summary>
        /// All files have been processed. Put listForCompare in duplicate file listForCompare. Добавление дубликатов в lvDuplicate
        /// </summary>
        /// <param name="duplicateList">Arraylist collection of duplicate files.</param>
        private delegate void DuplicatFileListAvailableDelegate(ArrayList dl);
        private void DuplicatFileListAvailableEventHandler(ArrayList duplicateList)
        {
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                object[] eventArgs = { duplicateList };
                Invoke(new DuplicatFileListAvailableDelegate(DuplicatFileListAvailableEventHandler), eventArgs);
                return;
            }

            if (duplicateList == null)
            {
                SetStatusSearch(LanguageManager.GetString("statusSearch_CompletedJob")); //Completed Job - No files found
                Controls_Enabled(true);
                return;
            }

            //_stateofSearch = StateOfSearch.ShowDuplicate;
            //toolStripButtonCancel.Enabled = false;
            progressBar1.Value = 0;
            SetVistaProgressState(ThumbnailProgressState.NoProgress);

            //заполняем наш лист дубликатов _listDuplicates 
            if (_undoRedoEngine.ListDuplicates != null)
                _undoRedoEngine.ListDuplicates.Clear();

            foreach (ExtendedFileInfo efi in duplicateList)
            {
                _undoRedoEngine.ListDuplicates.Add(efi);
            }

            lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;

            //lvDuplicates.View = View.Details;
            //lvDuplicates.View = View.List;
            //lvDuplicates.CheckBoxes = true;
            //lvDuplicates.OwnerDraw = true;
            //lvDuplicates.FullRowSelect = true;

            _undoRedoEngine.ListDuplicates.Sort(_lvwGroupSorter);
            UpdateColumnSortingIcons();

            _undoRedoEngine.ListDuplicates.ColoringOfGroups();


            SetWidthOfListView();

            SetStatusState(StatusState.Duplicate); ;
            SetStatusDuplicate(_fFunctions.DuplicateFileCount, _fFunctions.DuplicateFileSize, true);

            System.Media.SystemSounds.Beep.Play();  // Beep

            Controls_Enabled(true);
            tabControl1.SelectedTab = tabPageDuplicate;
        }

        private void SetWidthOfListView()
        {
            lvDuplicates.Columns[0].Width = _settings.Fields.Column0Width;
            lvDuplicates.Columns[1].Width = _settings.Fields.Column1Width;
            lvDuplicates.Columns[2].Width = _settings.Fields.Column2Width;
            lvDuplicates.Columns[3].Width = _settings.Fields.Column3Width;
            lvDuplicates.Columns[4].Width = _settings.Fields.Column4Width;
            lvDuplicates.Columns[5].Width = _settings.Fields.Column5Width;
        }

        /// <summary>
        /// All files have been processed. Put listForCompare in duplicate fileOfListDupl listForCompare.
        /// </summary>
        /// <param name="duplicateList">Arraylist collection of duplicate files.</param>
        private delegate void SearchCancelledDelegate();
        private void SearchCancelledEventHandler()
        {
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                Invoke(new SearchCancelledDelegate(SearchCancelledEventHandler));
                return;
            }

            progressBar1.Value = 0;
            SetStatusState(StatusState.Search);
            SetStatusSearch(LanguageManager.GetString("statusSearch_Cancelled"));
            _model.SeacrhState = SeacrhState.ShowDuplicate;
            Controls_Enabled(true);
        }

        /// <summary>
        /// Version Checker Handler for version checks.
        /// </summary>
        /// <param name="newVersion">if set to <c>true</c> [new version].</param>
        /// <param name="versionInfo">The version info.</param>
        void versionChecker_NewVersionChecked(bool newVersion, VersionManager.VersionInfo versionInfo, bool showFormVersion)
        {
            string msg = string.Empty;
            toolStripMenuItem_CheckForUpdate.Enabled = true;

            if (versionInfo != null)
            {
                if (newVersion)
                {
                    //msg = "New version " + versionInfo.VersionAndBuildString() + " released";
                    msg = LanguageManager.GetString("NewVersion") + versionInfo.VersionString() + LanguageManager.GetString("released");
                    toolStripMenuItem_VersionInfo.Text = msg;
                    toolStripMenuItem_VersionInfo.Visible = true;
                    this._versionInfo = versionInfo;

                    if (showFormVersion)
                    {
                        FormUpdate formUpdate = new FormUpdate();
                        formUpdate.Owner = this;
                        formUpdate.StartPosition = FormStartPosition.CenterParent;
                        formUpdate.Font = _settings.Fields.ProgramFont.ToFont();
                        formUpdate.Icon = Properties.Resources.UpdatesIco;
                        formUpdate.Version = LanguageManager.GetString("Version") + versionInfo.VersionString();
                        formUpdate.Changes = versionInfo.Changes;
                        formUpdate.BuildDate = versionInfo.BuildDate();

                        if (formUpdate.ShowDialog() == DialogResult.OK)
                        {
                            System.Diagnostics.Process.Start(versionInfo.DownloadWebPageAddress);
                        }
                        try
                        {
                            formUpdate.Dispose();
                        }
                        catch (Exception ex)
                        {
                            new CrashReport(ex, _settings).ShowDialog();
                        }
                    }
                }
                else
                {
                    if (showFormVersion)
                    {
                        //msg = "You have the current version.";
                        msg = LanguageManager.GetString("isHaveCurrentVersion");
                        MessageBox.Show(msg, "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        //public delegate void UpdateListViewHandler(Font font);
        private void UpdateFontOfListView(Font font)
        {
            //this.SuspendLayout();
            lvDuplicates.Font = font;
            _fontRegular = new Font(lvDuplicates.Font, FontStyle.Regular);
            _fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
            //this.ResumeLayout();
        }
        #endregion


        private void SetVistaProgressState(ThumbnailProgressState thumbnailProgressState)
        {
            if (Environment.OSVersion.Version >= new Version(6, 1))
                _taskbarProgress.SetProgressState(this.Handle, thumbnailProgressState);
        }

        private void SetVistaProgressValue(ulong value, ulong maximum)
        {
            if (Environment.OSVersion.Version >= new Version(6, 1))
                _taskbarProgress.SetProgressValue(this.Handle, value, maximum);
        }

        #region Main Menu
        private void MainMenuItem_Click_Setting(object sender, EventArgs e)
        {
            OpenSettings();
        }

        private void MainMenuItem_CheckForUpdate_Click(object sender, EventArgs e)
        {
            toolStripMenuItem_CheckForUpdate.Enabled = false;

            _updateChecker = new VersionManager.UpdateChecker(true);
            _updateChecker.VersionChecked += new VersionManager.UpdateChecker.NewVersionCheckedHandler(versionChecker_NewVersionChecked);
            //updateChecker.EndVersionChecked += new VersionManager.UpdateChecker.EndVersionCheckedHandler(EndVersionChecked);
        }

        private void MainMenuItem_VersionInfo_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(_versionInfo.DownloadWebPageAddress);
        }

        private void MainMenuItem_Click_Horizontal(object sender, EventArgs e)
        {
            ChangeOrientation(Orientation.Horizontal);
        }

        private void MainMenuItem_Click_Vertical(object sender, EventArgs e)
        {
            ChangeOrientation(Orientation.Vertical);
        }

        private void ChangeOrientation(Orientation orientation)
        {
            if (orientation == Orientation.Horizontal)
            {
                splitContainer1.Orientation = Orientation.Horizontal;
                //statusStripPicture.Dock = DockStyle.Left;
                //toolStripStatusLabel_Width.BorderSides = ToolStripStatusLabelBorderSides.Bottom;
                _settings.Fields.IsOrientationVert = false;
            }
            else if (orientation == Orientation.Vertical)
            {
                splitContainer1.Orientation = Orientation.Vertical;
                //statusStripPicture.Dock = DockStyle.Bottom;
                //toolStripStatusLabel_Width.BorderSides = ToolStripStatusLabelBorderSides.Right;
                _settings.Fields.IsOrientationVert = true;
            }
        }

        private void MainMenuItem_CheckedChanged_FullScreen(object sender, EventArgs e)
        {
            //this.SuspendLayout();
            if (toolStripMenuItem_FullScreen.Checked)
            {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                }

                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                menuMain.Hide();
                //toolStrip1.Hide();
                //splitContainer1.Dock = DockStyle.Fill;

                /*tsmiDockLeft.Enabled = tsmiDockRight.Enabled = false;
                tsmiDockBottom.Enabled = tsmiDockTop.Enabled = false;
                tsmiResetDock.Enabled = false;*/
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                menuMain.Show();
                //toolStrip1.Show();
                //splitContainer1.Dock = DockStyle.None;

                /*tsmiDockLeft.Enabled = tsmiDockTop.Enabled = true;
                tsmiDockRight.Enabled = tsmiDockBottom.Enabled = true;
                tsmiResetDock.Enabled = true;*/
            }

            //this.ResumeLayout();
        }

        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {//CancelButton перехватывает
            //MessageBox.Show(e.KeyCode.ToString());
            if (e.KeyCode.Equals(Keys.Escape) && toolStripMenuItem_FullScreen.Checked)
            {
                toolStripMenuItem_FullScreen.Checked = false;
            }
        }

        private void Generic_MouseMove(object sender, MouseEventArgs e)
        {
            if (toolStripMenuItem_FullScreen.Checked)
            {
                if ((sender != splitContainer1.Panel2) || ((sender == splitContainer1.Panel2) && (splitContainer1.Orientation == Orientation.Vertical)))
                    //menuMain.Visible = (e.Y < SHOW_SENSITIVITY);
                    menuMain.Visible = (e.Location.Y < SHOW_SENSITIVITY);
                //menuMain.Visible = (sender.Location.Y < SHOW_SENSITIVITY);
                //MessageBox.Show(e.Location.ToString());
            }
        }

        private void toolStripMenuItem_Save_Click(object sender, EventArgs e)
        {
            FormFileNameSelect ffns = new FormFileNameSelect();
            ffns.Owner = this;
            ffns.StartPosition = FormStartPosition.CenterParent;
            ffns.Font = _settings.Fields.ProgramFont.ToFont();
            ffns.Icon = Properties.Resources.TerminatorIco32;
            if (lvDirectorySearch.Items.Count == 1)
                ffns.SelectedName = (new DirectoryInfo(lvDirectorySearch.Items[0].Text).Name);

            if (ffns.ShowDialog() == DialogResult.OK)
            {
                string directory;
                //directory = Path.Combine(Application.StartupPath, ffns.SelectedName);
                directory = ffns.SelectedName;

                _settings.Fields.LastJob = directory;
                toolStripMenuItem_Current.Text = directory;
                Text = string.Format("{0} - {1}", AssemblyHelper.AssemblyTitle, directory);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (lvDuplicates.Items.Count > 0)
                    Save_ListDuplicate(directory);
                else if (File.Exists(Const.fileNameListDuplicate))
                {
                    File.Delete(Const.fileNameListDuplicate);
                }

                Save_ListDirectorySearch(ffns.SelectedName);
                Save_ListDirectorySkipped(ffns.SelectedName);
            }
        }

        private void toolStripMenuItem_Load_Click(object sender, EventArgs e)
        {
            FormJobLoad fjl = new FormJobLoad();
            fjl.Owner = this;
            fjl.StartPosition = FormStartPosition.CenterParent;
            fjl.Font = _settings.Fields.ProgramFont.ToFont();
            fjl.Icon = Properties.Resources.TerminatorIco32;
            if (FileUtils.IsDirectory(_settings.Fields.LastJob))
                fjl.SelectedJob = _settings.Fields.LastJob;

            if (fjl.ShowDialog() == DialogResult.OK)
            {
                string job = new DirectoryInfo(fjl.SelectedJob).Name;
                _settings.Fields.LastJob = job;
                Text = string.Format("{0} - {1}", AssemblyHelper.AssemblyTitle, job);
                toolStripMenuItem_Current.Text = job;
                Load_listDirectorySearch(job);
                Load_listDirectorySkipped(job);
                Load_ListDuplicate(job);
            }
        }

        #endregion

        #region ToolStrip

        /// <summary>
        /// Start the duplicate file search.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnStart_Click(object sender, EventArgs e)
        {
            //throw new ApplicationException("Exception");
            //new CrashReport("Test", _settings).ShowDialog();
            //new CrashReport(new Exception()).ShowDialog();
            if (_model.SeacrhState == SeacrhState.Search)
            {
                _model.SeacrhState = SeacrhState.Pause;
                //_fFunctions.PauseSearch();
                _searcher.Pause();

                SetVistaProgressState(ThumbnailProgressState.Paused);
            }
            else if (_model.SeacrhState == SeacrhState.Pause)
            {
                _model.SeacrhState = SeacrhState.Search;
                //_fFunctions.ResumeSearch();
                _searcher.Resume();
                SetVistaProgressState(ThumbnailProgressState.Normal);
            }
            else if (_model.SeacrhState == SeacrhState.ShowDuplicate)
            {
                if (lvDirectorySearch.CheckedIndices.Count == 0)
                {
                    MessageBox.Show(LanguageManager.GetString("S_NotSetSearchDir"), LanguageManager.GetString("S_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _model.SeacrhState = SeacrhState.Search;


                progressBar1.Value = 0;
                SetVistaProgressValue(Convert.ToUInt64(progressBar1.Value), Convert.ToUInt64(progressBar1.Maximum));

                SetStatusState(StatusState.Search);
                //ClearPicrureBox();

                Controls_Enabled(false);

                lvDuplicates.Items.Clear();

                _fFunctions.Clear_Search_Directory();
                _fFunctions.Clear_Skip_Directory();

                //_settings.Fields.SameContent = radioButtonSameContent.Checked;
                //_settings.Fields.DeepSimilarName = uint.Parse(labelSimilarDeepSize.Text);
                _settings.Fields.IncludePattern = comboBoxIncludeExtension.Text;
                _settings.Fields.ExcludePattern = comboBoxExcludeExtension.Text;
                _settings.Fields.limits = ParseMinMaxSizesFromForm();


                ReadOnlyCollection<(string DirectoryPath, bool SearchInSubdirectory)> directories = GetDirectorySearch();
           

                SetStatusSearch(LanguageManager.GetString("RetrievingStructure"));

                //_fFunctions.BeginSearch();

                var progressForm = new ProgressForm();
                //IProgress<ProgressDto> progress = new Progress<ProgressDto>(progressModel =>
                //{
                //    if (!progressForm.IsDisposed) // don't attempt to use disposed form
                //        progressForm.UpdateProgress(progressModel);
                //});

                IProgress<ProgressDto> progress = new ProgressWithTimer<ProgressDto>(TimeSpan.FromMilliseconds(100), progressModel =>
                {
                    if (!progressForm.IsDisposed) // don't attempt to use disposed form
                        progressForm.UpdateProgress(progressModel);
                });

                _searcher = new Searcher(
                    directories,
                    null,
                    new SearchSetting(),
                    _dbManager,
                    new WindowsUtil(),
                    progress,
                    _archiveService);

                progressForm.Cancelled += (s, e) => _searcher.Cancell();
                var progressFormTask = progressForm.ShowDialogAsync();

                try
                {
                    await _searcher.Start();
                }
                finally
                {
                    if (!progressForm.IsDisposed)
                        progressForm.Close();
                    await progressFormTask;
                }

                //результаты возврашает через событие DuplicateFileListAvailableDelegate DuplicatFileListAvailableEventHandler(System.Collections.ArrayList duplicateList)

                SearchEnded(_searcher);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(_model.SeacrhState));
            }
        }

        private void SearchEnded(Searcher searcher)
        {
            if (searcher.Duplicates == null || searcher.Duplicates != null && searcher.Duplicates.Count == 0)
            {
                SetStatusSearch(LanguageManager.GetString("statusSearch_CompletedJob")); //Completed Job - No files found
                Controls_Enabled(true);
                return;
            }

            //_stateofSearch = StateOfSearch.ShowDuplicate;
            //toolStripButtonCancel.Enabled = false;
            progressBar1.Value = 0;
            SetVistaProgressState(ThumbnailProgressState.NoProgress);

            
            //заполняем наш лист дубликатов _listDuplicates 
            if (_undoRedoEngine.ListDuplicates != null)
                _undoRedoEngine.ListDuplicates.Clear();

            //foreach (ExtendedFileInfo efi in duplicateList)
            //{
            //    _undoRedoEngine.ListDuplicates.Add(efi);
            //}

            //lvDuplicates.VirtualListSize = searcher.Duplicates.Count;
            //lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;

            //lvDuplicates.View = View.Details;
            //lvDuplicates.View = View.List;
            //lvDuplicates.CheckBoxes = true;
            //lvDuplicates.OwnerDraw = true;
            //lvDuplicates.FullRowSelect = true;

            _undoRedoEngine.ListDuplicates.Sort(_lvwGroupSorter);
            UpdateColumnSortingIcons();

            _undoRedoEngine.ListDuplicates.ColoringOfGroups();


            var duplicates = searcher.Duplicates.SelectMany(d => d.Files).Select(f => new FileViewModel(f)).ToList();
            //for (int i = 0; i < 300000; i++)
            //{
            //    //duplicates.Add(new FileViewModel(new ExtendedFileInfo(new FileInfo("c:\\DumpStack.log"))));
            //    duplicates.Add(new FileViewModel("c:\\DumpStack.log", 54161));
            //}
            _model.Dublicates = duplicates;

            lvDuplicates.VirtualListSize = _model.Dublicates.Count;

            SetWidthOfListView();

            SetStatusState(StatusState.Duplicate); ;
            SetStatusDuplicate(_fFunctions.DuplicateFileCount, _fFunctions.DuplicateFileSize, true);

            System.Media.SystemSounds.Beep.Play();  // Beep

            Controls_Enabled(true);
            tabControl1.SelectedTab = tabPageDuplicate;
        }

        private ReadOnlyCollection<(string DirectoryPath, bool SearchInSubdirectory)> GetDirectorySearch()
        {
            Collection<(string DirectoryPath, bool SearchInSubdirectory)> collection = new Collection<(string DirectoryPath, bool SearchInSubdirectory)>();
            for (int i = 0; i < lvDirectorySearch.CheckedItems.Count; i++)
            {
                ListViewItem lvi;
                lvi = lvDirectorySearch.CheckedItems[i];
                string directoryPath = lvi.Text;
                bool isSubDir = false;
                if (lvi.SubItems["SubDir"].Text == LanguageManager.GetString("Yes"))
                    isSubDir = true;
                else if (lvi.SubItems["SubDir"].Text == LanguageManager.GetString("No"))
                    isSubDir = false;
                else
                    throw new ArgumentException("lvDirectorySearch.SubItems[SubDir] not equal Yes or No");

                //System.Diagnostics.Debug.WriteLine("Add directory in fFunctions() " + dir + ", subdir=" + isSubDir);
                //_fFunctions.Add_Search_Directory(dir, isSubDir);
                collection.Add((directoryPath, isSubDir));
            }

            CheckedListBox.CheckedItemCollection cicSkip = checkedListBoxSkipFolder.CheckedItems;
            if (cicSkip.Count > 0)
            {
                foreach (string str in cicSkip)
                {
                    //_fFunctions.Add_Skip_Directory(str);
                }
            }

            return new ReadOnlyCollection<(string DirectoryPath, bool SearchInSubdirectory)>(collection);
        }

        /*private void ClearPicrureBox()
        {
            //pictureBox1.Image = null;
            if (pictureBoxPrev.Image != null)
            {
                pictureBoxPrev.Image.Dispose();
                pictureBoxPrev.Image = null;
            }
            if (pictureBoxNext.Image != null)
            {
                pictureBoxNext.Image.Dispose();
                pictureBoxNext.Image = null;
            }
        }*/

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            //if (_model.SeacrhState == SeacrhState.Pause)
            //{
            //    _fFunctions.ResumeSearch();
            //}
            //_fFunctions.CancelSearch();

            _searcher?.Cancell();
            _model.SeacrhState = SeacrhState.ShowDuplicate;
            progressBar1.Value = 0;
            SetVistaProgressState(ThumbnailProgressState.NoProgress);
        }

        private void OnAction()
        {
            toolStripButtonUndo.Enabled = _undoRedoEngine.UndoEnable();
            toolStripButtonRedo.Enabled = _undoRedoEngine.RedoEnable();
        }

        private void buttonUndo_Click(object sender, EventArgs e)
        {
            if (_undoRedoEngine.Undo())
            {
                lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;
                lvDuplicates.Invalidate();
            }
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            if (_undoRedoEngine.Redo())
            {
                lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;
                lvDuplicates.Invalidate();
            }
        }

        private void toolStripButtonSettings_Click(object sender, EventArgs e)
        {
            OpenSettings();
        }

        private void buttonSelectBy_Click(object sender, EventArgs e)
        {
            cmsSelectBy.Show(toolStripButtonSelectBy.Owner, toolStripButtonSelectBy.Bounds.X + 5, toolStripButtonSelectBy.Bounds.Y + 5);
        }

        private void buttonMove_Click(object sender, EventArgs e)
        {
            MoveToFolder();
        }

        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            RemoveMissingFilesFromList();
        }

        #endregion

        private void OpenSettings()
        {
            FormSetting fs = _serviceProvider.GetRequiredService<FormSetting>();
            fs.Owner = this;
            fs.StartPosition = FormStartPosition.CenterParent;
            fs.Font = _settings.Fields.ProgramFont.ToFont();
            fs.Icon = Properties.Resources.SettingIco;

            fs.UpdateListView += new FormSetting.UpdateListViewHandler(UpdateFontOfListView);
            fs.UpdatePreview += new FormSetting.UpdatePreviewHandler(UpdatePreviewBox);

            if (fs.ShowDialog() == DialogResult.OK)
            {
                _settings.WriteXml();
            }
            try
            {
                fs.UpdateListView -= new FormSetting.UpdateListViewHandler(UpdateFontOfListView);
                fs.UpdatePreview -= new FormSetting.UpdatePreviewHandler(UpdatePreviewBox);
                fs.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #region Main Form

        private void buttonSelectInclude_Click(object sender, EventArgs e)
        {
            FormFileFilterSelect fs = new FormFileFilterSelect();
            fs.Owner = this;
            fs.StartPosition = FormStartPosition.CenterParent;
            fs.Font = _settings.Fields.ProgramFont.ToFont();
            fs.Icon = Properties.Resources.TerminatorIco32;

            if (fs.ShowDialog() == DialogResult.OK)
            {
                comboBoxIncludeExtension.Text = _settings.Fields.IncludePattern = fs.GetSelectedExtension;
            }
            try
            {
                fs.Dispose();
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings).ShowDialog();
            }
        }

        private void buttonSelectExclude_Click(object sender, EventArgs e)
        {
            FormFileFilterSelect fs = new FormFileFilterSelect();
            fs.Owner = this;
            fs.StartPosition = FormStartPosition.CenterParent;
            fs.Font = _settings.Fields.ProgramFont.ToFont();
            fs.Icon = Properties.Resources.TerminatorIco32;

            if (fs.ShowDialog() == DialogResult.OK)
            {
                comboBoxExcludeExtension.Text = _settings.Fields.ExcludePattern = fs.GetSelectedExtension;
            }
            try
            {
                fs.Dispose();
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings).ShowDialog();
            }
        }

        private void checkBoxSameName_CheckedChanged(object sender, EventArgs e)
        {
            _settings.Fields.IsSameFileName = checkBoxSameName.Checked;
        }

        private long[] ParseMinMaxSizesFromForm()
        {
            long min = 0,
                 max = long.MaxValue;
            int multiL = 1,
                multiM = 1;

            if (nmMin.Value != 0)
            {
                if (rdbLBytes.Checked)
                    multiL = 1;
                else if (rdbLKilo.Checked)
                    multiL = 1024;
                else if (rdbLMega.Checked)
                    multiL = 1024 * 1024;
                else
                {
                    multiL = 1024 * 1024 * 1024;
                }
                min = (long)nmMin.Value * multiL;
            }

            if (nmMax.Value != 0)
            {
                if (rdbMBytes.Checked)
                    multiM = 1;
                else if (rdbMKilo.Checked)
                    multiM = 1024;
                else if (rdbMMega.Checked)
                    multiM = 1024 * 1024;
                else
                {
                    multiM = 1024 * 1024 * 1024;
                }
                max = (long)nmMax.Value * multiM;
            }
            return new long[] { min, max };
        }

        private void ParseMinMaxSizesToForm(long[] limits)
        {
            long min = limits[0],
                 max = limits[1];

            if (min != 0)
            {
                if (min < 1024)
                {
                    nmMin.Value = min;
                    rdbLBytes.Checked = true;
                }
                else if (min < 1024 * 1024)
                {
                    nmMin.Value = min / 1024;
                    rdbLKilo.Checked = true;
                }
                else if (min < 1024 * 1024 * 1024)
                {
                    nmMin.Value = min / (1024 * 1024);
                    rdbLMega.Checked = true;
                }
                else
                {
                    nmMin.Value = min / (1024 * 1024 * 1024);
                    rdbLGiga.Checked = true;
                }
            }
            if (max != long.MaxValue)
            {
                if (max < 1024)
                {
                    nmMin.Value = max;
                    rdbMBytes.Checked = true;
                }
                else if (max < 1024 * 1024)
                {
                    nmMin.Value = max / 1024;
                    rdbMKilo.Checked = true;
                }
                else if (max < 1024 * 1024 * 1024)
                {
                    nmMin.Value = max / (1024 * 1024);
                    rdbMMega.Checked = true;
                }
                else
                {
                    nmMin.Value = max / (1024 * 1024 * 1024);
                    rdbMGiga.Checked = true;
                }
            }
        }

        private void buttonDel_Click(object sender, EventArgs e)
        {
            DeleteDirectory(true);
        }

        private void DeleteDirectory(bool FocusOnList)
        {
            if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSearchFolder"])
            {
                if (lvDirectorySearch.FocusedItem != null)
                    lvDirectorySearch.FocusedItem.Remove();
            }
            else if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSkipFolder"])
            {
                int index = checkedListBoxSkipFolder.Items.IndexOf(checkedListBoxSkipFolder.SelectedItem);
                checkedListBoxSkipFolder.Items.Remove(checkedListBoxSkipFolder.SelectedItem);
                //Фокусировка на месте удаленного элемента
                if (checkedListBoxSkipFolder.Items.Count > 0)
                {
                    if (index <= checkedListBoxSkipFolder.Items.Count - 1)
                    {
                        checkedListBoxSkipFolder.SelectedIndices.Add(index);
                        //checkedListDirectorySearch.F= checkedListDirectorySearch.Items[indexOfGroupWithAllChecked];
                    }
                    else
                    {
                        checkedListBoxSkipFolder.SelectedIndices.Add(checkedListBoxSkipFolder.Items.Count - 1);
                    }
                    if (FocusOnList)
                        checkedListBoxSkipFolder.Focus();
                }
            }
        }

        private void buttonClearAll_Click(object sender, EventArgs e)
        {
            if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSearchFolder"])
            {
                lvDirectorySearch.Items.Clear();
            }
            else if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSkipFolder"])
            {
                checkedListBoxSkipFolder.Items.Clear();
            }
        }

        private void buttonClearNonExistent_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < lvDirectorySearch.Items.Count; i++)
            {
                if (!Directory.Exists(lvDirectorySearch.Items[i].Text))
                {
                    lvDirectorySearch.Items.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < checkedListBoxSkipFolder.Items.Count; i++)
            {
                if (!Directory.Exists(checkedListBoxSkipFolder.Items[i].ToString()))
                {
                    checkedListBoxSkipFolder.Items.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Allow user to add new search directory to directory listForCompare.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAddDirectory_Click(object sender, EventArgs e)
        {
            AddDirectory();
        }

        private void AddDirectory()
        {
            FormFolderSelect ffs = new FormFolderSelect();
            ffs.Owner = this;
            ffs.StartPosition = FormStartPosition.CenterParent;
            ffs.Font = _settings.Fields.ProgramFont.ToFont();
            ffs.Icon = Properties.Resources.TerminatorIco32;
            ffs.IsSubDir = true;

            //ffs.StartPosition = FormStartPosition.CenterParent;
            if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSearchFolder"])
            {
                if (lvDirectorySearch.FocusedItem != null)
                    ffs.SelectedPath = lvDirectorySearch.FocusedItem.Text;
            }
            else if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSkipFolder"])
            {
                if (checkedListBoxSkipFolder.SelectedIndices.Count > 0)
                    ffs.SelectedPath = Convert.ToString(checkedListBoxSkipFolder.SelectedItem);
            }

            if (ffs.ShowDialog() == DialogResult.OK)
            {
                if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSearchFolder"])
                {
                    if (AddFolderEvent != null)
                        AddFolderEvent(this, new AddFolderEventArgs(new DuplicateDirectory(ffs.SelectedPath, ffs.IsSubDir, TypeFolder.Search, true)));
                    //AddToSearchFolders(ffs.SelectedPath, ffs.IsSubDir);
                }
                else if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSkipFolder"])
                {
                    //AddToSkipFolders(ffs.SelectedPath);
                    if (AddFolderEvent != null)
                        AddFolderEvent(this, new AddFolderEventArgs(new DuplicateDirectory(ffs.SelectedPath, TypeFolder.Skip)));
                }
            }

            try
            {
                ffs.Dispose();
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings).ShowDialog();
            }
        }

        public void AddToSearchFolders(DuplicateDirectory directory)
        {
            ListViewItem.ListViewSubItem lvsi;
            ListViewItem lvi = new ListViewItem();
            lvi.Text = directory.Path;
            lvi.Tag = directory;
            lvi.Name = "Directory";
            lvi.Checked = directory.Checked;

            lvsi = new ListViewItem.ListViewSubItem();
            if (directory.IsSubDir)
            {
                lvsi.Text = LanguageManager.GetString("Yes");
                lvsi.Tag = 1;
            }
            else
            {
                lvsi.Text = LanguageManager.GetString("No");
                lvsi.Tag = 0;
            }
            lvsi.Name = "SubDir";
            lvi.SubItems.Add(lvsi);

            lvDirectorySearch.Items.Add(lvi);

            lvi = null;
            lvsi = null;
        }

        /*private void AddToSearchFolders(string directory, bool isSubDir)
        {
            int index = -1;
            if (!ListViewContainPath(lvDirectorySearch, directory, ref index))
            {
                //checkedListDirectorySearch.Items.Add(ffs.SelectedPath, true);
                //Фокусировка на новом элементе
                //checkedListDirectorySearch.SelectedIndices.Add(checkedListDirectorySearch.Items.Count - 1);

                ListViewItem.ListViewSubItem lvsi;
                ListViewItem lvi = new ListViewItem();
                lvi.Text = directory;
                lvi.Tag = directory;
                lvi.Name = "Directory";
                lvi.Checked = true;

                lvsi = new ListViewItem.ListViewSubItem();
                if (isSubDir)
                {
                    lvsi.Text = LanguageManager.GetString("Yes");
                    lvsi.Tag = 1;
                }
                else
                {
                    lvsi.Text = LanguageManager.GetString("No");
                    lvsi.Tag = 0;
                }
                lvsi.Name = "SubDir";
                lvi.SubItems.Add(lvsi);

                lvDirectorySearch.Items.Add(lvi);

                lvi = null;
                lvsi = null;
            }
            else if (!lvDirectorySearch.Items[index].Checked)
                lvDirectorySearch.Items[index].Checked = true;
            else
                MessageBox.Show(LanguageManager.GetString("Directory") + directory + Environment.NewLine + LanguageManager.GetString("AlreadyInList"));
        }*/

        private void AddToSkipFolders(string directory)
        {
            int index = checkedListBoxSkipFolder.FindStringExact(directory);
            if (index < 0)
            {
                checkedListBoxSkipFolder.Items.Add(directory, true);
                //Фокусировка на новом элементе
                checkedListBoxSkipFolder.SelectedIndices.Add(checkedListBoxSkipFolder.Items.Count - 1);
            }
            else if (!checkedListBoxSkipFolder.GetItemChecked(index))
                checkedListBoxSkipFolder.SetItemChecked(index, true);
            else
                MessageBox.Show(LanguageManager.GetString("Directory") + directory + Environment.NewLine + LanguageManager.GetString("AlreadyInList"));
        }

        private bool ListViewContainPath(ListView lvi, string path, ref int index)
        {
            bool containPath = false;
            for (int i = 0; i < lvi.Items.Count; i++)
            {
                if (String.Equals(lvi.Items[i].Text, path, StringComparison.CurrentCultureIgnoreCase))
                {
                    index = i;
                    containPath = true;
                    break;
                }
            }
            return containPath;
        }

        private void buttonEdit_Click(object sender, EventArgs e)
        {
            EditDirectory();
        }

        private void EditDirectory()
        {
            FormFolderSelect ffs = new FormFolderSelect();
            ffs.Owner = this;
            ffs.StartPosition = FormStartPosition.CenterParent;
            ffs.Font = _settings.Fields.ProgramFont.ToFont();
            ffs.Icon = Properties.Resources.TerminatorIco32;

            //ffs.StartPosition = FormStartPosition.CenterParent;
            if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSearchFolder"])
            {
                //if (checkedListDirectorySearch.SelectedIndices.Count > 0)
                //ffs.SelectedPath = Convert.ToString(checkedListDirectorySearch.SelectedItem);
                if (lvDirectorySearch.FocusedItem != null)
                {
                    ffs.SelectedPath = lvDirectorySearch.FocusedItem.Text;
                    if (lvDirectorySearch.FocusedItem.SubItems[1].Text == LanguageManager.GetString("Yes"))
                        ffs.IsSubDir = true;
                    else
                        ffs.IsSubDir = false;
                }
            }
            else if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSkipFolder"])
            {
                if (checkedListBoxSkipFolder.SelectedIndices.Count > 0)
                    ffs.SelectedPath = checkedListBoxSkipFolder.SelectedItem.ToString();
            }

            if (ffs.ShowDialog() == DialogResult.OK)
            {
                if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSearchFolder"])
                {
                    if (lvDirectorySearch.FocusedItem != null)
                    {
                        lvDirectorySearch.FocusedItem.Text = ffs.SelectedPath;
                        lvDirectorySearch.FocusedItem.Tag = ffs.SelectedPath;
                        if (ffs.IsSubDir)
                        {
                            lvDirectorySearch.FocusedItem.SubItems[1].Text = LanguageManager.GetString("Yes");
                            lvDirectorySearch.FocusedItem.SubItems[1].Tag = 1;
                        }
                        else
                        {
                            lvDirectorySearch.FocusedItem.SubItems[1].Text = LanguageManager.GetString("No");
                            lvDirectorySearch.FocusedItem.SubItems[1].Tag = 0;
                        }
                    }
                    //else
                    //    MessageBox.Show(LanguageManager.GetString("Directory") + ffs.SelectedPath + Environment.NewLine + LanguageManager.GetString("AlreadyInList"));
                }
                else if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSkipFolder"])
                {
                    if (!(checkedListBoxSkipFolder.FindStringExact(ffs.SelectedPath) >= 0))
                    {
                        checkedListBoxSkipFolder.Items.Add(ffs.SelectedPath, true);
                        //Фокусировка на новом элементе
                        checkedListBoxSkipFolder.SelectedIndices.Add(checkedListBoxSkipFolder.Items.Count - 1);
                    }
                    else
                        MessageBox.Show(LanguageManager.GetString("Directory") + ffs.SelectedPath + Environment.NewLine + LanguageManager.GetString("AlreadyInList"));
                }
            }

            try
            {
                ffs.Dispose();
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings, lvDuplicates).ShowDialog();
            }
        }

        private void lvDirectorySearch_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void lvDirectorySearch_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop, false);

                if (filenames != null)
                {
                    if (filenames.Length > 0)
                    {
                        foreach (string pathCycle in filenames)
                        {
                            string path;
                            int lastIndex = pathCycle.LastIndexOf('\\');
                            if (lastIndex != pathCycle.Length - 1)
                                path = pathCycle + '\\';
                            else
                                path = pathCycle;

                            //AddToSearchFolders(path, true);
                            AddToSearchFolders(new DuplicateDirectory(path, true, TypeFolder.Search, true));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings, lvDirectorySearch).ShowDialog();
            }
        }

        private void checkedListBoxSkipFolder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void checkedListBoxSkipFolder_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] filenames = (string[])e.Data.GetData(DataFormats.FileDrop, false);

                if (filenames != null)
                {
                    if (filenames.Length > 0)
                    {
                        foreach (string path in filenames)
                        {
                            if (!(checkedListBoxSkipFolder.FindStringExact(path) >= 0))
                            {
                                checkedListBoxSkipFolder.Items.Add(path, true);
                            }
                            else
                                MessageBox.Show(LanguageManager.GetString("Directory") + path + Environment.NewLine + LanguageManager.GetString("AlreadyInList"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings).ShowDialog();
            }
        }

        private void lvDirectorySearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (lvDirectorySearch.FocusedItem != null)
            {
                if (e.KeyCode == Keys.Delete)
                {
                    lvDirectorySearch.FocusedItem.Remove();
                }
            }
        }

        private void splitContainer1_Panel1_Resize(object sender, EventArgs e)
        {
            base.OnResize(e);
            SplitterPanel panel = sender as SplitterPanel;
            if (panel != null)
            {
                if (splitContainer1.Orientation == Orientation.Vertical)
                {
                    _pictureBox1.Width = panel.Width;
                    _pictureBox1.Height = panel.Height / 3 - MARGIN_BETWEEN_PB * 2;
                    if (_pictureBoxPrev != null && _pictureBoxNext != null)
                    {
                        _pictureBoxPrev.Width = _pictureBoxNext.Width = panel.Width;
                        _pictureBoxPrev.Height = _pictureBoxNext.Height = _pictureBox1.Height;
                        _pictureBoxPrev.Location = new Point(0, 0);
                        _pictureBox1.Location = new Point(0, _pictureBoxPrev.Location.Y + _pictureBoxPrev.Height + MARGIN_BETWEEN_PB);
                        _pictureBoxNext.Location = new Point(0, _pictureBox1.Location.Y + _pictureBox1.Height + MARGIN_BETWEEN_PB);
                    }
                }
                else if (splitContainer1.Orientation == Orientation.Horizontal)
                {
                    _pictureBox1.Height = panel.Height;
                    _pictureBox1.Width = panel.Width / 3 - MARGIN_BETWEEN_PB * 2;
                    _pictureBox1.Location = new Point(_pictureBoxPrev.Location.X + _pictureBoxPrev.Width + MARGIN_BETWEEN_PB, 0);
                    if (_pictureBoxPrev != null && _pictureBoxNext != null)
                    {
                        _pictureBoxPrev.Height = _pictureBoxNext.Height = panel.Height;
                        _pictureBoxPrev.Width = _pictureBoxNext.Width = _pictureBox1.Width;
                        _pictureBoxPrev.Location = new Point(0, 0);
                        _pictureBoxNext.Location = new Point(_pictureBox1.Location.X + _pictureBox1.Width + MARGIN_BETWEEN_PB, 0);
                    }
                }
            }
        }

        /*private void pictureBox_DoubleClick(object sender, EventArgs e)
        {
            PictureBox pic = sender as PictureBox;
            string path = pic.Tag as string;
            try
            {
                if (System.IO.File.Exists(path))
                    System.Diagnostics.Process.Start(path);
            }
            catch (System.ArgumentException ae)
            {
                MessageBox.Show(ae.Message);
            }
        }*/

        private void FormMain_FontChanged(object sender, EventArgs e)
        {
            menuMain.Font = cmsDuplicates.Font = cmsDirectorySearch.Font = cmsSelectBy.Font = statusStrip1.Font = this.Font;
        }

        #endregion

        #region Helper Functions
        /// <summary>
        /// Enable/Disable the controls that can not be used while the 
        /// search is in progress.
        /// </summary>
        /// <param name="enable"></param>
        private void Controls_Enabled(bool enable)
        {
            //btnStart.Enabled = enable;
            //toolStripButtonCancel.Enabled = !enable;
            lvDirectorySearch.Enabled = enable;
            checkedListBoxSkipFolder.Enabled = enable;
            lvDuplicates.Enabled = enable;
            buttonAddDirectory.Enabled = enable;
            buttonEdit.Enabled = enable;
            buttonDel.Enabled = enable;
            buttonClearAll.Enabled = enable;
            buttonClearNonExistent.Enabled = enable;

            groupBoxLessThan.Enabled = enable;
            groupBoxMoreThan.Enabled = enable;
            checkBoxSameName.Enabled = enable;
            groupBoxFileFilter.Enabled = enable;

            toolStripMenuItem_Settings.Enabled = enable;
            toolStripMenuItem_Save.Enabled = enable;
            toolStripMenuItem_Load.Enabled = enable;
        }

        /// <summary>
        /// Initialize the listview duplicate
        /// </summary>
        /// <param name="lv"></param>
        private void Set_ListViewItemDupl(ListView lv)
        {
            Debug.WriteLine(CultureInfo.CurrentCulture);
            Debug.WriteLine(CultureInfo.CurrentUICulture);

            ColumnHeader colHead = new ColumnHeader();
            colHead.Text = _stringLocalizer["File Name"];
            //colHead.Text = LanguageManager.GetString("ListViewColumn_FileName");
            lv.Columns.Add(colHead);

            colHead = new ColumnHeader();
            colHead.Text = LanguageManager.GetString("ListViewColumn_Path");
            lv.Columns.Add(colHead);

            colHead = new ColumnHeader();
            colHead.Text = LanguageManager.GetString("ListViewColumn_Size");
            lv.Columns.Add(colHead);

            colHead = new ColumnHeader();
            colHead.Text = LanguageManager.GetString("ListViewColumn_FileType");
            lv.Columns.Add(colHead);

            colHead = new ColumnHeader();
            colHead.Text = LanguageManager.GetString("ListViewColumn_LastAccessed");
            lv.Columns.Add(colHead);

            colHead = new ColumnHeader();
            colHead.Text = LanguageManager.GetString("ListViewColumn_MD5Checksum");
            lv.Columns.Add(colHead);

            lv.View = System.Windows.Forms.View.Details;
        }

        private void Set_ListViewItemDirectorySearch(ListView lv)
        {
            ColumnHeader colHead;

            colHead = new ColumnHeader();
            colHead.Text = LanguageManager.GetString("ListViewColumn_Path");
            //colHead.Text = LanguageManager.GetString("ListViewColumn_Path");
            lv.Columns.Add(colHead);
            lv.Columns[0].Width = 400;

            colHead = new ColumnHeader();
            colHead.Text = LanguageManager.GetString("ListViewColumn_SubDirectory");
            //colHead.Text = LanguageManager.GetString("ListViewColumn_FileName");
            lv.Columns.Add(colHead);
            lv.Columns[1].Width = 100;

            lv.View = System.Windows.Forms.View.Details;
        }

        /*private string SpaceThousands(long size)
        {
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
            nfi.NumberGroupSeparator = " ";
            nfi.NumberDecimalDigits = 0;
            return size.ToString("N", nfi);
        }*/

        /// <summary>
        /// Удалить отмеченные записи
        /// </summary>
        private bool DeleteSelectedItems()
        {
            if (_settings.Fields.IsConfirmDelete && MessageBox.Show(LanguageManager.GetString("YouSure"), LanguageManager.GetString("Warning"), MessageBoxButtons.OKCancel) == DialogResult.OK ||
                 !_settings.Fields.IsConfirmDelete)
            {
                if (_undoRedoEngine.ListDuplicates.Items.Count > 0)
                {
                    _beginUpdate = true;
                    //Проверка не выделены ли в какой-нибудь группе все файлы
                    if (!_settings.Fields.IsAllowDelAllFiles)
                    {
                        SetStatusDuplicate(LanguageManager.GetString("CheckAllSelected"));
                        int index;
                        if (_undoRedoEngine.ListDuplicates.CheckAllChekedInGroup(out index))
                        {
                            _beginUpdate = false;
                            if (MessageBox.Show(this,
                                LanguageManager.GetString("AllSelected"),
                                LanguageManager.GetString("SelectedFilesDelete"),
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning) == DialogResult.Yes)
                                _settings.Fields.IsAllowDelAllFiles = true;
                            else
                            {
                                lvDuplicates.Items[index].Selected = true;
                                lvDuplicates.EnsureVisible(index);
                                return false;
                            }
                        }
                    }

                    this.Cursor = Cursors.WaitCursor;
                    //lvDuplicates.RetrieveVirtualItem -= lvDuplicates_RetrieveVirtualItem;
                    LabelStaStrip5.Text = String.Empty;
                    SetStatusState(StatusState.Duplicate);

                    //_undoRedoEngine.ListDuplicates.DeletingCompleteEvent += () =>
                    /*ListViewSave.DeletingCompleteDelegate dw = () =>
                    {
                        if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
                        {
                            object[] eventArgs = { };
                            Invoke(new ListViewSave.DeletingCompleteDelegate(dw), eventArgs);
                            return;
                        }

                        lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;

                        lvDuplicates.SelectedIndices.Clear();
                        this.Cursor = Cursors.Default;

                        showDuplicateInfo();
                        showDuplicateInfoSelected();

                        _beginUpdate = false;
                    };*/
                    //_undoRedoEngine.ListDuplicates.DeletingCompleteEvent += DeletingCompleteEventHandler;

                    //new Thread(_undoRedoEngine.ListDuplicates.DeleteChekedItems).Start();

                    _beginUpdate = true;
                    _undoRedoEngine.ListDuplicates.DeleteChekedItems();
                    _beginUpdate = false;

                    lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;

                    lvDuplicates.SelectedIndices.Clear();
                    this.Cursor = Cursors.Default;

                    showDuplicateInfo();
                    showDuplicateInfoSelected();

                    _beginUpdate = false;


                    //lvDuplicates_SelectedIndexChanged(lvDuplicates, new EventArgs());

                    //return true; todo сменить на void
                }
            }
            return false;
        }

        /*public void DeletingCompleteEventHandler()
        {
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                Invoke(new ListViewSave.DeletingCompleteDelegate(DeletingCompleteEventHandler));
                return;
            }

            lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;

            lvDuplicates.SelectedIndices.Clear();
            this.Cursor = Cursors.Default;

            showDuplicateInfo();
            showDuplicateInfoSelected();

            _beginUpdate = false;
        }*/
        #endregion

        #region SetStatus

        /// <summary>
        /// Устанавливает полосу состояния в нужный режим.
        /// </summary>
        private void SetStatusState(StatusState state)
        {
            if (state == StatusState.Search)
            {
                statusStrip1.Items.Clear();
                this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.LabelStaStrip1,
                this.LabelStaStrip2,
                this.LabelStaStrip3,
                this.LabelStaStrip4,
                this.LabelStaStrip5});

                statusStrip1.Items[0].Text = LanguageManager.GetString("statusStripSearch1");
                statusStrip1.Items[1].Text = LanguageManager.GetString("statusStripSearch2");
                statusStrip1.Items[2].Text = LanguageManager.GetString("statusStripSearch3");
                statusStrip1.Items[3].Text = LanguageManager.GetString("statusStripSearch4");
            }
            else if (state == StatusState.Duplicate)
            {
                statusStrip1.Items.Clear();
                this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
                {
                    this.LabelStaStrip1,
                    this.LabelStaStrip2,
                    this.LabelStaStrip5
                });
            }
        }

        private void SetStatusSearch(string status)
        {
            statusStrip1.Items[4].Text = status;
        }

        /// <summary>
        /// Устанавливает статус дубликатов
        /// </summary>
        /// <param name="count">Количество дубликатов double</param>
        /// <param name="size">Размер дубликатов ulong</param>
        /// <param name="ClearSelected">Очистить отображение выбранных записей</param>
        private void SetStatusDuplicate(double count, ulong size, bool ClearSelected)
        {
            //statusStrip1.Items[0].Text = statusStripDubli1 + count;
            statusStrip1.Items[0].Text = LanguageManager.GetString("statusStripDubli1") + count;
            statusStrip1.Items[1].Text = LanguageManager.GetString("statusStripDubli2") + GetStringToSize(size, "F03");
            if (ClearSelected)
                statusStrip1.Items[2].Text = String.Empty;
        }

        private void SetStatusDuplicate(int count)
        {
            statusStrip1.Items[0].Text = LanguageManager.GetString("statusStripDubli1") + count;
        }

        /// <summary>
        /// Set the current enable in statusStrip
        /// </summary>
        /// <param name="enable">Status message</param>
        private void SetStatusDuplicate(string status)
        {
            if (statusStrip1.Items.Count >= 2)
                statusStrip1.Items[2].Text = status;
        }
        #endregion

        #region List View Duplicates
        /// <summary>
        /// Открытие контекстного меню дубликатов.
        /// </summary>
        private void cmsDuplicates_Opening(object sender, CancelEventArgs e)
        {
            if (_undoRedoEngine.ListDuplicates.Items.Count > 0)
            {
                int index = lvDuplicates.FocusedItem.Index;
                if (File.Exists(_undoRedoEngine.ListDuplicates.GetPath(index)))
                {
                    cmsDuplicates.Items["tmsi_Dubli_RenameFile"].Visible = true;
                    cmsDuplicates.Items["tmsi_Dubli_MoveFileToNeighbour"].Visible = true;

                    GroupOfDupl group = _undoRedoEngine.ListDuplicates.GetGroup(index);
                    if (group != null)
                    {
                        // Если в группе больше двух файлов.
                        if (group.Items.Count > 2)
                        {
                            //cmsDuplicates.Items["renameFileLikeNeighbourToolStripMenuItem"].Enabled = false;
                            cmsDuplicates.Items["tmsi_Dubli_RenameFileLikeNeighbour"].Visible = false;
                            cmsDuplicates.Items["tmsi_Dubli_MoveSelectedFilesToFolder"].Visible = false;
                        }
                        else
                        {
                            cmsDuplicates.Items["tmsi_Dubli_RenameFileLikeNeighbour"].Visible = true;
                            if (
                                String.Compare(group.Items[0].Directory,
                                               group.Items[1].Directory, true) == 0)
                                cmsDuplicates.Items["tmsi_Dubli_MoveSelectedFilesToFolder"].Visible = false;
                            else
                                cmsDuplicates.Items["tmsi_Dubli_MoveSelectedFilesToFolder"].Visible = true;
                        }
                    }
                }
                else
                {
                    cmsDuplicates.Items["tmsi_Dubli_RenameFile"].Visible = false;
                    cmsDuplicates.Items["tmsi_Dubli_MoveFileToNeighbour"].Visible = false;
                    cmsDuplicates.Items["tmsi_Dubli_RenameFileLikeNeighbour"].Visible = false;
                    cmsDuplicates.Items["tmsi_Dubli_MoveSelectedFilesToFolder"].Visible = false;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }
        /// <summary>
        /// Open file with associated program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lvDuplicates_DoubleClick(object sender, EventArgs e)
        {
            fileOpen();
        }

        private void fileOpen()
        {
            if (lvDuplicates.FocusedItem != null)
            {
                int index = lvDuplicates.FocusedItem.Index;
                string filePreview = _undoRedoEngine.ListDuplicates.GetPath(index);
                try
                {
                    if (File.Exists(filePreview))
                        System.Diagnostics.Process.Start(filePreview);
                }
                catch (System.ArgumentException ae)
                {
                    MessageBox.Show(ae.Message);
                }
            }
        }

        private void lvDuplicates_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView lv = (ListView)sender;
            if (lv.FocusedItem != null)
            {
                int index = lv.FocusedItem.Index;
                string filePreview = _undoRedoEngine.ListDuplicates.GetPath(index);
                //ClearPicrureBox();

                if (File.Exists(filePreview))
                {
                    /*FileStream stream = new FileStream(filePreview, FileMode.Open, FileAccess.Read);
                    FileViewer viewer = new FileViewer(stream);
                    //viewer.Location = new System.Drawing.Point(0, 0);
                    viewer.Parent = this;
                    viewer.Dock = DockStyle.Fill;
                    this.splitContainer1.Panel1.Controls.Add(viewer);*/

                    _pictureBox1.UpdateImage(filePreview);
                    //pictureBox1.UpdateImagePadding();

                    if (_settings.Fields.ShowNeighboringFiles)
                        ShowingNeighboringFiles(filePreview);
                    /*if (LoadPictureBox(pictureBox1, filePreview))
                    {
                        ShowingNeighboringFiles(filePreview);
                    }*/

                    //statusStripPicture.Visible = true;
                    //toolStripStatusLabel_Width.Text = pictureBox1.Image.Width.ToString();
                    //toolStripStatusLabel_Height.Text = pictureBox1.Image.Height.ToString();
                }
                else //если файла не существует надо это указать
                {
                    _undoRedoEngine.ListDuplicates.FileNotExist(index);
                }
            }
            return;
        }

        private void ShowingNeighboringFiles(string filePreview)
        {
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(filePreview));
            List<FileInfo> files = new List<FileInfo>();
            files.AddRange(di.GetFiles());
            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].FullName == filePreview)
                {
                    if (i > 0)  //previos
                        if (File.Exists(files[i - 1].FullName))
                        {
                            //LoadPictureBox(pictureBoxPrev, files[i - 1].FullName);
                            _pictureBoxPrev.UpdateImage(files[i - 1].FullName);
                        }
                    if (i < files.Count - 1) //next
                        if (File.Exists(files[i + 1].FullName))
                            _pictureBoxNext.UpdateImage(files[i + 1].FullName);
                    //LoadPictureBox(pictureBoxNext, files[i + 1].FullName);
                    break;
                }
            }
        }

        #endregion

        #region Context Menu Search

        private void tsmi_Search_Add_Click(object sender, EventArgs e)
        {
            AddDirectory();
        }

        private void tsmi_Search_Edit_Click(object sender, EventArgs e)
        {
            EditDirectory();
        }

        private void tsmi_Search_Delete_Click(object sender, EventArgs e)
        {
            //checkedListDirectorySearch.Items.Remove(checkedListDirectorySearch.SelectedItem);
            lvDirectorySearch.FocusedItem.Remove();
        }


        private void tsmi_Search_SelectAll_Click(object sender, EventArgs e)
        {
            if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSearchFolder"])
            {
                foreach (ListViewItem item in lvDirectorySearch.Items)
                    item.Checked = true;
            }
            else if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSkipFolder"])
            {
                for (int i = 0; i < checkedListBoxSkipFolder.Items.Count; i++)
                    checkedListBoxSkipFolder.SetItemChecked(i, true);
            }
        }

        private void tsmi_Search_DeselectAll_Click(object sender, EventArgs e)
        {
            if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSearchFolder"])
            {
                foreach (ListViewItem item in lvDirectorySearch.Items)
                    item.Checked = false;
            }
            else if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSkipFolder"])
            {
                for (int i = 0; i < checkedListBoxSkipFolder.Items.Count; i++)
                    checkedListBoxSkipFolder.SetItemChecked(i, false);
            }
        }

        #endregion

        #region Context Menu Duplicate
        /// <summary>
        /// Контекстное меню дубликатов - Выбрать все кроме одного
        /// </summary>
        private void tmsi_Dubli_SelectAllButOne_Click(object sender, EventArgs e)
        {
            // Если есть выделенные.
            if (lvDuplicates.SelectedIndices.Count > 1)
                _undoRedoEngine.ListDuplicates.CheckAllButOne(lvDuplicates.SelectedIndices);
            else
                _undoRedoEngine.ListDuplicates.CheckAllButOne();
            lvDuplicates.Invalidate(); //все перерисовывается
            showDuplicateInfoSelected();
        }

        private void buttonDeleteSelectedFiles_Click(object sender, EventArgs e)
        {
            DeleteSelectedItems();
        }

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать все
        /// </summary>
        private void tmsi_Dubli_SelectAll_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.SelectedIndices.Count > 1)  //выделенные только обрабатываем
                _undoRedoEngine.ListDuplicates.CheckAll(lvDuplicates.SelectedIndices);
            else
                _undoRedoEngine.ListDuplicates.CheckAll();

            _undoRedoEngine.ListDuplicates.ColoringAllCheckedGroups();
            lvDuplicates.Invalidate(); //все перерисовывается
            showDuplicateInfoSelected();
        }

        /// <summary>
        /// Контекстное меню дубликатов - Сбросить все
        /// </summary>
        private void tmsi_Dubli_DeSelectAll_Click(object sender, EventArgs e)
        {
            //выделенные только обрабатываем
            if (lvDuplicates.SelectedIndices.Count > 1)
                _undoRedoEngine.ListDuplicates.DeselectAll(lvDuplicates.SelectedIndices);
            else
                _undoRedoEngine.ListDuplicates.DeselectAll();

            if (!_settings.Fields.IsAllowDelAllFiles)
                _undoRedoEngine.ListDuplicates.ColoringAllCheckedGroups();

            _undoRedoEngine.ListDuplicates.ColoringOfGroups();

            lvDuplicates.Invalidate(); //обновление иначе галочки не сотрутся

            showDuplicateInfoSelected();
        }

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать все в этой папке
        /// </summary>
        private void tmsi_Dubli_SelectAllInThisFolder_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                _undoRedoEngine.ListDuplicates.CheckAllInThisFolder(lvDuplicates.FocusedItem.Index);
                lvDuplicates.Invalidate(); //все перерисовываетсяSelectAllInThisFolder
                showDuplicateInfoSelected();
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать все в этой папке (в группах с этими папками)
        /// </summary>
        private void tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolders_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                _undoRedoEngine.ListDuplicates.CheckAllInThisFolderinGroupWithThisFolders(lvDuplicates.FocusedItem.Index);
                lvDuplicates.Invalidate(); //все перерисовывается
                showDuplicateInfoSelected();
            }
        }

        private void tmsi_Dubli_SelectGroup_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                _undoRedoEngine.ListDuplicates.CheckAllInThisGroup(lvDuplicates.FocusedItem.Index);
                lvDuplicates.Invalidate(); //все перерисовывается
                showDuplicateInfoSelected();
            }
        }

        private void lvDuplicates_KeyDown(object sender, KeyEventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                if (e.KeyCode == Keys.F2)
                {
                    lvDuplicates.LabelEdit = true;
                    lvDuplicates.FocusedItem.BeginEdit();
                }
                else if (e.KeyCode == Keys.F3)
                {
                    renameFileLikeNeighbour();
                }
                else if (e.KeyCode == Keys.Delete)
                {
                    DeleteItem(false);
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    fileOpen();
                }
                else if (e.KeyData == (Keys.Control | Keys.C))
                {
                    string name = _undoRedoEngine.ListDuplicates.GetFileName(lvDuplicates.FocusedItem.Index);
                    if (!String.IsNullOrEmpty(name))
                    {
                        Clipboard.SetDataObject(name);
                    }
                }
                else if (e.KeyData == (Keys.Control | Keys.V))
                {
                    if (Clipboard.ContainsText())
                    {
                        string pasteName = Clipboard.GetText();
                        int index = lvDuplicates.FocusedItem.Index;
                        _undoRedoEngine.RenameTo(index, pasteName);
                    }
                    /*if (this.GetCurrentListBox() != null)
                    {
                        if (Clipboard.ContainsFileDropList())
                        {
                            StringCollection fileDropList = Clipboard.GetFileDropList();
                            string[] array = new string[fileDropList.Count];
                            fileDropList.CopyTo(array, 0);
                            this.AddPath(array);
                        }
                        else if (Clipboard.ContainsText())
                        {
                            string[] additional = new string[] { Clipboard.GetText() };
                            this.AddPath(additional);
                        }
                    }*/
                }
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Скопировать путь
        /// </summary>
        private void tmsi_Dubli_CopyPath_Click(object sender, EventArgs e)
        {
            string name = _undoRedoEngine.ListDuplicates.GetDirectory(lvDuplicates.FocusedItem.Index);
            if (!String.IsNullOrEmpty(name))
            {
                Clipboard.SetDataObject(name);
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Переименовать файл
        /// </summary>
        private void tmsi_Dubli_RenameFile_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                lvDuplicates.LabelEdit = true;
                lvDuplicates.FocusedItem.BeginEdit();
            }
        }

        private void lvDuplicates_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            try
            {
                string label = e.Label;
                if (String.IsNullOrEmpty(label) || (label == lvDuplicates.FocusedItem.Text))
                {
                    e.CancelEdit = true;
                }
                else if (_undoRedoEngine.ListDuplicates.Items.Count > 0)
                {
                    int index = lvDuplicates.FocusedItem.Index;
                    if (_undoRedoEngine.RenameTo(index, label))
                        _undoRedoEngine.ListDuplicates.Items[index].FileName = label;
                    else
                        e.CancelEdit = true;
                    /*string fileName = _undoRedoEngine.ListDuplicates.GetPath(index);
                    string destFileName = Path.Combine(_undoRedoEngine.ListDuplicates.GetDirectory(index), label);
                    try
                    {
                        new FileInfo(fileName).MoveTo(destFileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        e.CancelEdit = true;
                    }*/

                }
                lvDuplicates.LabelEdit = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageManager.GetString("ErrorRename") + ex.Message);
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Переместить файл к соседу
        /// </summary>
        private void tmsi_Dubli_MoveFileToNeighbour_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                _undoRedoEngine.ListDuplicates.MoveFileToNeighbour(lvDuplicates.FocusedItem.Index);
                lvDuplicates.Invalidate(); //все перерисовывается
            }
        }

        private void RemoveMissingFilesFromList()
        {
            _beginUpdate = true;
            if (_undoRedoEngine.ListDuplicates.RemoveMissingFilesFromList())
            {
                lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;
                showDuplicateInfo();
                showDuplicateInfoSelected();
            }
            _beginUpdate = false;
        }

        /// <summary>
        /// Контекстное меню дубликатов - Переименовать файл как соседний
        /// </summary>
        private void tmsi_Dubli_RenameFileLikeNeighbour_Click(object sender, EventArgs e)
        {
            renameFileLikeNeighbour();
        }

        private void renameFileLikeNeighbour()
        {
            if (lvDuplicates.FocusedItem != null)
            {
                int index = lvDuplicates.FocusedItem.Index;
                _undoRedoEngine.RenameLikeNeighbour(index);
                lvDuplicates.Invalidate();
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            DeleteItem(true);
        }

        private void DeleteItem(bool FocusOnList)
        {
            if (_settings.Fields.IsConfirmDelete && MessageBox.Show(LanguageManager.GetString("YouSure"), LanguageManager.GetString("Warning"), MessageBoxButtons.OKCancel) == DialogResult.OK ||
                !_settings.Fields.IsConfirmDelete)
            {
                if (lvDuplicates.FocusedItem != null)
                {
                    int index = lvDuplicates.FocusedItem.Index;
                    if (_undoRedoEngine.ListDuplicates.DeleteItem(index))
                    {
                        lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;
                        showDuplicateInfo();
                        showDuplicateInfoSelected();

                        //Фокусировка на месте удаленного элемента
                        if (_undoRedoEngine.ListDuplicates.Items.Count > 0)
                        {
                            if (index <= lvDuplicates.Items.Count - 1)
                            {
                                lvDuplicates.SelectedIndices.Add(index);
                                lvDuplicates.FocusedItem = lvDuplicates.Items[index];
                                lvDuplicates_SelectedIndexChanged(lvDuplicates, new EventArgs());
                            }
                            else
                            {
                                lvDuplicates.SelectedIndices.Add(lvDuplicates.Items.Count - 1);
                            }
                            if (FocusOnList)
                                lvDuplicates.Focus();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Переместить выбранные файлы в папку
        /// </summary>
        private void tmsi_Dubli_MoveSelectedFilesToFolder_Click(object sender, EventArgs e)
        {
            MoveToFolder();
        }

        private void MoveToFolder()
        {
            if (_undoRedoEngine.ListDuplicates.Items.Count > 0)
            {
                int index = lvDuplicates.FocusedItem.Index;

                FormFolderSelect ffs = new FormFolderSelect();
                ffs.Owner = this;
                ffs.StartPosition = FormStartPosition.CenterParent;
                ffs.Text = LanguageManager.GetString("MoveFolder");
                ffs.Font = _settings.Fields.ProgramFont.ToFont();
                ffs.Icon = Properties.Resources.movetoIco;
                ffs.ShowSubDirCheck = false;

                /*if (!String.IsNullOrEmpty(_settings.Fields.FolderToMove))
                {
                    ffs.SelectedPath = _settings.Fields.FolderToMove;
                }*/
                if (lvDuplicates.FocusedItem != null)
                {
                    ffs.SelectedPath = _undoRedoEngine.ListDuplicates.GetDirectory(index);
                }

                if (ffs.ShowDialog() == DialogResult.OK)
                {
                    _undoRedoEngine.ListDuplicates.MoveCheckedToFolder(ffs.SelectedPath);
                    lvDuplicates.Invalidate(); //все перерисовывается
                    SetStatusDuplicate(lvDuplicates.Items.Count);
                }

                try
                {
                    ffs.Dispose();
                }
                catch (Exception ex)
                {
                    new CrashReport(ex, _settings, lvDuplicates).ShowDialog();
                }
            }
        }

        /// <summary>
        /// Есть ли переданный путь в путях для поиска дубликатов.
        /// </summary>
        private bool IsSearchDirectoryContain(string movePath)
        {
            bool ContainSearchPath = false;
            char[] split = new char[] { Path.DirectorySeparatorChar };
            for (int i = 0; i < lvDirectorySearch.CheckedItems.Count; i++)
            {
                string[] search = lvDirectorySearch.CheckedItems[i].ToString().Split(split, StringSplitOptions.RemoveEmptyEntries);
                string[] move = movePath.Split(split, StringSplitOptions.RemoveEmptyEntries);

                bool Include = true;
                if (move.Length < search.Length)
                {
                    Include = false;
                }
                else
                {
                    for (int j = 0; j < search.Length; j++)
                    {
                        if (!String.Equals(search[j], move[j]))
                        {
                            //IsContainSearchPath = true;
                            Include = false;
                            break;
                        }
                    }
                }

                if (Include)
                {
                    ContainSearchPath = true;
                    break;
                }
            }
            return ContainSearchPath;
        }
        #endregion

        #region SortinIcon
        public static bool SetSortIcon(ListView lv, int iColumn, SortOrder so)
        {
            if (lv == null)
            {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            try
            {
                IntPtr hHeader = NativeMethods.SendMessage(lv.Handle, NativeMethods.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);

                for (int i = 0; i < lv.Columns.Count; ++i)
                {
                    int nGetMsg = ((WinUtil.IsWindows2000 || WinUtil.IsWindowsXP ||
                                    WinUtil.IsAtLeastWindowsVista)
                                       ? NativeMethods.HDM_GETITEMW
                                       : NativeMethods.HDM_GETITEMA);
                    int nSetMsg = ((WinUtil.IsWindows2000 || WinUtil.IsWindowsXP ||
                                    WinUtil.IsAtLeastWindowsVista)
                                       ? NativeMethods.HDM_SETITEMW
                                       : NativeMethods.HDM_SETITEMA);
                    IntPtr pColIndex = new IntPtr(i);

                    NativeMethods.HDITEM hdItem = new NativeMethods.HDITEM();
                    hdItem.mask = NativeMethods.HDI_FORMAT;

                    if (NativeMethods.SendMessageHDItem(hHeader, nGetMsg, pColIndex, ref hdItem) == IntPtr.Zero)
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }

                    if ((i != iColumn) || (so == SortOrder.None))
                        hdItem.fmt &= (~NativeMethods.HDF_SORTUP & ~NativeMethods.HDF_SORTDOWN);
                    else
                    {
                        if (so == SortOrder.Ascending)
                        {
                            hdItem.fmt &= ~NativeMethods.HDF_SORTDOWN;
                            hdItem.fmt |= NativeMethods.HDF_SORTUP;
                        }
                        else // SortOrder.Descending
                        {
                            hdItem.fmt &= ~NativeMethods.HDF_SORTUP;
                            hdItem.fmt |= NativeMethods.HDF_SORTDOWN;
                        }
                    }

                    System.Diagnostics.Debug.Assert(hdItem.mask == NativeMethods.HDI_FORMAT);
                    if (NativeMethods.SendMessageHDItem(hHeader, nSetMsg, pColIndex, ref hdItem) == IntPtr.Zero)
                    {
                        System.Diagnostics.Debug.Assert(false);
                    }
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.Assert(false);
                return false;
            }

            return true;
        }

        private void UpdateColumnSortingIcons()
        {
            if (SetSortIcon(lvDuplicates, _lvwGroupSorter.SortColumn,
                _lvwGroupSorter.Order))
                return;

            /*if (lvwGroupSorter.SortColumn < 0)
            {
                System.Diagnostics.Debug.Assert(lvDuplicates.ListViewItemSorter == null);
            }*/

            string strAsc = "  \u2191"; // Must have same length
            string strDsc = "  \u2193"; // Must have same length
            if (WinUtil.IsWindows9x || WinUtil.IsWindows2000 || WinUtil.IsWindowsXP ||
                NativeLib.IsUnix())
            {
                strAsc = @"  ^";
                strDsc = @"  v";
            }
            else if (WinUtil.IsAtLeastWindowsVista)
            {
                strAsc = "  \u25B3";
                strDsc = "  \u25BD";
            }

            foreach (ColumnHeader ch in lvDuplicates.Columns)
            {
                string strCur = ch.Text, strNew = null;

                if (strCur.EndsWith(strAsc) || strCur.EndsWith(strDsc))
                {
                    strNew = strCur.Substring(0, strCur.Length - strAsc.Length);
                    strCur = strNew;
                }

                if ((ch.Index == _lvwGroupSorter.SortColumn) &&
                    (_lvwGroupSorter.Order != SortOrder.None))
                {
                    if (_lvwGroupSorter.Order == SortOrder.Ascending)
                        strNew = strCur + strAsc;
                    else if (_lvwGroupSorter.Order == SortOrder.Descending)
                        strNew = strCur + strDsc;
                }

                if (strNew != null) ch.Text = strNew;
            }//*/
        }
        #endregion

        #region List View Duplicate
        private void lvDuplicates_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == _lvwGroupSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (_lvwGroupSorter.Order == SortOrder.Ascending)
                    _lvwGroupSorter.Order = SortOrder.Descending;
                else
                    _lvwGroupSorter.Order = SortOrder.Ascending;
            }
            else
            {
                _lvwGroupSorter.Order = SortOrder.Ascending;
                _lvwGroupSorter.SortColumn = e.Column;
                /*switch (e.Column)
                { 
                    case 0:
                        lvwGroupSorter.SortColumn = 
                }*/
            }

            this.Cursor = Cursors.WaitCursor;

            _undoRedoEngine.ListDuplicates.Sort(_lvwGroupSorter);
            lvDuplicates.Invalidate(); //все перерисовывается

            UpdateColumnSortingIcons();

            this.Cursor = Cursors.Default;
        }

        private void CheckExistsColoring(ref ListView listView)
        {
            foreach (ListViewItem item in listView.Items)
            {
                if (!System.IO.File.Exists(Path.Combine(item.SubItems["Path"].Text, item.SubItems["FileName"].Text)))
                {
                    item.BackColor = _settings.Fields.ColorRowNotExist.ToColor();
                }
            }
        }

        private void lvDuplicates_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            int index = e.Item.Index;
            //_listDuplicates.Items[indexOfGroupWithAllChecked].Checked = !_listDuplicates.Items[indexOfGroupWithAllChecked].Checked;
            //string group = _undoRedoEngine.ListDuplicates.Items[index].Group; //выделенная группа

            //if (e.Item.Checked)
            if (_undoRedoEngine.ListDuplicates.Items[index].Checked)
            {
                //if (_undoRedoEngine.ListDuplicates.ColoringAllCheckedGroups())
                if (_undoRedoEngine.ListDuplicates.AllChekedInGroup(index))
                    lvDuplicates.Invalidate();
                //проверка не выделены ли все файлы в группе
                /*if (!_settings.Fields.IsAllowDelAllFiles) //если разрешено удаление всех, то не проверяем 
                {
                    int indexOfGroup;
                    if (_undoRedoEngine.ListDuplicates.CheckAllChekedInGroup(out indexOfGroup))
                        _undoRedoEngine.ListDuplicates.ColoringAllCheckedGroups();
                }*/
            }
            else //снятие флажка
            {
                if (_undoRedoEngine.ListDuplicates.Items[index].Color == _settings.Fields.ColorRowError)
                {
                    _undoRedoEngine.ListDuplicates.ClearColorForGroup(index);
                    _undoRedoEngine.ListDuplicates.ColoringOfGroups();
                    lvDuplicates.Invalidate();
                }
            }

            showDuplicateInfoSelected();

            //e.Item.Bounds.
            //lvDuplicates.Items[e.Item].In
        }
        #endregion

        #region Status Strip

        private void showDuplicateInfo()
        {
            bool ClearSelected = false;
            ulong nSizes = 0;

            foreach (ListViewItemSave item in _undoRedoEngine.ListDuplicates.Items)
            {
                nSizes += ulong.Parse(item.SubItems[2].Text.Replace(" ", string.Empty));
            }
            SetStatusDuplicate(lvDuplicates.Items.Count, nSizes, ClearSelected);
        }

        private void showDuplicateInfoSelected()
        {
            ulong nSizes = 0;
            //string bytesName = null;
            //ulong dSizes = 0;
            uint checkedCount = 0;

            foreach (ListViewItemSave item in _undoRedoEngine.ListDuplicates.Items)
            {
                if (item.Checked)
                {
                    checkedCount++;
                    nSizes += ulong.Parse(item.SubItems[2].Text.Replace(" ", string.Empty));
                }
            }
            SetStatusDuplicate(checkedCount.ToString() +
                LanguageManager.GetString("DuplicateFilesTotalSize") +
                GetStringToSize(nSizes, "F03"));
            //SetStatusDuplicate(checkedCount + LanguageManager.GetString("DuplicateFilesTotalSize"));
        }

        private string GetStringToSize(ulong size, string pattern)
        {
            string measuringUnit;

            if (size < 1024)
            {
                measuringUnit = " " + LanguageManager.GetProperty(this, "rdbLBytes.Text"); //" Byte";
            }
            else if (size < (1024 * 1024))
            {
                size = size / 1024;
                measuringUnit = " " + LanguageManager.GetProperty(this, "rdbLKilo.Text"); //" Kb";
            }
            else if (size < (1024 * 1024 * 1024))
            {
                size = size / (1024 * 1024);
                measuringUnit = " " + LanguageManager.GetProperty(this, "rdbLMega.Text"); //" Mb";
            }
            else
            {
                size = size / (1024 * 1024 * 1024);
                measuringUnit = " " + LanguageManager.GetProperty(this, "rdbLGiga.Text"); //" Gb";
            }
            return size.ToString(pattern) + measuringUnit;
        }

        private void tmsi_Select_ByNewestFilesInEachGroup_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.SelectedIndices.Count > 1)
                _undoRedoEngine.ListDuplicates.CheckByDate(lvDuplicates.SelectedIndices, SortByDateEnum.NewestFirst);
            else
                _undoRedoEngine.ListDuplicates.CheckByDate(SortByDateEnum.NewestFirst);
            lvDuplicates.Invalidate();
            showDuplicateInfoSelected();
        }

        private void tmsi_Select_ByOldestFileInEachGroup_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.SelectedIndices.Count > 1)
                _undoRedoEngine.ListDuplicates.CheckByDate(lvDuplicates.SelectedIndices, SortByDateEnum.OlderFirst);
            else
                _undoRedoEngine.ListDuplicates.CheckByDate(SortByDateEnum.OlderFirst);
            lvDuplicates.Invalidate();
            showDuplicateInfoSelected();
        }

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать по дате - Старый файл в каждой группе
        /// </summary>
        private void tmsi_Dubli_SelectByDateOldestFiles_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                _undoRedoEngine.ListDuplicates.CheckByDate(lvDuplicates.FocusedItem.Index, SortByDateEnum.OlderFirst);
                lvDuplicates.Invalidate(); //все перерисовывается
                showDuplicateInfoSelected();
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать по дате - Новый файл в каждой группе
        /// </summary>
        private void tmsi_Dubli_SelectByDateNewestFiles_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                _undoRedoEngine.ListDuplicates.CheckByDate(lvDuplicates.FocusedItem.Index, SortByDateEnum.NewestFirst);
                lvDuplicates.Invalidate(); //все перерисовывается
                showDuplicateInfoSelected();
            }
        }

        private void tmsi_Select_ByFileName_Click(object sender, EventArgs e)
        {
            FormFileNameSelect ffns = new FormFileNameSelect();
            ffns.Owner = this;
            ffns.StartPosition = FormStartPosition.CenterParent;
            ffns.Font = _settings.Fields.ProgramFont.ToFont();
            ffns.Icon = Properties.Resources.TerminatorIco32;
            if (ffns.ShowDialog() == DialogResult.OK)
            {

                if (lvDuplicates.SelectedIndices.Count > 1) //в выбранных
                    _undoRedoEngine.ListDuplicates.CheckByName(lvDuplicates.SelectedIndices, ffns.SelectedName);
                else
                    _undoRedoEngine.ListDuplicates.CheckByName(ffns.SelectedName);
                lvDuplicates.Invalidate(); //все перерисовывается
                showDuplicateInfoSelected();
            }
            try
            {
                ffns.Dispose();
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings, lvDuplicates).ShowDialog();
            }
        }

        private void tmsi_Select_ByShorterFileNameLength_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.SelectedIndices.Count > 1) //в выбранных
                _undoRedoEngine.ListDuplicates.CheckByFileNameLength(lvDuplicates.SelectedIndices, SortByFileNameLengthEnum.ShorterFirst);
            else //во всех
                _undoRedoEngine.ListDuplicates.CheckByFileNameLength(SortByFileNameLengthEnum.ShorterFirst);
            lvDuplicates.Invalidate(); //все перерисовывается
            showDuplicateInfoSelected();
        }

        private void tmsi_Select_ByLongerFileNameLength_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.SelectedIndices.Count > 1) //в выбранных
                _undoRedoEngine.ListDuplicates.CheckByFileNameLength(lvDuplicates.SelectedIndices, SortByFileNameLengthEnum.LongerFirst);
            else //во всех
                _undoRedoEngine.ListDuplicates.CheckByFileNameLength(SortByFileNameLengthEnum.LongerFirst);
            lvDuplicates.Invalidate(); //все перерисовывается
            showDuplicateInfoSelected();
        }

        private void tsmi_Select_biggestNumberInEachGroup_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.SelectedIndices.Count > 1) //в выбранных
                _undoRedoEngine.ListDuplicates.CheckByNumberInFileName(lvDuplicates.SelectedIndices, SortByNumberInFileNameEnum.BiggerFirst);
            else //во всех
                _undoRedoEngine.ListDuplicates.CheckByNumberInFileName(SortByNumberInFileNameEnum.BiggerFirst);
            lvDuplicates.Invalidate(); //все перерисовывается
            showDuplicateInfoSelected();
        }

        private void tsmi_Select_lowestNumberInEachGroup_Click(object sender, EventArgs e)
        {
            //во всех
            _undoRedoEngine.ListDuplicates.CheckByNumberInFileName(SortByNumberInFileNameEnum.LowestFirst);
            lvDuplicates.Invalidate(); //все перерисовывается
            showDuplicateInfoSelected();
        }


        /// <summary>
        /// Контекстное меню дубликатов - Удалить группу из списка
        /// </summary>
        private void tmsi_Dubli_DeleteGroup_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.SelectedIndices.Count > 1)
                _undoRedoEngine.ListDuplicates.DeleteGroupsFromList(lvDuplicates.SelectedIndices);
            else
                _undoRedoEngine.DeleteGroupsFromList(lvDuplicates.FocusedItem.Index);
            lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;
            lvDuplicates.Invalidate(); //все перерисовывается
            showDuplicateInfoSelected();
        }

        #endregion

        #region Language

#if ExtLang
        private readonly string Filename = Path.Combine(Environment.CurrentDirectory, "language.xml");
        private void SaveLanguages()
        {
            //сохраняем данные настроек в файл хмл
            XmlWriterSettings settingsXml = new XmlWriterSettings();
            // включаем отступ для элементов XML документа
            // (позволяет наглядно изобразить иерархию XML документа)
            settingsXml.Indent = true;
            //settingsXml.IndentChars = "    "; // задаем отступ, здесь у меня 4 пробела
            // задаем переход на новую строку
            settingsXml.NewLineChars = "\n";

            using (XmlWriter output = XmlWriter.Create(Filename, settingsXml))
            {
                // Создали открывающийся тег
                output.WriteStartElement("language");
                output.WriteAttributeString("culture", "en");
                // Создаем элемент
                output.WriteElementString("author", "D.Borisov");
                output.WriteElementString("version", "1.0");

                output.WriteStartElement("messages");

                /*output.WriteStartElement("message");
                output.WriteAttributeString("name", "S_AppTitle");
                output.WriteAttributeString("value", "DupTerminator");
                output.WriteEndElement();*/

                output.WriteStartElement("message");
                output.WriteAttributeString("name", "S_NotSetSearchDir");
                output.WriteAttributeString("value", "Not set search directory!");
                output.WriteEndElement();
                output.WriteStartElement("message");
                output.WriteAttributeString("name", "S_Error");
                output.WriteAttributeString("value", "Error");
                output.WriteEndElement();
                output.WriteStartElement("message");
                output.WriteAttributeString("name", "classTypes_Picture");
                output.WriteAttributeString("value", "Picture");
                output.WriteEndElement();
                output.WriteStartElement("message");
                output.WriteAttributeString("name", "classTypes_Audio");
                output.WriteAttributeString("value", "Audio");
                output.WriteEndElement();
                output.WriteStartElement("message");
                output.WriteAttributeString("name", "classTypes_Video");
                output.WriteAttributeString("value", "Video");
                output.WriteEndElement();
                output.WriteStartElement("message");
                output.WriteAttributeString("name", "classTypes_Documents");
                output.WriteAttributeString("value", "Documents");
                output.WriteEndElement();
                output.WriteStartElement("message");
                output.WriteAttributeString("name", "classTypes_Saved sites");
                output.WriteAttributeString("value", "Saved sites");
                output.WriteEndElement();
                output.WriteStartElement("message");
                output.WriteAttributeString("name", "Crash_UnknownError");
                output.WriteAttributeString("value", "An unknown error has occurred. WallpaperChanger cannot continue.");
                output.WriteEndElement();
                output.WriteStartElement("message");
                output.WriteAttributeString("name", "Crash_ErrorReportSent");
                output.WriteAttributeString("value", "Error report sent. Thank you for helping to improve ");
                output.WriteEndElement();
                output.WriteStartElement("message");
                output.WriteAttributeString("name", "Crash_ SendingReportFailed");
                output.WriteAttributeString("value", " Sending report failed.");
                output.WriteEndElement();

                output.WriteEndElement(); //messages

                output.WriteStartElement("forms");

                SaveLanguage(this, output);

                FormAbout ab = new FormAbout();
                SaveLanguage(ab, output);
                ab.Dispose();

                FormFileFilterSelect fffs = new FormFileFilterSelect();
                SaveLanguage(fffs, output);
                fffs.Dispose();

                FormFileNameSelect ffns = new FormFileNameSelect();
                SaveLanguage(ffns, output);
                ffns.Dispose();

                FormFolderSelect ffs = new FormFolderSelect();
                SaveLanguage(ffs, output);
                ffs.Dispose();

                FormJobLoad fjl = new FormJobLoad();
                SaveLanguage(fjl, output);
                fjl.Dispose();

                FormSetting fs = new FormSetting(new FormMain());
                SaveLanguage(fs, output);
                fs.Dispose();

                FormUpdate fu = new FormUpdate();
                SaveLanguage(fu, output);
                fu.Dispose();

                CrashReport cr = new CrashReport(new Exception());
                SaveLanguage(cr, output);
                cr.Dispose();

                FormProgress fp = new FormProgress();
                SaveLanguage(fp, output);
                fp.Dispose();

                output.WriteEndElement();//Forms
                output.WriteEndElement();//language
                // Сбрасываем буфферизированные данные
                output.Flush();
                // Закрываем фаил, с которым связан output
                output.Close();
            }
        }

        private void SaveLanguage(Form form, XmlWriter writer)
        {
                writer.WriteStartElement("form");
                writer.WriteAttributeString("type", form.GetType().FullName);
                writer.WriteStartElement("property");
                writer.WriteAttributeString("name", "Text");
                writer.WriteAttributeString("value", form.Text);
                writer.WriteEndElement();

                WriteContols(writer, form.Controls);
                writer.WriteEndElement();//Form
        }

        private void WriteContols(XmlWriter writer, Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (!String.IsNullOrEmpty(control.Text) 
                    && !String.IsNullOrEmpty(control.Name)
                    && control.Text != "0"
                    && control.Name != "statusStrip1"
                    && control.Name != "menuMain"
                    && control.Name != "labelListRowFontValue"
                    && control.Name != "labelProgramFontValue")
                {
                    writer.WriteStartElement("property");
                    writer.WriteAttributeString("name", control.Name + ".Text");
                    writer.WriteAttributeString("value", control.Text);
                    writer.WriteEndElement();
                }

                if (control is MenuStrip)
                //if (control.GetType().IsSubclassOf(typeof(ToolStrip)))
                {
                    foreach (ToolStripMenuItem item in ((MenuStrip)control).Items)
                    //foreach (ToolStripMenuItem item in control.Items)
                    {
                        writer.WriteStartElement("property");
                        writer.WriteAttributeString("name", item.Name + ".Text");
                        writer.WriteAttributeString("value", item.Text);
                        writer.WriteEndElement();
                        foreach (ToolStripDropDownItem subitem in item.DropDownItems)
                        {
                            writer.WriteStartElement("property");
                            writer.WriteAttributeString("name", subitem.Name + ".Text");
                            writer.WriteAttributeString("value", subitem.Text);
                            writer.WriteEndElement();
                            //bool f = subitem.HasDropDownItems;
                            foreach (ToolStripDropDownItem subitem2 in subitem.DropDownItems)
                            {
                                writer.WriteStartElement("property");
                                writer.WriteAttributeString("name", subitem2.Name + ".Text");
                                writer.WriteAttributeString("value", subitem2.Text);
                                writer.WriteEndElement();
                            }
                        }
                        
                        //.DropDownItems
                    }
                }

                if (control.ContextMenuStrip != null)
                {
                    for (int i = 0; i < control.ContextMenuStrip.Items.Count; i++)
                    {
                        if (!(control.ContextMenuStrip.Items[i] is ToolStripSeparator))
                        {
                            writer.WriteStartElement("property");
                            writer.WriteAttributeString("name", control.ContextMenuStrip.Items[i].Name + ".Text");
                            writer.WriteAttributeString("value", control.ContextMenuStrip.Items[i].Text);
                            writer.WriteEndElement();

                            //control.ContextMenuStrip.Items[i].
                            //tmsi_Select_ByLastAcess.DropDownItems
                            foreach (ToolStripDropDownItem subitem in ((ToolStripMenuItem)control.ContextMenuStrip.Items[i]).DropDownItems)
                            {
                                writer.WriteStartElement("property");
                                writer.WriteAttributeString("name", subitem.Name + ".Text");
                                writer.WriteAttributeString("value", subitem.Text);
                                writer.WriteEndElement();
                            }
                        }
                    }
                }

                if (control.HasChildren)
                {
                    WriteContols(writer, control.Controls);
                }
            }
        }
#endif

        private void InitLanguage()
        {
            string savedLang = _settings.Fields.Language;
            if (String.IsNullOrEmpty(savedLang))
            {
                CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
                if (!String.IsNullOrEmpty(cultureInfo.Parent.Name))
                    savedLang = cultureInfo.Parent.Name;
            }
            bool langFound = false;

            foreach (string lang in LanguageManager.Languages)
            {
                if (savedLang == lang)
                    langFound = true;

                var menuItem = new ToolStripMenuItem { Text = LanguageManager.GetNativeName(lang) };
                menuItem.Click += OnLanguageMenuItemCliick;
                menuItem.Tag = lang;
                toolStripMenuItem_Language.DropDownItems.Add(menuItem);
            }

            SetCurrentLang(String.IsNullOrEmpty(savedLang) || !langFound ? "en" : savedLang);
        }

        private void OnLanguageMenuItemCliick(object sender, EventArgs e)
        {
            var menuItem = (ToolStripMenuItem)sender;
            SetCurrentLang(menuItem.Tag.ToString());
        }

        private void SetCurrentLang(string lang)
        {
            foreach (ToolStripMenuItem item in toolStripMenuItem_Language.DropDownItems)
                item.Checked = item.Tag.ToString() == lang;

            _settings.Fields.Language = lang;

            LanguageManager.SetLanguage(lang);
            LanguageManager.Localize(this);
            LocalizeLVDirectorySearch();

            UpdatePanelWidth(panel2);
            UpdatePanelWidth(panel4);

            if (lvDuplicates.Columns.Count > 0)
            {
                lvDuplicates.Columns[0].Text = LanguageManager.GetString("ListViewColumn_FileName");
                lvDuplicates.Columns[1].Text = LanguageManager.GetString("ListViewColumn_Path");
                lvDuplicates.Columns[2].Text = LanguageManager.GetString("ListViewColumn_Size");
                lvDuplicates.Columns[3].Text = LanguageManager.GetString("ListViewColumn_FileType");
                lvDuplicates.Columns[4].Text = LanguageManager.GetString("ListViewColumn_LastAccessed");
                lvDuplicates.Columns[5].Text = LanguageManager.GetString("ListViewColumn_MD5Checksum");
            }

            if (lvDuplicates.Items.Count > 0)
            {
                SetStatusState(StatusState.Duplicate); ;
                showDuplicateInfo();
            }
            else
            {
                SetStatusState(StatusState.Search);
            }
        }

        private void LocalizeLVDirectorySearch()
        {
            foreach (ListViewItem lv in lvDirectorySearch.Items)
            {
                if (lv.SubItems["SubDir"].Tag != null)
                {
                    if (Convert.ToInt32(lv.SubItems["SubDir"].Tag) == 1)
                        lv.SubItems["SubDir"].Text = LanguageManager.GetString("Yes");
                    else if (Convert.ToInt32(lv.SubItems["SubDir"].Tag) == 0)
                        lv.SubItems["SubDir"].Text = LanguageManager.GetString("No");
                }
                //else
                //    lv.SubItems["SubDir"].Text = LanguageManager.GetString("Yes");
            }
        }

        #endregion

        #region lvDuplicates

        private void lvDuplicates_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (!_beginUpdate)
            {
                int index = e.ItemIndex;
                //if (index >= _undoRedoEngine.ListDuplicates.Items.Count)
                if (index >= _model.Dublicates.Count)
                {
                    throw new ArgumentOutOfRangeException($"RetrieveVirtualItem: index({index}) >= ListDuplicates.Items({_undoRedoEngine.ListDuplicates.Items.Count}), lvDuplicates.VirtualListSize({lvDuplicates.VirtualListSize})");
                }

                ListViewItem listViewItem = new ListViewItem(
                    new string[]
                    {
                        _model.Dublicates[index].Checksum,
                        _model.Dublicates[index].Path,
                        "",
                        "",
                        "",
                        "",
                    });

                /*
                ListViewItem listViewItem = new ListViewItem(
                        new string[]
                    {
                        _undoRedoEngine.ListDuplicates.Items[index].SubItems[0].Text,
                        _undoRedoEngine.ListDuplicates.Items[index].SubItems[1].Text,
                        GetStringToSize(ulong.Parse(_undoRedoEngine.ListDuplicates.Items[index].SubItems[2].Text), "F01"),
                        _undoRedoEngine.ListDuplicates.Items[index].SubItems[3].Text,
                        _undoRedoEngine.ListDuplicates.Items[index].SubItems[4].Text,
                        _undoRedoEngine.ListDuplicates.Items[index].SubItems[5].Text
                    }
                    );


                if (_undoRedoEngine.ListDuplicates.Items[index].Checked)
                {
                    listViewItem.Checked = true;
                    listViewItem.Checked = false;
                    listViewItem.Checked = true;
                    listViewItem.Font = _fontStrikeout;
                }
                else
                {
                    listViewItem.Checked = true;
                    listViewItem.Checked = false;
                    listViewItem.Font = _fontRegular;
                }

                listViewItem.BackColor = _undoRedoEngine.ListDuplicates.Items[index].Color.ToColor();
                */

                e.Item = listViewItem;
            }
            else
            {
                e.Item = new ListViewItem(new string[]
                    {
                        "",
                        "",
                        "",
                        "",
                        "",
                        ""
                    });
            }
        }

        /*private void lvDuplicates_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
            if (!e.Item.Checked)
            {
                e.Item.Checked = true;
                e.Item.Checked = false;
            }
            /*Rectangle rectItem = lvDuplicates.GetItemRect(e.ItemIndex, ItemBoundsPortion.Entire);
            Rectangle rectText = lvDuplicates.GetItemRect(e.ItemIndex, ItemBoundsPortion.ItemOnly);

            if (!e.Item.Checked)
            {
                ControlPaint.DrawCheckBox(e.Graphics, rectItem.Left, rectItem.Top, rectText.Left, rectText.Height, ButtonState.Normal);
            }
            else
            {
                ControlPaint.DrawCheckBox(e.Graphics, rectItem.Left, rectItem.Top, rectText.Left, rectText.Height, ButtonState.Checked);
            }
        }*/

        private void lvDuplicates_MouseClick(object sender, MouseEventArgs e)
        {
            ListView lv = (ListView)sender;
            ListViewItem lvi = lv.GetItemAt(e.X, e.Y);
            if (lvi != null)
            {
                if (e.X < (lvi.Bounds.Left + 18))
                {
                    //lvi.Checked = !lvi.Checked;
                    //lvi.Checked = true;
                    //lvDuplicates.Items[lvi.Index].Checked = true;
                    //lvi.Text = "chec";
                    //lvi.SubItems[1].Text = "sub";
                    _undoRedoEngine.ListDuplicates.Items[lvi.Index].Checked = !_undoRedoEngine.ListDuplicates.Items[lvi.Index].Checked;

                    lvDuplicates_ItemChecked(sender, new ItemCheckedEventArgs(lvi));

                    lv.Invalidate(lvi.Bounds);
                }
            }
        }

        #endregion

        private void tmsi_Dubli_AddDirectoryToSkipped_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                AddToSkipFolders(Path.GetDirectoryName(_undoRedoEngine.ListDuplicates.GetPath(lvDuplicates.FocusedItem.Index)));
                _undoRedoEngine.DeleteGroupsFromList(lvDuplicates.FocusedItem.Index);
                lvDuplicates.VirtualListSize = _undoRedoEngine.ListDuplicates.Items.Count;
                lvDuplicates.Invalidate(); //все перерисовывается
                showDuplicateInfoSelected();
            }
        }


        /*private void lvDuplicates_VirtualItemsSelectionRangeChanged(object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e)
        {
            //MessageBox.Show("VirtualItemsSelectionRangeChange: start=" + e.StartIndex.ToString() + ", end=" + e.EndIndex.ToString());
        }*/

        private void tmsi_Dubli_Delete_Click(object sender, EventArgs e)
        {
            DeleteItem(false);
        }

        #region Члены IMainView

        public new void Show()
        {
            Application.Run(this);
        }

        #endregion

  




    }
}
