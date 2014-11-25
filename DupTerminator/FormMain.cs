//#define ExtLang  //извлечь языки в xml

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using DupTerminator.Native;
using DupTerminator.Util;
using System.Globalization;
using System.Xml; //Language
//using Microsoft.VisualBasic;
//using System.Runtime.InteropServices;//DllImport
//using System.Drawing.Drawing2D; //menu paint



namespace DupTerminator
{
    internal partial class FormMain : BaseForm
    {
        private ToolTip ttMainForm;
        private FileFunctions fFunctions = new FileFunctions();
        //Properties.Settings mySettings = new Properties.Settings();
        private SettingsApp _settings = new SettingsApp(); //экземпляр класса с настройками 
        private DateTime _timeStart;
        //private int _lastCount;
        //private bool _cancell = false;

        //обработчки сортировки колонок
        private ListViewGroupSorter lvwGroupSorter;

        public bool AllowListChkUpdate;

        //public const string defaultDirectory = "default";
        private VersionManager.UpdateChecker updateChecker = null;
        private VersionManager.VersionInfo versionInfo = null;
        //private Boolean showFormVersion = false;

        // The smaller the number the less sensitive
        const uint SHOW_SENSITIVITY = 5;

        private ITaskbarList3 taskbarProgress;

        //SQLite
        private DBManager _dbManager = new DBManager();

        public FormMain()
        {
            InitializeComponent();

            if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1)
                || Environment.OSVersion.Version.Major > 6)
                taskbarProgress = (ITaskbarList3)new ProgressTaskbar();


            AllowListChkUpdate = true;
            // Create an instance of a ListView column sorter and assign it 
            // to the ListView control.
            lvwGroupSorter = new ListViewGroupSorter();
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
            if (!isDotNet35Installed())
            {
                //MessageBox.Show("You Need Microsoft .NET Framework 4 Full in order to run this program.", ".NET Framework Detection", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                if (MessageBox.Show("You Need Microsoft .NET Framework 3.5 in order to run this program. Want to download .Net Framework 3.5?", "Warning",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                    System.Diagnostics.Process.Start("http://www.microsoft.com/ru-ru/download/details.aspx?id=22");
                base.Close();
                //System.Diagnostics.Process.GetCurrentProcess().Kill();
            }


            readSetting();

            #if ExtLang
            SaveLanguages();
            #else
            InitLanguage();
            #endif

            if (_settings.Fields.IsCheckUpdate)
            {
                toolStripMenuItem_CheckForUpdate.Enabled = false;
                updateChecker = new VersionManager.UpdateChecker(false);
                updateChecker.VersionChecked += new VersionManager.UpdateChecker.NewVersionCheckedHandler(versionChecker_NewVersionChecked);
            }

            SetStatusSearch();

            ttMainForm = new ToolTip();
            /*ttMainForm.SetToolTip(buttonStart, "Starts the search for duplicate files");
            ttMainForm.SetToolTip(buttonAddDirectory, "Adds directory");
            ttMainForm.SetToolTip(chkRecurse, "Scan all directories and subdirectories for duplicate files");
            ttMainForm.SetToolTip(comboBoxIncludeExtension, "Types files which be included to search");
            ttMainForm.SetToolTip(comboBoxExcludeExtension, "Types files which be excluded to search");*/
            ttMainForm.SetToolTip(buttonStart, LanguageManager.GetString("toolTip_buttonStart"));
            ttMainForm.SetToolTip(buttonAddDirectory, LanguageManager.GetString("toolTip_buttonAddDirectory"));
            //ttMainForm.SetToolTip(chkRecurse, LanguageManager.GetString("toolTip_chkRecurse"));
            ttMainForm.SetToolTip(comboBoxIncludeExtension, LanguageManager.GetString("toolTip_comboBoxIncludeExtension"));
            ttMainForm.SetToolTip(comboBoxExcludeExtension, LanguageManager.GetString("toolTip_comboBoxExcludeExtension"));
            ttMainForm.SetToolTip(buttonSelectBy, LanguageManager.GetString("toolTip_buttonSelectBy"));
            ttMainForm.SetToolTip(buttonMove, LanguageManager.GetString("toolTip_buttonMove"));

            toolStripMenuItem_Current.Text = _settings.Fields.LastJob;
            Text = string.Format("{0} - {1}", AssemblyHelper.AssemblyTitle, _settings.Fields.LastJob);
            //lvDuplicates.ContextMenuStrip = cmsDuplicates;
            //lvDuplicates.ShowGroups = false;

            Set_ListViewItemDupl(lvDuplicates);
            Set_ListViewItemDirectorySearch(lvDirectorySearch);

            Load_listDirectorySearch(_settings.Fields.LastJob);
            Load_listDirectorySkipped(_settings.Fields.LastJob);
            //if (_settings.Fields.IsSaveLoadListDub)
                //Load_ListDuplicate();

            //System.Diagnostics.Debug.WriteLine("Form1_Load _dbManager.Active=" + _dbManager.Active);
            fFunctions.settings = _settings;
            fFunctions.dbManager = _dbManager;
            //  событие                     подписчик   экземпляр делегата
            //DCSearch.FolderChangedEvent += new EventHandler(DCSearch_FolderChanged);
            //          event                   delegate FileCountAvailableDelegate(double Number)  private void FileCountCompleteEventHandler(double Number)
            //public delegate void FileCheckInProgressDelegate(string fileName, int currentCount);
            //public event FileCheckInProgressDelegate FileCheckInProgressEvent;
            //FileCheckInProgressEvent(efiGroup.fileInfo.FullName, currentFileCount);
            //private delegate void FileCheckUpdateDelegate(string fileName, int currentCount);
            //private void FileUpdateEventHandler(string fileName, int currentCount);
            //fFunctions.FileCheckInProgressEvent += new FileFunctions.FileCheckInProgressDelegate(FileUpdateEventHandler);
            //событие вызывающего += new делегат вызывающего(собыите принимающего)
            fFunctions.FolderChangedEvent += new FileFunctions.FolderChangedDelegate(FolderChangedEventHandler);
            fFunctions.FileCountAvailableEvent += new FileFunctions.FileCountAvailableDelegate(FileCountCompleteEventHandler);
            fFunctions.FileListAvailableEvent += new FileFunctions.FileListAvailableDelegate(CompleteFileListAvailableEventHandler);
            fFunctions.DuplicateFileListAvailableEvent += new FileFunctions.DuplicateFileListAvailableDelegate(DuplicatFileListAvailableEventHandler);
            fFunctions.FileCheckInProgressEvent += new FileFunctions.FileCheckInProgressDelegate(FileUpdateEventHandler);
            fFunctions.SearchCancelledEvent += new FileFunctions.SearchCancelledDelegate(SearchCancelledEventHandler);
        }

        public static bool isDotNet35Installed()
        {
            try
            {
                return (Convert.ToInt32(Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5").GetValue("Install")) == 1);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Загрузить список директорий поиска
        /// </summary>
        /// <param name="directory"></param>
        private void Load_listDirectorySearch(string directory)
        {
            int i = 0;

            string filePath;
            if (directory == string.Empty)
            {
                directory = Path.Combine(Application.StartupPath, Const.defaultDirectory);
                filePath = Path.Combine(directory, Const.fileNameDirectorySearch);
            }
            else if (!IsDirectory(directory))
            {
                directory = Path.Combine(Application.StartupPath, directory);
                filePath = Path.Combine(directory, Const.fileNameDirectorySearch);
            }
            else
            {
                filePath = Const.fileNameDirectorySearch;
            }
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    List<ListViewItemSearchDir> listDir = new List<ListViewItemSearchDir>();
                    Stream file = new System.IO.FileStream(filePath, FileMode.OpenOrCreate);
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    listDir = (List<ListViewItemSearchDir>)formatter.Deserialize(file);
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
                            if (item.IsSubDir)
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
                    file.Close();
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

            string filePath;
            if (directory == string.Empty)
            {
                directory = Path.Combine(Application.StartupPath, Const.defaultDirectory);
                filePath = Path.Combine(directory, Const.fileNameDirectorySkipped);
            }
            else if (!IsDirectory(directory))
            {
                directory = Path.Combine(Application.StartupPath, directory);
                filePath = Path.Combine(directory, Const.fileNameDirectorySkipped);
            }
            else
                filePath = Const.fileNameDirectorySkipped;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.Collections.ArrayList al;
                    Stream file = new System.IO.FileStream(filePath, FileMode.OpenOrCreate);
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    al = (System.Collections.ArrayList)formatter.Deserialize(file);
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
                    file.Close();
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void Load_ListDuplicate(string directory)
        {
            ulong size = 0;
            string fileName;
            if (directory == string.Empty)
                fileName = Path.Combine(Const.defaultDirectory, Const.fileNameListDuplicate);
            else
            {
                fileName = Path.Combine(directory, Const.fileNameListDuplicate);
            }
            if (System.IO.File.Exists(fileName))
            {
                try
                {
                    SetStatusDuplicate();
                    ListViewSave listViewItemSave = new ListViewSave();
                    SetStatusDuplicate(LanguageManager.GetString("LoadListLoad") + Const.fileNameListDuplicate);
                    Application.DoEvents();
                    Stream file = new System.IO.FileStream(fileName, FileMode.OpenOrCreate);
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    listViewItemSave = formatter.Deserialize(file) as ListViewSave;
                    file.Close();

                    AllowListChkUpdate = false; // иначе будет запускаться lvDuplicates_ItemChecked
                    lvDuplicates.BeginUpdate();
                    SetStatusDuplicate(LanguageManager.GetString("LoadListLoad") + listViewItemSave.Groups.Count.ToString() + LanguageManager.GetString("LoadListGroups"));
                    progressBar1.Maximum = listViewItemSave.Groups.Count;
                    Application.DoEvents();

                    lvDuplicates.Groups.Clear();
                    lvDuplicates.Items.Clear();
                    foreach (string group in listViewItemSave.Groups)
                    {
                        lvDuplicates.Groups.Add(group, group);
                        progressBar1.PerformStep();
                    }

                    SetStatusDuplicate(LanguageManager.GetString("LoadListLoad") + listViewItemSave.Items.Count.ToString() + LanguageManager.GetString("LoadListItem"));
                    Application.DoEvents();
                    progressBar1.Maximum = listViewItemSave.Items.Count;
                    foreach (ListViewItemSave myItem in listViewItemSave.Items)
                    {
                        ListViewItem lvi = new ListViewItem();
                        lvi.Text = myItem.Text;
                        lvi.Tag = myItem.Text;
                        lvi.Name = myItem.Name;
                        lvi.Checked = myItem.Checked;

                        lvi.Group = lvDuplicates.Groups[myItem.Group];

                        if (myItem.SubItems == null)
                        {
                            MessageBox.Show("Error load list of duplicate" + Environment.NewLine + "Item.SubItems = null", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        }

                        foreach (ListViewItemSaveSubItem subItem in myItem.SubItems)
                        {
                            ListViewItem.ListViewSubItem lvsi = new ListViewItem.ListViewSubItem();
                            lvsi.Text = subItem.Text;
                            lvsi.Name = subItem.Name;
                            lvi.SubItems.Add(lvsi);
                        }
                        lvDuplicates.Items.Add(lvi);

                        size += ulong.Parse(lvi.SubItems["Size"].Text.Replace(" ", string.Empty));

                        progressBar1.PerformStep();
                    }
                    progressBar1.Value = 0;

                    //RemoveMissingFilesFromList
                    if (_settings.Fields.IsCheckNonExistentOnLoad)
                    {
                        SetStatusDuplicate(LanguageManager.GetString("LoadListRemove"));
                        Application.DoEvents();
                        progressBar1.Maximum = lvDuplicates.Items.Count;
                        for (int i = 0; i < lvDuplicates.Items.Count; i++)
                        {
                            string path = Path.Combine(lvDuplicates.Items[i].SubItems["Path"].Text, lvDuplicates.Items[i].SubItems["FileName"].Text);
                            if (!System.IO.File.Exists(path))
                            {
                                string groupName = lvDuplicates.Items[i].Group.Name;

                                lvDuplicates.Items[i].Group.Items.Remove(lvDuplicates.Items[i]);
                                lvDuplicates.Groups[groupName].Items.Remove(lvDuplicates.Items[i]);
                                lvDuplicates.Items[i].Remove();
                                i--;
                                //Если в группе остался 1 файл
                                if (lvDuplicates.Groups[groupName].Items.Count == 1)
                                {
                                    //Удаление группы
                                    lvDuplicates.Items.RemoveAt(lvDuplicates.Groups[groupName].Items[0].Index);
                                    if (i > -1)
                                        i = i - 1;
                                    //lvDuplicates.Groups[groupName].Items.RemoveAt(0);
                                    lvDuplicates.Groups.Remove(lvDuplicates.Groups[groupName]);
                                }
                            }
                            progressBar1.PerformStep();
                        }
                        progressBar1.Value = 0;
                    }

                    //AllCheckedColoring(ref lvDuplicates);

                    SetStatusDuplicate(LanguageManager.GetString("LoadListColoring"));
                    Application.DoEvents();
                    GroupColoring(ref lvDuplicates);

                    SetStatusDuplicate(LanguageManager.GetString("LoadListResize"));
                    Application.DoEvents();
                    for (int u = 0; u < lvDuplicates.Columns.Count; u++)
                        lvDuplicates.Columns[u].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

                    lvDuplicates.EndUpdate();

                    SetStatusDuplicate(lvDuplicates.Items.Count, size, true);
                    AllowListChkUpdate = true;
                    tabControl1.SelectedTab = tabPageDuplicate;
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message);
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
                //splitContainer1.SplitterDistance = _settings.Fields.splitDistance;

                menuMain.Font = cmsDuplicates.Font = cmsDirectorySearch.Font = cmsSelectBy.Font = statusStrip1.Font = this.Font = _settings.Fields.ProgramFont.ToFont();
                lvDuplicates.Font = _settings.Fields.ListRowFont.ToFont();
                checkBoxSameName.Checked = _settings.Fields.IsSameFileName;
                comboBoxIncludeExtension.Text = _settings.Fields.IncludePattern;
                comboBoxExcludeExtension.Text = _settings.Fields.ExcludePattern;
                if (_settings.Fields.IsOrientationVert)
                {
                    splitContainer1.Orientation = Orientation.Vertical;
                    statusStripPicture.Dock = DockStyle.Bottom;
                    toolStripStatusLabel_Width.BorderSides = ToolStripStatusLabelBorderSides.Right;
                }
                else
                {
                    splitContainer1.Orientation = Orientation.Horizontal;
                    statusStripPicture.Dock = DockStyle.Left;
                    toolStripStatusLabel_Width.BorderSides = ToolStripStatusLabelBorderSides.Bottom;
                }
                ParseMinMaxSizesToForm(_settings.Fields.limits);
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings).ShowDialog();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //fFunctions.CancelSearch();
            fFunctions.FolderChangedEvent -= new FileFunctions.FolderChangedDelegate(FolderChangedEventHandler);
            fFunctions.FileCountAvailableEvent -= new FileFunctions.FileCountAvailableDelegate(FileCountCompleteEventHandler);
            fFunctions.FileListAvailableEvent -= new FileFunctions.FileListAvailableDelegate(CompleteFileListAvailableEventHandler);
            fFunctions.DuplicateFileListAvailableEvent -= new FileFunctions.DuplicateFileListAvailableDelegate(DuplicatFileListAvailableEventHandler);
            fFunctions.FileCheckInProgressEvent -= new FileFunctions.FileCheckInProgressDelegate(FileUpdateEventHandler);
            fFunctions.SearchCancelledEvent -= new FileFunctions.SearchCancelledDelegate(SearchCancelledEventHandler);

            if (updateChecker != null)
            {
                updateChecker.VersionChecked -= new VersionManager.UpdateChecker.NewVersionCheckedHandler(versionChecker_NewVersionChecked);
            }

            Save_ListDirectorySearch(_settings.Fields.LastJob);
            Save_ListDirectorySkipped(_settings.Fields.LastJob);

            if (_settings.Fields.IsSaveLoadListDub)
            {
                if (lvDuplicates.Items.Count > 0)
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
                lvisd.IsSubDir = isSubDir;
                lvisd.IsChecked = lvi.Checked;
                listDir.Add(lvisd);
            }

            string filePath;
            if (directory == string.Empty)
            {
                directory = Path.Combine(Application.StartupPath, Const.defaultDirectory);
                filePath = Path.Combine(directory, Const.fileNameDirectorySearch);
            }
            else if (!IsDirectory(directory))
            {
                directory = Path.Combine(Application.StartupPath, directory);
                filePath = Path.Combine(directory, Const.fileNameDirectorySearch);
            }
            else 
            {
                filePath = Const.fileNameDirectorySearch;
            }
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                Stream file = new System.IO.FileStream(filePath, FileMode.OpenOrCreate);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(file, listDir);
                file.Close();
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
            string filePath;
            if (directory == string.Empty)
            {
                directory = Path.Combine(Application.StartupPath, Const.defaultDirectory);
                filePath = Path.Combine(directory, Const.fileNameDirectorySkipped);
            }
            else if (!IsDirectory(directory))
            {
                directory = Path.Combine(Application.StartupPath, directory);
                filePath = Path.Combine(directory, Const.fileNameDirectorySkipped);
            }
            else
            {
                filePath = Const.fileNameDirectorySkipped;
            }
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            try
            {
                Stream file = new System.IO.FileStream(filePath, FileMode.OpenOrCreate);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(file, al);
                file.Close();
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
            ListViewSave myListView;
            myListView = new ListViewSave();
            SetStatusDuplicate(LanguageManager.GetString("SaveList"));
            Application.DoEvents();
            if (lvDuplicates == null)
                new CrashReport("lvDuplicates == null", lvDuplicates).ShowDialog();
            try
            {
                foreach (ListViewGroup group in lvDuplicates.Groups)
                {
                    myListView.Groups.Add(group.Name);
                }
                
                progressBar1.Maximum = lvDuplicates.Items.Count;
                foreach (ListViewItem item in lvDuplicates.Items)
                {
                    ListViewItemSave itemMy = new ListViewItemSave(lvDuplicates.Columns.Count);
                    //ListViewItemSaveSubItemCollection subitemCol = new ListViewItemSaveSubItemCollection(lvDuplicates.Columns.Count);
                    itemMy.Name = item.Name;
                    itemMy.Text = item.Text;
                    itemMy.Checked = item.Checked;
                    itemMy.Group = item.Group.Name;
                    
                    for (int i = 1; i < item.SubItems.Count; i++)
                    {
                        ListViewItemSaveSubItem subItem = new ListViewItemSaveSubItem();
                        
                        subItem.Name = item.SubItems[i].Name;
                        subItem.Text = item.SubItems[i].Text;
                        itemMy.SubItems[i-1] = subItem;
                    }
                    myListView.Items.Add(itemMy);
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;

                string filePath;
                if (directory == string.Empty)
                {
                    directory = Path.Combine(Application.StartupPath, Const.defaultDirectory);
                    filePath = Path.Combine(directory, Const.fileNameListDuplicate);
                }
                else if (!IsDirectory(directory))
                {
                    directory = Path.Combine(Application.StartupPath, directory);
                    filePath = Path.Combine(directory, Const.fileNameListDuplicate);
                }
                else
                    filePath = Const.fileNameListDuplicate;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                Stream file3 = new System.IO.FileStream(filePath, FileMode.Create);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter3 =
                    new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter3.Serialize(file3, myListView);
                file3.Close();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                new CrashReport(ex, _settings, lvDuplicates).ShowDialog();
            }
        }

        /// <summary>
        /// Запись настроек
        /// </summary>
        private void writeSetting()
        {
            /*_settings.Fields.Win_State = this.WindowState;
            _settings.Fields.Win_Left = this.Left;
            _settings.Fields.Win_Top = this.Top;
            _settings.Fields.splitDistance = splitContainer1.SplitterDistance;*/

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
            if (InvokeRequired)
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
        /// <param name="fileName">Filename of file getting processed.</param>
        //delegate in fileFunction public delegate void FileCheckInProgressDelegate(string fileName, int currentCount);
        private delegate void FileCheckUpdateDelegate(string fileName, int currentCount);
        private void FileUpdateEventHandler(string fileName, int currentCount)
        {
            if (InvokeRequired)
            {
                object[] eventArgs = { fileName, currentCount };
                Invoke(new FileCheckUpdateDelegate(FileUpdateEventHandler), eventArgs);
                return;
            }

            //SetStatus("Checking file: '" + fileName + "'");
            statusStrip1.Items[4].Text = LanguageManager.GetString("statusStripSearch5") + fileName;
            progressBar1.Value = currentCount;

            if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1)
                || Environment.OSVersion.Version.Major > 6)
                taskbarProgress.SetProgressValue(this.Handle, Convert.ToUInt64(progressBar1.Value), Convert.ToUInt64(progressBar1.Maximum));


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
            statusStrip1.Items[0].Text = LanguageManager.GetString("statusStripSearch1") + fFunctions.TotalFiles.ToString();
            statusStrip1.Items[1].Text = LanguageManager.GetString("statusStripSearch2") + fFunctions.DuplicateFileCount.ToString();
            //Duplicate group
            statusStrip1.Items[2].Text = LanguageManager.GetString("statusStripSearch3") + fFunctions.DuplicateFileSize.ToString("###,###,###,###,###");

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
            if (InvokeRequired)
            {
                object[] eventArgs = { Count};
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
            if (InvokeRequired)
            {
                object[] eventArgs = {fl};
                Invoke(new CompleteFileListAvailableDelegate(CompleteFileListAvailableEventHandler), eventArgs);
                return;
            }

            //SetStatusSearch(String.Empty);
            SetStatusSearch(LanguageManager.GetString("ComputationChecksum"));
            //SetStatus("File List Compiled");

            _timeStart = DateTime.UtcNow;
            // _lastCount = 0;
            return;
        }


        /// <summary>
        /// All files have been processed. Put listForCompare in duplicate file listForCompare. Добавление дубликатов в lvDuplicate
        /// </summary>
        /// <param name="dl">Arraylist collection of duplicate files.</param>
        private delegate void DuplicatFileListAvailableDelegate(System.Collections.ArrayList dl);
        private void DuplicatFileListAvailableEventHandler(System.Collections.ArrayList dl)
        {
            if (InvokeRequired)
            {
                object[] eventArgs = { dl };
                Invoke(new DuplicatFileListAvailableDelegate(DuplicatFileListAvailableEventHandler), eventArgs);
                return;
            }

            if (dl == null)
            {
                SetStatusSearch(LanguageManager.GetString("statusSearch_CompletedJob")); //Completed Job - No files found
                Controls_Enabled(true);
                return;
            }

            //SetStatusSearch("Adding duplicates to ListView...");
            progressBar1.Value = 0;
            if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1)
                || Environment.OSVersion.Version.Major > 6)
                taskbarProgress.SetProgressState(this.Handle, TBPFLAG.TBPF_NOPROGRESS);

           // long s1, s2, s3, s4, s5;
            //Stopwatch sWatch = new Stopwatch();
            //Debug.WriteLine("Hashtable groups.Add");
            //sWatch.Start(); //любой набор операций (работа с базой данных) 
            Color groupColor = _settings.Fields.ColorRow1.ToColor();
            //System.Collections.Hashtable groups = new System.Collections.Hashtable();
            Dictionary<string, ListViewGroup> groups = new Dictionary<string, ListViewGroup>();
            foreach (ExtendedFileInfo item in dl)
            {
                string subItemText = item.CheckSum;

                //if (!groups.Contains(subItemText))
                if (!groups.ContainsKey(subItemText))
                {
                    //groups.Add(subItemText, new ListViewGroup(subItemText, HorizontalAlignment.Left));
                    groups.Add(subItemText, new ListViewGroup(subItemText, subItemText));
                    lvDuplicates.Groups.Add(subItemText, subItemText);
                    if (groupColor == _settings.Fields.ColorRow2.ToColor())
                        groupColor = _settings.Fields.ColorRow1.ToColor();
                    else
                        groupColor = _settings.Fields.ColorRow2.ToColor();
                }
                Add_LVI(lvDuplicates, item, true, groupColor);
            }
           // sWatch.Stop();
            //Console.WriteLine(sWatch.ElapsedMilliseconds.ToString());
            //Debug.WriteLine("Milliseconds="+sWatch.ElapsedMilliseconds.ToString());
            //s1 = sWatch.ElapsedMilliseconds;
            //TimeSpan tSpan;
            //tSpan = sWatch.Elapsed;
            //Console.WriteLine(tSpan.ToString());
            //Debug.WriteLine("Time spent="+tSpan.ToString());

            //Debug.WriteLine("AutoResize");
            //sWatch.Reset();
            //sWatch.Start();
            //SetStatusDuplicate("AutoResize");
            for (int i = 0; i < lvDuplicates.Columns.Count; i++)
                lvDuplicates.Columns[i].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            //sWatch.Stop();
            //Debug.WriteLine("Milliseconds=" + sWatch.ElapsedMilliseconds.ToString());
            //s2 = sWatch.ElapsedMilliseconds;

            lvDuplicates.EndUpdate();

            /*Debug.WriteLine("SetStatusDuplicate");
            sWatch.Reset();
            sWatch.Start();
            SetStatusDuplicate("SetStatusDuplicate");*/
            SetStatusDuplicate();
            SetStatusDuplicate(fFunctions.DuplicateFileCount, fFunctions.DuplicateFileSize, true);
            /*sWatch.Stop();
            Debug.WriteLine("Milliseconds=" + sWatch.ElapsedMilliseconds.ToString());
            s3 = sWatch.ElapsedMilliseconds;
            MessageBox.Show("s1=" + s1.ToString() + "\n" +
                            "s2=" + s2.ToString() + "\n" +
                            "s3=" + s3.ToString());*/

            System.Media.SystemSounds.Beep.Play();  // Beep

            AllowListChkUpdate = true;
            Controls_Enabled(true);
            tabControl1.SelectedTab = tabPageDuplicate;
        }

        /// <summary>
        /// All files have been processed. Put listForCompare in duplicate file listForCompare.
        /// </summary>
        /// <param name="dl">Arraylist collection of duplicate files.</param>
        private delegate void SearchCancelledDelegate();
        private void SearchCancelledEventHandler()
        {
            if (InvokeRequired)
            {
                Invoke(new SearchCancelledDelegate(SearchCancelledEventHandler));
                return;
            }

            progressBar1.Value = 0;
            SetStatusSearch();
            SetStatusSearch(LanguageManager.GetString("statusSearch_Cancelled"));
            //buttonStart.Text = "Start";
            buttonStart.Text = LanguageManager.GetProperty(this, "buttonStart.Text");
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
                    this.versionInfo = versionInfo;

                    if (showFormVersion)
                    {
                        FormUpdate formUpdate = new FormUpdate();
                        formUpdate.Owner = this;
                        formUpdate.StartPosition = FormStartPosition.CenterParent;
                        formUpdate.Font = _settings.Fields.ProgramFont.ToFont();
                        //versionInfoForm.Icon = Properties.Resources.ICO;
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

        public delegate void UpdateListViewHandler(Font font);
        private void UpdateListView(Font font)
        {
            //this.SuspendLayout();
            lvDuplicates.Font = font;
            //this.ResumeLayout();
        }
        #endregion


        /// <summary>
        /// Start the duplicate file search.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            //_dbManager.Add("G:\\1.txt", DateTime.Today, 1, "c4ca4238a0b923820dcc509a6f75849b"); //constraint failed
            //_dbManager.Update("G:\\1.txt", DateTime.Today, 1, "c4ca4238a0b923820dcc509a6f75849b");
            //throw new ApplicationException("Exception");
            //new CrashReport("Test", _settings).ShowDialog();
            //new CrashReport(new Exception()).ShowDialog();
            // Play more than one files (try to seperate by space)
            //if (buttonStart.Text == "Pause")
            if (buttonStart.Text == LanguageManager.GetString("Pause"))
            {
                fFunctions.PauseSearch();
                //buttonStart.Text = "Resume";
                buttonStart.Text = LanguageManager.GetString("Resume");
                //ttMainForm.SetToolTip(buttonStart, "Resume the search");
                ttMainForm.SetToolTip(buttonStart, LanguageManager.GetString("toolTip_ResumeSearch"));
                if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1)
                    || Environment.OSVersion.Version.Major > 6)
                    taskbarProgress.SetProgressState(this.Handle, TBPFLAG.TBPF_PAUSED);
                return;
            }
            if (buttonStart.Text == LanguageManager.GetString("Resume"))
            {
                fFunctions.ResumeSearch();
                buttonStart.Text = LanguageManager.GetString("Pause");
                if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1)
                || Environment.OSVersion.Version.Major > 6)
                    taskbarProgress.SetProgressState(this.Handle, TBPFLAG.TBPF_NORMAL);
                return;
            }
            if (lvDirectorySearch.CheckedIndices.Count == 0)
            {
                //MessageBox.Show("Not set search directory!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                MessageBox.Show(LanguageManager.GetString("S_NotSetSearchDir"), LanguageManager.GetString("S_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            buttonStart.Text = LanguageManager.GetString("Pause");
            //ttMainForm.SetToolTip(buttonStart, "Pause are search");
            ttMainForm.SetToolTip(buttonStart, LanguageManager.GetString("toolTip_PauseSearch"));

            progressBar1.Value = 0;
            if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1)
                || Environment.OSVersion.Version.Major > 6)
                taskbarProgress.SetProgressValue(this.Handle, Convert.ToUInt64(progressBar1.Value), Convert.ToUInt64(progressBar1.Maximum));

            SetStatusSearch();
            pictureBox1.Image = null;

            Controls_Enabled(false);

            lvDuplicates.Items.Clear();
            lvDuplicates.Groups.Clear();
            AllowListChkUpdate = false;

            fFunctions.Clear_Search_Directory();
            fFunctions.Clear_Skip_Directory();

            //_settings.Fields.SameContent = radioButtonSameContent.Checked;
            //_settings.Fields.DeepSimilarName = uint.Parse(labelSimilarDeepSize.Text);
            _settings.Fields.IncludePattern = comboBoxIncludeExtension.Text;
            _settings.Fields.ExcludePattern = comboBoxExcludeExtension.Text;
            _settings.Fields.limits = ParseMinMaxSizesFromForm();


            //CheckedListBox.CheckedItemCollection cic = checkedListDirectorySearch.CheckedItems;
            /*foreach (string str in cic)
            {
                fFunctions.Add_Search_Directory(str);
            }*/
            for (int i = 0; i < lvDirectorySearch.CheckedItems.Count; i++)
            {
                ListViewItem lvi;
                lvi = lvDirectorySearch.CheckedItems[i];
                string dir = lvi.Text;
                bool isSubDir = false;
                if (lvi.SubItems["SubDir"].Text == LanguageManager.GetString("Yes"))
                    isSubDir = true;
                else if (lvi.SubItems["SubDir"].Text == LanguageManager.GetString("No"))
                    isSubDir = false;
                else
                    new CrashReport("lvDirectorySearch.SubItems[SubDir] not equal Yes or No", _settings, lvDirectorySearch).ShowDialog();


                //System.Diagnostics.Debug.WriteLine("Add directory in fFunctions() " + dir + ", subdir=" + isSubDir);
                fFunctions.Add_Search_Directory(dir, isSubDir);
            }

            CheckedListBox.CheckedItemCollection cicSkip = checkedListBoxSkipFolder.CheckedItems;
            if (cicSkip.Count > 0)
            {
                foreach (string str in cicSkip)
                {
                    fFunctions.Add_Skip_Directory(str);
                }
            }

            //SetStatusSearch("Retrieving directory structure information...");
            SetStatusSearch(LanguageManager.GetString("RetrievingStructure"));
            
            fFunctions.BeginSearch();

            //результаты возврашает через событие DuplicateFileListAvailableDelegate DuplicatFileListAvailableEventHandler(System.Collections.ArrayList dl)
            return;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (buttonStart.Text == LanguageManager.GetString("Resume"))
            {
                fFunctions.ResumeSearch();
            }
            fFunctions.CancelSearch();
            //_cancell = true;

            progressBar1.Value = 0;
            if ((Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor >= 1)
                || Environment.OSVersion.Version.Major > 6)
                taskbarProgress.SetProgressState(this.Handle, TBPFLAG.TBPF_NOPROGRESS);
        }

        #region Main Menu
        private void MainMenuItem_Click_Setting(object sender, EventArgs e)
        {
            FormSetting fs = new FormSetting(this);
            //FormSetting fs = new FormSetting();
            //fs.Owner = this;
            fs.StartPosition = FormStartPosition.CenterParent;
            fs.settings = _settings;
            fs.Font = _settings.Fields.ProgramFont.ToFont();
            fs.Icon = Properties.Resources.SettingIco;
            fs.dbManager = _dbManager;

            fs.UpdateListView += new FormSetting.UpdateListViewHandler(UpdateListView);

            if (fs.ShowDialog() == DialogResult.OK)
            {
                _settings.WriteXml();
            }
            try
            {
                fs.UpdateListView -= new FormSetting.UpdateListViewHandler(UpdateListView);
                fs.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void MainMenuItem_Click_About(object sender, EventArgs e)
        {
            FormAbout ab = new FormAbout();
            ab.Font = _settings.Fields.ProgramFont.ToFont();
            ab.ShowDialog();
            try
            {
                ab.Dispose();
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings).ShowDialog();
            }
        }

        private void MainMenuItem_CheckForUpdate_Click(object sender, EventArgs e)
        {
            toolStripMenuItem_CheckForUpdate.Enabled = false;

            updateChecker = new VersionManager.UpdateChecker(true);
            updateChecker.VersionChecked += new VersionManager.UpdateChecker.NewVersionCheckedHandler(versionChecker_NewVersionChecked);
            //updateChecker.EndVersionChecked += new VersionManager.UpdateChecker.EndVersionCheckedHandler(EndVersionChecked);
        }

        private void MainMenuItem_VersionInfo_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(versionInfo.DownloadWebPageAddress);
        }

        private void MainMenuItem_Click_Horizontal(object sender, EventArgs e)
        {
            splitContainer1.Orientation = Orientation.Horizontal;
            statusStripPicture.Dock = DockStyle.Left;
            toolStripStatusLabel_Width.BorderSides = ToolStripStatusLabelBorderSides.Bottom;
            _settings.Fields.IsOrientationVert = false;
        }

        private void MainMenuItem_Click_Vertical(object sender, EventArgs e)
        {
            splitContainer1.Orientation = Orientation.Vertical;
            statusStripPicture.Dock = DockStyle.Bottom;
            toolStripStatusLabel_Width.BorderSides = ToolStripStatusLabelBorderSides.Right;
            _settings.Fields.IsOrientationVert = true;
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
                panel1.Hide();
                splitContainer1.Dock = DockStyle.Fill;

                /*tsmiDockLeft.Enabled = tsmiDockRight.Enabled = false;
                tsmiDockBottom.Enabled = tsmiDockTop.Enabled = false;
                tsmiResetDock.Enabled = false;*/
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                menuMain.Show();
                panel1.Show();
                splitContainer1.Dock = DockStyle.None;

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
        #endregion

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

        #region Searchable Folders
        /// <summary>
        /// Delete the selected item in the listForCompare.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chklbDirectory_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void buttonDel_Click(object sender, EventArgs e)
        {
            DeleteDirectory(true);
        }

        private void DeleteDirectory(bool FocusOnList)
        {
            if (tabControlFolder.SelectedTab == tabControlFolder.TabPages["tabPageSearchFolder"])
            {
                /*int index = checkedListDirectorySearch.Items.IndexOf(checkedListDirectorySearch.SelectedItem);
                checkedListDirectorySearch.Items.Remove(checkedListDirectorySearch.SelectedItem);
                //Фокусировка на месте удаленного элемента
                if (checkedListDirectorySearch.Items.Count > 0)
                {
                    if (index <= checkedListDirectorySearch.Items.Count - 1)
                    {
                        checkedListDirectorySearch.SelectedIndices.Add(index);
                        //checkedListDirectorySearch.F= checkedListDirectorySearch.Items[index];
                    }
                    else
                    {
                        checkedListDirectorySearch.SelectedIndices.Add(checkedListDirectorySearch.Items.Count - 1);
                    }
                    if (FocusOnList)
                        checkedListDirectorySearch.Focus();
                }*/

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
                        //checkedListDirectorySearch.F= checkedListDirectorySearch.Items[index];
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
            ffs.settings = _settings;
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
                    if (!ListViewContainPath(lvDirectorySearch, ffs.SelectedPath))
                    {
                        //checkedListDirectorySearch.Items.Add(ffs.SelectedPath, true);
                        //Фокусировка на новом элементе
                        //checkedListDirectorySearch.SelectedIndices.Add(checkedListDirectorySearch.Items.Count - 1);

                        ListViewItem.ListViewSubItem lvsi;
                        ListViewItem lvi = new ListViewItem();
                        lvi.Text = ffs.SelectedPath;
                        lvi.Tag = ffs.SelectedPath;
                        lvi.Name = "Directory";
                        lvi.Checked = true;

                        lvsi = new ListViewItem.ListViewSubItem();
                        if (ffs.IsSubDir)
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
                    else
                        MessageBox.Show(LanguageManager.GetString("Directory") + ffs.SelectedPath + Environment.NewLine + LanguageManager.GetString("AlreadyInList"));
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
                new CrashReport(ex, _settings).ShowDialog();
            }
        }

        private bool ListViewContainPath(ListView lvi, string path)
        {
            bool containPath = false;
            for (int i = 0; i < lvi.Items.Count; i++)
            {
                if (String.Equals(lvi.Items[i].Text, path, StringComparison.CurrentCultureIgnoreCase))
                {
                    containPath = true;
                    break;
                }
            }
            return containPath;
        }
        #endregion

        #region "Helper Functions"
        /// <summary>
        /// Enable/Disable the controls that can not be used while the 
        /// search is in progress.
        /// </summary>
        /// <param name="status"></param>
        private void Controls_Enabled(bool status)
        {
            //btnStart.Enabled = status;
            buttonCancel.Enabled = !status;
            lvDirectorySearch.Enabled = status;
            checkedListBoxSkipFolder.Enabled = status;
            lvDuplicates.Enabled = status;
            buttonAddDirectory.Enabled = status;
            buttonEdit.Enabled = status;
            buttonDel.Enabled = status;
            buttonClearAll.Enabled = status;
            buttonClearNonExistent.Enabled = status;

            groupBoxLessThan.Enabled = status;
            groupBoxMoreThan.Enabled = status;
            checkBoxSameName.Enabled = status;
            groupBoxFileFilter.Enabled = status;

            toolStripMenuItem_Settings.Enabled = status;
            toolStripMenuItem_Save.Enabled = status;
            toolStripMenuItem_Load.Enabled = status;

            //ControlBox = status;
            if (status == true)
            {
                //buttonStart.Text = "Start";
                buttonStart.Text = LanguageManager.GetProperty(this, "buttonStart.Text");
                //ttMainForm.SetToolTip(buttonStart, "Starts the search for duplicate files");
                ttMainForm.SetToolTip(buttonStart, LanguageManager.GetString("toolTip_buttonStart"));
            }
            else
            {
                buttonStart.Text = LanguageManager.GetString("Pause");
            }
        }

        /// <summary>
        /// Initialize the listview duplicate
        /// </summary>
        /// <param name="lv"></param>
        private void Set_ListViewItemDupl(ListView lv)
        {
            ColumnHeader colHead;

            colHead = new ColumnHeader();
            //colHead.Text = "File Name";
            colHead.Text = LanguageManager.GetString("ListViewColumn_FileName");
            lv.Columns.Add(colHead);
            lv.Columns[0].Width = 100;

            colHead = new ColumnHeader();
            //colHead.Text = "Path";
            colHead.Text = LanguageManager.GetString("ListViewColumn_Path");
            lv.Columns.Add(colHead);
            lv.Columns[1].Width = 400;

            colHead = new ColumnHeader();
            //colHead.Text = "Size";
            colHead.Text = LanguageManager.GetString("ListViewColumn_Size");
            lv.Columns.Add(colHead);
            lv.Columns[2].Width = 75;
                      
            colHead = new ColumnHeader();
            //colHead.Text = "File Type";
            colHead.Text = LanguageManager.GetString("ListViewColumn_FileType");
            lv.Columns.Add(colHead);
            lv.Columns[3].Width = 100;

            colHead = new ColumnHeader();
            //colHead.Text = "Last Accessed";
            colHead.Text = LanguageManager.GetString("ListViewColumn_LastAccessed");
            lv.Columns.Add(colHead);
            lv.Columns[4].Width = 125;

            colHead = new ColumnHeader();
            //colHead.Text = "MD5 Checksum";
            colHead.Text = LanguageManager.GetString("ListViewColumn_MD5Checksum");
            lv.Columns.Add(colHead);
            lv.Columns[5].Width = 200;

            lv.View = View.Details;
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
            
            lv.View = View.Details;
        }

        /// <summary>
        /// Add extended file info information to listview.
        /// </summary>
        /// <param name="lv">Listview object to add files to</param>
        /// <param name="efi">File to be added</param>
        /// <param name="chkSum">Show checksum value</param>
        private void Add_LVI(ListView lv, ExtendedFileInfo efi, bool chkSum)
        {
            Add_LVI(lv, efi, chkSum, System.Drawing.Color.AliceBlue);
        }

        /// <summary>
        /// Add a new item to the listForCompare
        /// </summary>
        /// <param name="lv">listview to be updated</param>
        /// <param name="efi">File information to be added to the listForCompare view</param>
        /// <param name="chkSum">If true, add the check sum to the listForCompare as well. This is not added
        /// every time since the check sum has not always been calculated in the efi and only needs 
        /// to be calculated if two files have the same size. Big deal on large files</param>
        /// <param name="bgColor">Row color</param>
        private void Add_LVI(ListView lv, ExtendedFileInfo efi, bool chkSum, System.Drawing.Color bgColor)
        {
            lv.BeginUpdate();

            ListViewItem lvi;
            ListViewItem.ListViewSubItem lvsi;

            lvi = new ListViewItem();
            lvi.Text = efi.fileInfo.Name;
            lvi.Tag = efi.fileInfo.Name;
            lvi.BackColor = bgColor ;
            lvi.Name = "FileName";

            lvsi = new ListViewItem.ListViewSubItem();
            lvsi.Text = efi.fileInfo.DirectoryName;
            lvsi.Name = "Path";
            lvi.SubItems.Add(lvsi);

            lvsi = new ListViewItem.ListViewSubItem();
            //lvsi.Text = efi.fileInfo.Length.ToString();
            lvsi.Text = SpaceThousands(efi.fileInfo.Length);
            lvsi.Name = "Size";
            lvi.SubItems.Add(lvsi);
     
            lvsi = new ListViewItem.ListViewSubItem();
            lvsi.Text = efi.fileInfo.Extension.Replace(".", string.Empty).ToUpper() + " File";
            lvsi.Name = "FileType";
            lvi.SubItems.Add(lvsi);

            lvsi = new ListViewItem.ListViewSubItem();
            lvsi.Text = efi.fileInfo.LastAccessTime.ToString();
            lvsi.Name = "LastAccessed";
            lvi.SubItems.Add(lvsi);
            

            lvsi = new ListViewItem.ListViewSubItem();
            lvsi.Text = efi.CheckSum;
            lvsi.Name = "MD5Checksum";
            lvi.SubItems.Add(lvsi);

            //Группы
            lvi.Group = lv.Groups[efi.CheckSum];

            lv.Items.Add(lvi);

            lvi = null;
            lvsi = null;

            lv.EndUpdate();
        }


        private string SpaceThousands(long size)
        {
            System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
            nfi.NumberGroupSeparator = " ";
            nfi.NumberDecimalDigits = 0;
            return size.ToString("N", nfi);
        }

        private void DeleteSelectedItems()
        {
            if (lvDuplicates.Items.Count > 0)
            {
                lvDuplicates.SelectedItems.Clear();
                lvDuplicates.BeginUpdate();
                //Проверка не выделены ли в какой-нибудь группе все файлы
                if (!_settings.Fields.IsAllowDelAllFiles)
                {
                    SetStatusDuplicate(LanguageManager.GetString("CheckAllSelected"));
                    for (int i = 0; i < lvDuplicates.CheckedItems.Count; i++)
                    {
                        int index = lvDuplicates.CheckedItems[i].Index;
                        if (AllChekedInGroups(lvDuplicates.Items[index].Group))
                        {
                            MessageBox.Show(this,
                                LanguageManager.GetString("AllSelected"),
                                LanguageManager.GetString("SelectedFilesDelete"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            lvDuplicates.Items[index].Selected = true;
                            lvDuplicates.EnsureVisible(index);
                            lvDuplicates.EndUpdate();
                            return;
                        }
                    }
                }

                this.Cursor = Cursors.WaitCursor;
                LabelStaStrip5.Text = String.Empty;
                SetStatusDuplicate();
                //progressBar1.Step = 1;
                /*SetStatusDuplicate("Delete checked files");
                Application.DoEvents();
                long s1, s2, s3;
                Stopwatch sWatch = new Stopwatch();
                Debug.WriteLine("Delete from lvDuplicates");
                sWatch.Start();*/
                progressBar1.Maximum = lvDuplicates.CheckedItems.Count;
                for (int i = 0; i < lvDuplicates.CheckedItems.Count; i++)
                {
                    string groupName = lvDuplicates.CheckedItems[i].Group.Name;
                    string spath = Path.Combine(lvDuplicates.CheckedItems[i].SubItems["Path"].Text, lvDuplicates.CheckedItems[i].Text);
                    //System.Diagnostics.Debug.WriteLine(spath);
                    if (System.IO.File.Exists(spath))
                        try
                        {
                            MoveToRecycleBin(spath);

                            //lvDuplicates.CheckedItems[i].Group.Items.Remove(lvDuplicates.CheckedItems[i]);
                            lvDuplicates.Groups[groupName].Items.Remove(lvDuplicates.CheckedItems[i]);
                            lvDuplicates.CheckedItems[i].Remove();
                            i--; //потому что список на 1 вверх уехал
                            //проверка на то что нет дубликатов
                            if (lvDuplicates.Groups[groupName].Items.Count == 1)
                            {
                                //Удаление группы
                                lvDuplicates.Items.RemoveAt(lvDuplicates.Groups[groupName].Items[0].Index);
                                if (i > -1)
                                    i = i - 1;
                                lvDuplicates.Groups[groupName].Items.RemoveAt(0);
                                lvDuplicates.Groups.Remove(lvDuplicates.Groups[groupName]);
                            }
                        }
                        catch (System.UnauthorizedAccessException ex)
                        {
                            //ExtendedFileInfo efi = new ExtendedFileInfo((System.IO.FileInfo)lvsi.Text);
                            //ExtendedFileInfo efi = new ExtendedFileInfo((System.IO.FileInfo)lvsi.Text);
                            //System.IO.FileInfo fi = (ExtendedFileInfo)lvsi.;
                            MessageBox.Show(ex.Message);
                        }
                    else //not exist
                    {
                        //Пометка серым отсутствующих
                        //текущая группа
                        if (lvDuplicates.CheckedItems[i].Group != null)
                        {
                            foreach (ListViewItem item in lvDuplicates.CheckedItems[i].Group.Items)
                            {
                                string path;
                                path = Path.Combine(item.SubItems["Path"].Text, item.SubItems["FileName"].Text);
                                if (!System.IO.File.Exists(path))
                                {
                                    //item.BackColor = System.Drawing.Color.LightGray;
                                    item.BackColor = _settings.Fields.ColorRowNotExist.ToColor();
                                }
                            }
                        }
                        //все группы
                        //foreach (ListViewItem item in lvDuplicates.Items)
                        //{
                        //    if (!System.IO.File.Exists(Path.Combine(item.SubItems["Path"].Text, item.SubItems["FileName"].Text)))
                        //    {
                        //        item.BackColor = System.Drawing.Color.LightGray;
                        //    }
                        //}
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
                /*sWatch.Stop();
                Debug.WriteLine("Milliseconds=" + sWatch.ElapsedMilliseconds.ToString());
                s1 = sWatch.ElapsedMilliseconds;*/

                //SetStatusDuplicate(lvDuplicates.Items.Count);
                /*SetStatusDuplicate("GroupColoring");
                Debug.WriteLine("Coloring and showDuplicate");
                sWatch.Reset();
                sWatch.Start();*/

                GroupColoring(ref lvDuplicates);

                /*sWatch.Stop();
                Debug.WriteLine("Milliseconds=" + sWatch.ElapsedMilliseconds.ToString());
                s2 = sWatch.ElapsedMilliseconds;
                sWatch.Reset();
                sWatch.Start();
                SetStatusDuplicate("showDuplicateInfo");*/

                showDuplicateInfo();
                showDuplicateInfoSelected();

                /*sWatch.Stop();
                Debug.WriteLine("Milliseconds=" + sWatch.ElapsedMilliseconds.ToString());
                s3 = sWatch.ElapsedMilliseconds;
                MessageBox.Show("Delete from lvDuplicates=" + s1.ToString() + "\n" +
                                "GroupColoring=" + s2.ToString() + "\n" +
                                "showDuplicateInfo=" + s3.ToString());*/

                lvDuplicates.EndUpdate();
                this.Cursor = Cursors.Default;

                //CheckLvDublicate();
            }
        }

        private bool AllChekedInGroups(ListViewGroup group)
        {
            Boolean allChecked = true;
            foreach (ListViewItem item in group.Items)
            {
                if (!item.Checked)
                {
                    allChecked = false;
                    break;
                }
            }
            if (allChecked)
            {
                foreach (ListViewItem item in group.Items)
                {
                    item.BackColor = _settings.Fields.ColorRowError.ToColor();
                }
            }
            else
            {
                foreach (ListViewItem item in group.Items)
                {
                    item.BackColor = _settings.Fields.ColorRow1.ToColor();
                }
            }
            return allChecked;
        }

        private void MoveToRecycleBin(string file)
        {
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, 
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                //return true;
            }
            catch (OperationCanceledException ex)
            { }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings, lvDuplicates).ShowDialog();
                //return false;
            }
        }
        #endregion

        #region SetStatus
        /// <summary>
        /// Устанавливает полосу состояния в режим поиска
        /// </summary>
        private void SetStatusSearch()
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

        private void SetStatusSearch(string status)
        {
            statusStrip1.Items[4].Text = status;
        }

        /// <summary>
        /// Устанавливает полосу состояния в режим отображения дубликатов
        /// </summary>
        private void SetStatusDuplicate()
        {
            statusStrip1.Items.Clear();
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
                {
                    this.LabelStaStrip1,
                    this.LabelStaStrip2,
                    this.LabelStaStrip5
                });
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
            ulong dSizes = size;
            string bytesName = String.Empty;
            getSizeAndNameByte(ref dSizes, ref bytesName);
            statusStrip1.Items[1].Text = LanguageManager.GetString("statusStripDubli2") + dSizes.ToString("F03") + bytesName;
            if (ClearSelected)
                statusStrip1.Items[2].Text = String.Empty;
        }

        private void SetStatusDuplicate(int count)
        {
            statusStrip1.Items[0].Text = LanguageManager.GetString("statusStripDubli1") + count;
        }

        /// <summary>
        /// Set the current status in statusStrip
        /// </summary>
        /// <param name="status">Status message</param>
        private void SetStatusDuplicate(string status)
        {
            if (statusStrip1.Items.Count>=2)
                statusStrip1.Items[2].Text = status;
        }
        #endregion

        #region List View Duplicates
        private void cmsDuplicates_Opening(object sender, CancelEventArgs e)
        {
            if (lvDuplicates.Items.Count > 0)
            {
                if (lvDuplicates.FocusedItem.Group != null)
                {
                    if (lvDuplicates.FocusedItem.Group.Items.Count != 2)
                    {
                        //cmsDuplicates.Items["renameFileLikeNeighbourToolStripMenuItem"].Enabled = false;
                        cmsDuplicates.Items["tmsi_Dubli_RenameFileLikeNeighbour"].Visible = false;
                        cmsDuplicates.Items["tmsi_Dubli_MoveSelectedFilesToFolder"].Visible = false;
                    }
                    else
                    {
                        cmsDuplicates.Items["tmsi_Dubli_RenameFileLikeNeighbour"].Visible = true;
                        if (
                            String.Compare(lvDuplicates.FocusedItem.Group.Items[0].SubItems["Path"].Text,
                                           lvDuplicates.FocusedItem.Group.Items[1].SubItems["Path"].Text, true) == 0)
                            cmsDuplicates.Items["tmsi_Dubli_MoveSelectedFilesToFolder"].Visible = false;
                        else
                            cmsDuplicates.Items["tmsi_Dubli_MoveSelectedFilesToFolder"].Visible = true;
                    }
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
            string filePreview;
            ListViewItem lvi;
            ListViewItem.ListViewSubItem lvsi;
            lvi = lvDuplicates.FocusedItem;
            if (lvi != null)
            {
                lvsi = lvi.SubItems["Path"];
                filePreview = lvsi.Text;
                lvsi = lvi.SubItems["FileName"];
                filePreview += "\\" + lvsi.Text;

                try
                {
                    if (System.IO.File.Exists(filePreview))
                        System.Diagnostics.Process.Start(filePreview);
                }
                catch (System.ArgumentException ae)
                {
                    MessageBox.Show(ae.Message);
                    //return;
                }
            }
            //return;
        }

        private void lvDuplicates_SelectedIndexChanged(object sender, EventArgs e)
        {
            string filePreview;
            ListViewItem lvi;
            //ListViewItem.ListViewSubItem lvsi;
            lvi = lvDuplicates.FocusedItem;
            if (lvi != null)
            {
                filePreview = Path.Combine(lvi.SubItems["Path"].Text, lvi.SubItems["FileName"].Text);
                //filePreview += "\\" + lvsi.Text;
                try
                {
                    if (System.IO.File.Exists(filePreview))
                    {
                        if (String.Compare(Path.GetExtension(filePreview), ".gif", true) == 0)
                        {   //Animated gif
                            Image gifImage = Image.FromFile(filePreview);
                            System.Drawing.Imaging.FrameDimension dimension = new System.Drawing.Imaging.FrameDimension(gifImage.FrameDimensionsList[0]);
                            //int frameCount = gifImage.GetFrameCount(dimension);
                            //gifImage.SelectActiveFrame(dimension, 0);
                            if (gifImage.GetFrameCount(dimension) > 1)
                            {
                                pictureBox1.Image = new Bitmap((Bitmap)gifImage);
                            }
                            else
                            {
                                pictureBox1.Load(filePreview);
                            }
                            gifImage.Dispose();
                        }
                        else
                        {
                            pictureBox1.Load(filePreview);
                            /*Image image = Image.FromFile(filePreview);
                            pictureBox1.Image = image;*/
                        }
                        statusStripPicture.Visible = true;
                        toolStripStatusLabel_Width.Text = pictureBox1.Image.Width.ToString();
                        toolStripStatusLabel_Height.Text = pictureBox1.Image.Height.ToString();
                    }
                    else
                    {
                        //текущая группа
                        if (lvi.Group != null)
                        {
                            foreach (ListViewItem item in lvi.Group.Items)
                            {
                                string path;
                                path = Path.Combine(item.SubItems["Path"].Text, item.SubItems["FileName"].Text);
                                if (!System.IO.File.Exists(path))
                                {
                                    item.BackColor = System.Drawing.Color.LightGray;
                                }
                            }
                        }//*/
                        //все группы
                        /*foreach (ListViewItem item in lvDuplicates.Items)
                        {
                            if (
                                !System.IO.File.Exists(Path.Combine(item.SubItems["Path"].Text,
                                                                    item.SubItems["FileName"].Text)))
                            {
                                item.BackColor = _settings.Fields.ColorRowNotExist.ToColor();
                            }
                        }*/
                        statusStripPicture.Visible = false;
                    }
                    //pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                    //pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                    //System.Diagnostics.Process.Start(filePreview);
                    
                }
                catch (System.ArgumentException)
                {
                    //MessageBox.Show(ae.Message);
                    //pictureBox1.Dispose();
                    pictureBox1.Image = null;
                    toolStripStatusLabel_Width.Text = String.Empty;
                    toolStripStatusLabel_Height.Text = String.Empty;
                    statusStripPicture.Visible = false;
                    return;
                }
            }
            return;
        }
        #endregion

        #region Context Menu Duplicate
        /// <summary>
        /// Контекстное меню дубликатов - Выбрать все кроме одного
        /// </summary>
        private void tmsi_Dubli_SelectAllButOne_Click(object sender, EventArgs e)
        {   
            string prevCheckSum = null;
            string curCheckSum = null;
            ListViewItem lvi;
            ListViewItem.ListViewSubItem lvsi;

            //выделенные только обрабатываем
            AllowListChkUpdate = false;
            Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
            if (lvDuplicates.SelectedIndices.Count > 1)
            {
                progressBar1.Maximum = lvDuplicates.SelectedItems.Count;
                for (int i = 0; i < lvDuplicates.SelectedItems.Count; i++)
                {
                    lvDuplicates.SelectedItems[i].Checked = false;

                    lvsi = lvDuplicates.SelectedItems[i].SubItems["MD5Checksum"];
                    curCheckSum = lvsi.Text;

                    if (prevCheckSum != null)
                        if (prevCheckSum == curCheckSum)
                        {
                            lvDuplicates.SelectedItems[i].Checked = true;
                            lvDuplicates.SelectedItems[i].Font = fontStrikeout;
                        }

                    prevCheckSum = curCheckSum;
                    progressBar1.PerformStep();
                }
            }
            else
            {
                progressBar1.Maximum = lvDuplicates.Items.Count;
                for (int i = 0; i < lvDuplicates.Items.Count; i++)
                {
                    lvDuplicates.Items[i].Checked = false;

                    lvi = lvDuplicates.Items[i];
                    lvsi = lvi.SubItems["MD5Checksum"];

                    curCheckSum = lvsi.Text;

                    if (prevCheckSum != null)
                        if (prevCheckSum == curCheckSum)
                        {
                            lvDuplicates.Items[i].Checked = true;
                            lvDuplicates.Items[i].Font = fontStrikeout;
                        }

                    prevCheckSum = curCheckSum;
                    progressBar1.PerformStep();
                }
            }
            progressBar1.Value = 0;
            AllowListChkUpdate = true;
            showDuplicateInfoSelected();
        }

        private void buttonDeleteSelectedFiles_Click(object sender, EventArgs e)
        {
            DeleteSelectedFiles();
        }

        /// <summary>
        /// Удалить отмеченные записи
        /// </summary>
        private void tmsi_Dubli_DeleteSelectedItems_Click(object sender, EventArgs e)
        {
            DeleteSelectedFiles();
        }

        private void DeleteSelectedFiles()
        {
            if (_settings.Fields.IsConfirmDelete)
            {
                //if (MessageBox.Show("Are you sure?", "Warning", MessageBoxButtons.OKCancel) == DialogResult.OK)
                if (MessageBox.Show(LanguageManager.GetString("YouSure"), LanguageManager.GetString("Warning"), MessageBoxButtons.OKCancel) == DialogResult.OK)
                    DeleteSelectedItems();
            }
            else
                DeleteSelectedItems(); 
        }

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать все
        /// </summary>
        private void tmsi_Dubli_SelectAll_Click(object sender, EventArgs e)
        {
            AllowListChkUpdate = false;
            Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
            //выделенные только обрабатываем
            if (lvDuplicates.SelectedIndices.Count > 1)
            {
                progressBar1.Maximum = lvDuplicates.SelectedIndices.Count;
                for (int i = 0; i < lvDuplicates.SelectedItems.Count; i++)
                {
                    lvDuplicates.SelectedItems[i].Checked = true;
                    lvDuplicates.SelectedItems[i].Font = fontStrikeout;
                    progressBar1.PerformStep();
                }
            }
            else
            {
                progressBar1.Maximum = lvDuplicates.Items.Count;
                for (int i = 0; i < lvDuplicates.Items.Count; i++)
                {
                    lvDuplicates.Items[i].Checked = true;
                    lvDuplicates.Items[i].Font = fontStrikeout;
                    progressBar1.PerformStep();
                }
            }

            progressBar1.Value = 0;
            if (!_settings.Fields.IsAllowDelAllFiles)
                AllCheckedColoring(ref lvDuplicates);
            AllowListChkUpdate = true;
            showDuplicateInfoSelected();
        }

        /// <summary>
        /// Контекстное меню дубликатов - Сбросить все
        /// </summary>
        private void tmsi_Dubli_DeSelectAll_Click(object sender, EventArgs e)
        {
            AllowListChkUpdate = false;
            Font fontRegular = new Font(lvDuplicates.Font, FontStyle.Regular);
            //выделенные только обрабатываем
            if (lvDuplicates.SelectedIndices.Count > 1)
            {
                progressBar1.Maximum = lvDuplicates.SelectedIndices.Count;
                for (int i = 0; i < lvDuplicates.SelectedItems.Count; i++)
                {
                    lvDuplicates.SelectedItems[i].Checked = false;
                    lvDuplicates.SelectedItems[i].Font = fontRegular;
                    progressBar1.PerformStep();
                }
            }
            else
            {
                progressBar1.Maximum = lvDuplicates.Items.Count;
                for (int i = 0; i < lvDuplicates.Items.Count; i++)
                {
                    lvDuplicates.Items[i].Checked = false;
                    //lvDuplicates.Items[i].BackColor = Color.Empty;
                    lvDuplicates.Items[i].Font = fontRegular;
                    progressBar1.PerformStep();
                }
            }
            progressBar1.Value = 0;
            if (!_settings.Fields.IsAllowDelAllFiles)
                AllCheckedColoring(ref lvDuplicates);
            GroupColoring(ref lvDuplicates);
            AllowListChkUpdate = true;
            showDuplicateInfoSelected();
        }

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

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать все в этой папке
        /// </summary>
        private void tmsi_Dubli_SelectAllInThisFolder_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                AllowListChkUpdate = false;
                //lvDuplicates.BeginUpdate();

                //Font fontStrikeout = new Font(lvDuplicates.FocusedItem.Font, FontStyle.Strikeout);
                Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
                progressBar1.Maximum = lvDuplicates.Items.Count;
                string path = lvDuplicates.FocusedItem.SubItems["Path"].Text;
                foreach (ListViewItem item in lvDuplicates.Items)
                {
                    if (item.SubItems["Path"].Text == path)
                    {
                        item.Checked = true;
                        item.Font = fontStrikeout;
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;

                AllowListChkUpdate = true;
                //lvDuplicates.EndUpdate();
                showDuplicateInfoSelected();
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать все в этой папке (в группах с этими папками)
        /// </summary>
        private void tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem != null && lvDuplicates.FocusedItem.Group != null)
            {
                AllowListChkUpdate = false;
                //lvDuplicates.BeginUpdate();

                //Font fontStrikeout = new Font(lvDuplicates.FocusedItem.Font, FontStyle.Strikeout);
                Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
                List<string> listForCompare = new List<string>(lvDuplicates.FocusedItem.Group.Items.Count);

                progressBar1.Maximum = lvDuplicates.Groups.Count;
                //foreach (ListViewItem item in lvDuplicates.FocusedItem.Group.Items)
                foreach (ListViewItem item in lvDuplicates.Groups[lvDuplicates.FocusedItem.Group.Name].Items)
                {
                    listForCompare.Add(item.SubItems["path"].Text);
                }

                //проверяем каждую группу на наличие ВСЕХ записей из искомой
                foreach (ListViewGroup group in lvDuplicates.Groups)
                {
                    if (group.Items.Count >= listForCompare.Count)
                    {
                        bool check = true;
                        foreach (string compared in listForCompare) //в искомой
                        {
                            bool groupApproaches = false;
                            foreach (ListViewItem item in group.Items) //в текущей
                            {
                                if (String.Compare(compared, item.SubItems["path"].Text) == 0)
                                {
                                    groupApproaches = true;
                                    break;
                                }
                            }
                            //если не найдена то пропускаем
                            if (!groupApproaches)
                            {
                                check = false; //группа не подходит
                                break; //выход из общего цикла
                            }

                        }
                        if (check)
                        {
                            foreach (ListViewItem item in group.Items)
                            {
                                if (item.SubItems["path"].Text == lvDuplicates.FocusedItem.SubItems["path"].Text)
                                {
                                    item.Checked = true;
                                    item.Font = fontStrikeout;
                                }
                            }
                        }
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
                if (!_settings.Fields.IsAllowDelAllFiles)
                    AllCheckedColoring(ref lvDuplicates);
                GroupColoring(ref lvDuplicates);
                AllowListChkUpdate = true;
                //lvDuplicates.EndUpdate();
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
                    if (_settings.Fields.IsConfirmDelete)
                    {
                        if (MessageBox.Show(LanguageManager.GetString("YouSure"), LanguageManager.GetString("Warning"), MessageBoxButtons.OKCancel) == DialogResult.OK)
                            DeleteItem(false);
                    }
                    else
                        DeleteItem(false);
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    fileOpen();
                }

                else if (e.KeyData == (Keys.Control | Keys.C))
                {
                    string name = lvDuplicates.FocusedItem.SubItems["FileName"].Text;
                    if (!String.IsNullOrEmpty(name))
                    {
                        Clipboard.SetDataObject(name);
                    }
                }
                else if (e.KeyData == (Keys.Control | Keys.V))
                {
                    if (Clipboard.ContainsText())
                    {
                        string name = Clipboard.GetText();
                        if (String.Compare(name, lvDuplicates.FocusedItem.SubItems["FileName"].Text) != 0)
                        {
                            string fileName = Path.Combine(lvDuplicates.FocusedItem.SubItems["Path"].Text,
                                                           lvDuplicates.FocusedItem.Text);
                            string destFileName = Path.Combine(lvDuplicates.FocusedItem.SubItems["Path"].Text, name);
                            try
                            {
                                new FileInfo(fileName).MoveTo(destFileName);
                                lvDuplicates.FocusedItem.SubItems["FileName"].Text = name;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
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
            string name = lvDuplicates.FocusedItem.SubItems["Path"].Text;
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
                if (String.IsNullOrEmpty(label) | (label == lvDuplicates.FocusedItem.Text))
                {
                    e.CancelEdit = true;
                }
                else if (lvDuplicates.Items.Count > 0)
                {
                    string fileName = Path.Combine(lvDuplicates.FocusedItem.SubItems["Path"].Text, lvDuplicates.FocusedItem.Text);
                    //string fileName = lvDuplicates.FocusedItem.SubItems["Path"].Text + lvDuplicates.FocusedItem.Text;
                    string destFileName = Path.Combine(lvDuplicates.FocusedItem.SubItems["Path"].Text, label);
                    try
                    {
                        new FileInfo(fileName).MoveTo(destFileName);
                    }
                    catch (Exception exception1)
                    {
                        MessageBox.Show(exception1.Message);
                        e.CancelEdit = true;
                    }
                }
                lvDuplicates.LabelEdit = false;
            }
            catch (Exception ex)
            {
                //Interaction.MsgBox("Error in Rename: " + exception2.Message, MsgBoxStyle.OkOnly, null);
                //MessageBox.Show("Error in Rename: " + exception3.Message);
                MessageBox.Show(LanguageManager.GetString("ErrorRename") + ex.Message);
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Переместить файл к соседу
        /// </summary>
        private void tmsi_Dubli_MoveFileToNeighbour_Click(object sender, EventArgs e)
        {
            ListViewItem lvi = lvDuplicates.FocusedItem;
            if (lvi != null)
            {
                string sourcePath = Path.Combine(lvi.SubItems["Path"].Text, lvi.SubItems["FileName"].Text);
                string targetPath = null;
                string targetFolder = null;
                string item1 = lvi.Group.Items[0].SubItems["Path"].Text;
                string item2 = lvi.Group.Items[1].SubItems["Path"].Text;
                if (String.Compare(lvi.SubItems["Path"].Text, item1, true) == 0)
                    targetFolder = item2;
                else
                    targetFolder = item1;
                targetPath = Path.Combine(targetFolder, lvi.SubItems["FileName"].Text);

                if (System.IO.File.Exists(sourcePath))
                    try
                    {
                        if (!System.IO.File.Exists(targetPath))
                        {
                            new FileInfo(sourcePath).MoveTo(targetPath);
                        }
                        else
                        {
                            targetPath = MoveAndRename(sourcePath, targetPath);
                        }
                        lvi.SubItems["Path"].Text = targetFolder;
                        lvi.SubItems["FileName"].Text = Path.GetFileName(targetPath);
                        //Update Text
                    }
                    catch (System.UnauthorizedAccessException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Удалить отсутствующие файлы из списка
        /// </summary>
        private void tmsi_Dubli_RemoveMissingFilesFromList_Click(object sender, EventArgs e)
        {
            RemoveMissingFilesFromList();
        }

        private void RemoveMissingFilesFromList()
        {
            bool changed = false;
            this.Cursor = Cursors.WaitCursor;
            lvDuplicates.BeginUpdate();
            progressBar1.Maximum = lvDuplicates.Items.Count;
            //foreach (ListViewItem item in lvDuplicates.Items)
            for (int i = 0; i < lvDuplicates.Items.Count; i++)
            {
                string path;
                path = Path.Combine(lvDuplicates.Items[i].SubItems["Path"].Text, lvDuplicates.Items[i].SubItems["FileName"].Text);
                if (!System.IO.File.Exists(path))
                {
                    changed = true;
                    string groupName = lvDuplicates.Items[i].Group.Name;

                    lvDuplicates.Items[i].Group.Items.Remove(lvDuplicates.Items[i]);
                    lvDuplicates.Groups[groupName].Items.Remove(lvDuplicates.Items[i]);
                    //lvDuplicates.Groups[groupName].Items.RemoveAt(lvDuplicates.Items[i].Index); 
                    lvDuplicates.Items[i].Remove();
                    i--;
                    //lvDuplicates.Items.Remove(item);
                    //Если в группе остался 1 файл
                    if (lvDuplicates.Groups[groupName].Items.Count == 1)
                    {
                        //Удаление группы
                        /*foreach (ListViewItem itemGroup in lvDuplicates.Groups[groupName].Items)
                        {
                            itemGroup.Remove();
                        }*/
                        lvDuplicates.Items.RemoveAt(lvDuplicates.Groups[groupName].Items[0].Index);
                        if (i > -1)
                            i = i - 1;
                        lvDuplicates.Groups[groupName].Items.RemoveAt(0);
                        lvDuplicates.Groups.Remove(lvDuplicates.Groups[groupName]);
                    }

                    //lvDuplicates.Groups.Remove(item.Group);
                    //foreach (ListViewItem itemGroup in item.Group.Items)
                    /*string groupName = item.Group.Name;
                    foreach (ListViewItem itemGroup in lvDuplicates.Groups[groupName].Items)
                    {
                        itemGroup.Remove();
                    }
                    lvDuplicates.Groups.Remove(lvDuplicates.Groups[groupName]);*/
                }
                progressBar1.PerformStep();
            }
            progressBar1.Value = 0;

            if (changed)
            {
                //Sorting items by Group
                /*for (int i = 0; i < lvDuplicates.Groups.Count; i++)
                {
                    foreach (ListViewItem item in lvDuplicates.Groups[i].Items)
                    {
                        lvDuplicates.Items.RemoveByKey(item.Text);
                        lvDuplicates.Items.IndexOfKey(item.Text);//Insert(itemIndex, item);
                        //itemIndex++;
                    }
                }*/

                GroupColoring(ref lvDuplicates);
                //SetStatusDuplicate(lvDuplicates.Items.Count);
                showDuplicateInfo();
                showDuplicateInfoSelected();
            }
            lvDuplicates.EndUpdate();
            this.Cursor = Cursors.Default;
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
            ListViewItem lvi = lvDuplicates.FocusedItem;

            if (lvi != null)
            {
                string sourceName = lvi.SubItems["FileName"].Text;
                string sourceFolder = lvi.SubItems["Path"].Text;
                string sourcePath = Path.Combine(sourceFolder, sourceName);
                string targetName = null;
                string targetPath = null;
                string item1 = lvi.Group.Items[0].SubItems["FileName"].Text;
                string item2 = lvi.Group.Items[1].SubItems["FileName"].Text;
                if (String.Compare(sourceName, item1, true) == 0)
                    targetName = item2;
                else
                    targetName = item1;
                targetPath = Path.Combine(sourceFolder, targetName);

                try
                {
                    if (!System.IO.File.Exists(targetPath))
                    {
                        new FileInfo(sourcePath).MoveTo(targetPath);
                    }
                    else
                    {
                        targetPath = MoveAndRename(sourcePath, targetPath);
                    }
                    //Update Text
                    //lvi.SubItems["Path"].Text = targetFolder;
                    lvi.SubItems["FileName"].Text = Path.GetFileName(targetPath);
                    //вместо Undo
                    Clipboard.SetDataObject(sourcePath);
                }
                catch (System.UnauthorizedAccessException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (_settings.Fields.IsConfirmDelete)
            {
                if (MessageBox.Show(LanguageManager.GetString("YouSure"), LanguageManager.GetString("Warning"), MessageBoxButtons.OKCancel) == DialogResult.OK)
                    DeleteItem(true);
            }
            else
                DeleteItem(true);
        }

        private void DeleteItem(bool FocusOnList)
        {
            if (lvDuplicates.FocusedItem != null)
            {
                //ListViewItem lvi = lvDuplicates.FocusedItem;
                int index = lvDuplicates.FocusedItem.Index;
                //System.Diagnostics.Debug.Write("FocusedItem=" + lvDuplicates.FocusedItem.Text + "\n");
                //System.Diagnostics.Debug.Write(index + " (" + lvi.Text + ")  +1=" + lvDuplicates.Items[index + 1].Text + "+2= " + lvDuplicates.Items[index + 2].Text + "\n");
                string groupName = lvDuplicates.FocusedItem.Group.Name;
                //lvi = lvDuplicates.Items[i];

                //lvsi = lvi.SubItems["Path"];
                String path = Path.Combine(lvDuplicates.FocusedItem.SubItems["Path"].Text, lvDuplicates.FocusedItem.SubItems["FileName"].Text);
                //String path = Path.Combine(lvi.SubItems["Path"].Text, lvi.Name.ToString());
                if (System.IO.File.Exists(path))
                    try
                    {
                        MoveToRecycleBin(path);
                        //lvDuplicates.BeginUpdate();
                        //lvDuplicates.Items[lvi].Remove();
                        lvDuplicates.FocusedItem.Group.Items.Remove(lvDuplicates.FocusedItem);
                        lvDuplicates.Groups[groupName].Items.Remove(lvDuplicates.FocusedItem);
                        lvDuplicates.Items.Remove(lvDuplicates.FocusedItem);
                        //проверка на то что нет дубликатов
                        if (lvDuplicates.Groups[groupName].Items.Count == 1)
                        {

                            //Удаление группы
                            lvDuplicates.Items.RemoveAt(lvDuplicates.Groups[groupName].Items[0].Index);
                            lvDuplicates.Groups[groupName].Items.RemoveAt(0);

                            lvDuplicates.Groups.Remove(lvDuplicates.Groups[groupName]);

                            GroupColoring(ref lvDuplicates);
                            //SetStatusDuplicate(lvDuplicates.Items.Count);
                            showDuplicateInfo();
                            showDuplicateInfoSelected();
                            //index++;
                        }
                        //lvDuplicates.EndUpdate();
                        //Фокусировка на месте удаленного элемента
                        if (lvDuplicates.Items.Count > 0)
                        {
                            //System.Diagnostics.Debug.Write("Select lvDuplicates[" + index + "]=" + lvDuplicates.Items[index].Text +"\n");
                            //lvDuplicates.SelectedItems(index);
                            if (index <= lvDuplicates.Items.Count - 1)
                            {
                                lvDuplicates.SelectedIndices.Add(index);
                                //lvDuplicates.Focus();
                                //lvDuplicates.EnsureVisible(lvDuplicates.SelectedIndices[0]);
                                lvDuplicates.FocusedItem = lvDuplicates.Items[index];
                            }
                            else
                            {
                                lvDuplicates.SelectedIndices.Add(lvDuplicates.Items.Count - 1);
                            }
                            if (FocusOnList)
                                lvDuplicates.Focus();
                        }

                        //CheckLvDublicate();
                    }
                    catch (System.UnauthorizedAccessException ex)
                    {
                        MessageBox.Show(ex.Message);
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
            if (lvDuplicates.Items.Count > 0)
            {
                FormFolderSelect ffs = new FormFolderSelect();
                ffs.Owner = this;
                ffs.StartPosition = FormStartPosition.CenterParent;
                ffs.settings = _settings;
                //ffs.Text = "Move to Folder";
                ffs.Text = LanguageManager.GetString("MoveFolder");
                ffs.Font = _settings.Fields.ProgramFont.ToFont();
                ffs.Icon = Properties.Resources.movetoIco;

                /*if (!String.IsNullOrEmpty(_settings.Fields.FolderToMove))
                {
                    ffs.SelectedPath = _settings.Fields.FolderToMove;
                }*/
                if (lvDuplicates.FocusedItem != null)
                {
                    ffs.SelectedPath = lvDuplicates.FocusedItem.SubItems["Path"].Text;
                }

                if (ffs.ShowDialog() == DialogResult.OK)
                {
                    ListViewItem lvi;
                    //ListViewItem.ListViewSubItem lvsi;
                    string selectedPath = ffs.SelectedPath;
                    progressBar1.Maximum = lvDuplicates.CheckedItems.Count;
                    for (int i = 0; i < lvDuplicates.CheckedItems.Count; i++)
                    {
                        lvi = lvDuplicates.CheckedItems[i];
                        string groupName = lvi.Group.Name;

                        string sourceFolder = lvi.SubItems["Path"].Text;
                        string sourcePath = Path.Combine(sourceFolder, lvi.Text);
                        if (System.IO.File.Exists(sourcePath))
                        {
                            if (!Directory.Exists(selectedPath))
                                Directory.CreateDirectory(selectedPath);

                            string targetPath = Path.Combine(selectedPath, lvi.Text);
                            try
                            {
                                if (!System.IO.File.Exists(targetPath))
                                {
                                    new FileInfo(sourcePath).MoveTo(targetPath);
                                }
                                else
                                {
                                    targetPath = MoveAndRename(sourcePath, targetPath);
                                }

                                if (IsContainSearchPath(selectedPath))
                                {
                                    //Edit Label Path
                                    lvi.Text = Path.GetFileName(targetPath);
                                    lvi.SubItems["Path"].Text = Directory.GetParent(targetPath).ToString();
                                }
                                else
                                {
                                    //Delete Item
                                    lvDuplicates.CheckedItems[i].Group.Items.Remove(lvi);
                                    lvDuplicates.Groups[groupName].Items.Remove(lvi);
                                    lvDuplicates.CheckedItems[i].Remove();
                                    i--;
                                    //lvDuplicates.Items.Remove(lvi);
                                    //проверка на то что нет дубликатов
                                    if (lvDuplicates.Groups[groupName].Items.Count == 1)
                                    {
                                        //Удаление группы
                                        lvDuplicates.Items.RemoveAt(lvDuplicates.Groups[groupName].Items[0].Index);
                                        if (i > -1)
                                            i = i - 1;
                                        lvDuplicates.Groups[groupName].Items.RemoveAt(0);
                                        lvDuplicates.Groups.Remove(lvDuplicates.Groups[groupName]);
                                    }
                                    GroupColoring(ref lvDuplicates);
                                    showDuplicateInfo();
                                    showDuplicateInfoSelected();
                                }
                            }
                            catch (System.UnauthorizedAccessException ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                        progressBar1.PerformStep();
                    }
                    progressBar1.Value = 0;
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
       

        private bool IsContainSearchPath(string moveS)
        {
            bool ContainSearchPath = false;
            char[] split = new char[] { Path.DirectorySeparatorChar };
            for (int i = 0; i < lvDirectorySearch.CheckedItems.Count; i++)
            {
                string[] search = lvDirectorySearch.CheckedItems[i].ToString().Split(split, StringSplitOptions.RemoveEmptyEntries);
                string[] move = moveS.Split(split, StringSplitOptions.RemoveEmptyEntries);

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

        public class sortByGroup : System.Collections.IComparer
        {
            /// <summary>
            /// Determine the larger of two files
            /// </summary>
            /// <param name="fi1"></param>
            /// <param name="fi2"></param>
            /// <returns></returns>
            int System.Collections.IComparer.Compare(object object1, object object2)
            {
                ExtendedFileInfo efi1 = (ExtendedFileInfo)object1;
                ExtendedFileInfo efi2 = (ExtendedFileInfo)object2;
                return (int)string.Compare(efi1.CheckSum, efi2.CheckSum);
            }
        }

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
            if (SetSortIcon(lvDuplicates, lvwGroupSorter.SortColumn,
                lvwGroupSorter.Order)) return;

            if (lvwGroupSorter.SortColumn < 0)
            {
                System.Diagnostics.Debug.Assert(lvDuplicates.ListViewItemSorter == null);
            }

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

                if ((ch.Index == lvwGroupSorter.SortColumn) &&
                    (lvwGroupSorter.Order != SortOrder.None))
                {
                    if (lvwGroupSorter.Order == SortOrder.Ascending)
                        strNew = strCur + strAsc;
                    else if (lvwGroupSorter.Order == SortOrder.Descending)
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
            if (e.Column == lvwGroupSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwGroupSorter.Order == SortOrder.Ascending)
                {
                    lvwGroupSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwGroupSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwGroupSorter.SortColumn = e.Column;
                lvwGroupSorter.Order = SortOrder.Ascending;
            }

            this.Cursor = Cursors.WaitCursor;

            //Sorting in group Сортировка внутри групп
            lvDuplicates.BeginUpdate();
            //сортировщик Item-ов
            ListViewItemSorter listViewItemSorter = new ListViewItemSorter();
            List<ListViewItem> listListViewItem = new List<ListViewItem>();
            listViewItemSorter.Order = SortOrder.Ascending;
            listViewItemSorter.SortColumn = e.Column;
            //Sort in group
            progressBar1.Maximum = lvDuplicates.Groups.Count;
            foreach (ListViewGroup lvg in lvDuplicates.Groups)
            {
                foreach (ListViewItem item in lvg.Items)
                {
                    listListViewItem.Add(item);
                }
                lvg.Items.Clear();
                listListViewItem.Sort(listViewItemSorter);
                lvg.Items.AddRange(listListViewItem.ToArray());
                listListViewItem.Clear();
                progressBar1.PerformStep();
            }
            progressBar1.Value = 0;

            List<ListViewGroup> listListViewGroup = new List<ListViewGroup>(lvDuplicates.Groups.Count);
            for (int i = 0; i < lvDuplicates.Groups.Count; ++i)
            {
                listListViewGroup.Add(lvDuplicates.Groups[i]);
            }
            //lvDuplicates.Items.Clear();
            lvDuplicates.Groups.Clear();
            listListViewGroup.Sort(lvwGroupSorter);

            foreach (ListViewGroup listViewGroup in listListViewGroup)
            {
                lvDuplicates.Groups.Add(listViewGroup);
            }

            //RedrawItems
            //lvDuplicates.Clear();
            int itemIndex = 0;
            ListViewGroup group;
            progressBar1.Maximum = lvDuplicates.Groups.Count;
            for(int i = 0; i<lvDuplicates.Groups.Count;i++)
            {
                group = new ListViewGroup(lvDuplicates.Groups[i].Name, lvDuplicates.Groups[i].Header);
                foreach (ListViewItem item in lvDuplicates.Groups[i].Items)
                {
                    lvDuplicates.Items.RemoveAt(item.Index);
                    //int y = lvDuplicates.Items.IndexOf(item);
                    //lvDuplicates.Items.RemoveAt(lvDuplicates.Items.IndexOf(item));
                    //lvDuplicates.Items.Remove(item); //медленно
                    //lvDuplicates.Items.RemoveByKey(item.Name);
                    item.Group = group;
                    lvDuplicates.Items.Insert(itemIndex, item);
                    itemIndex++;
                }
                progressBar1.PerformStep();
            }
            progressBar1.Value = 0;

            GroupColoring(ref lvDuplicates);
            UpdateColumnSortingIcons();

            //lvDuplicates.ShowGroups = false;
            lvDuplicates.EndUpdate();

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

        private void AllCheckedColoring(ref ListView listView)
        {
            foreach (ListViewGroup group in listView.Groups)
            {
                bool allChecked = true;
                foreach (ListViewItem item in group.Items)
                {
                    if (!item.Checked)
                    {
                        allChecked = false;
                        break;
                    }
                }
                if (allChecked)
                {
                    foreach (ListViewItem item in group.Items)
                    {
                        item.BackColor = _settings.Fields.ColorRowError.ToColor();
                    }
                }
                else
                {
                    foreach (ListViewItem item in group.Items)
                    {
                        item.BackColor = _settings.Fields.ColorRow1.ToColor();
                    }
                }
            }
        }

        private void GroupColoring(ref ListView listView)
        {
            bool prevBlue = false;
            string lastHash = String.Empty;
            for (int i = 0; i < listView.Items.Count; i++)
            {
                if (String.Compare(listView.Items[i].SubItems["MD5Checksum"].Text, lastHash) != 0)
                {
                    //новый цвет
                    if (prevBlue)
                        prevBlue = false;
                    else
                        prevBlue = true;
                    lastHash = listView.Items[i].SubItems["MD5Checksum"].Text;
                }
                if (prevBlue)
                {
                    if ((listView.Items[i].BackColor != _settings.Fields.ColorRowError.ToColor()) && (listView.Items[i].BackColor != _settings.Fields.ColorRowNotExist.ToColor()))
                    {
                        listView.Items[i].BackColor = _settings.Fields.ColorRow1.ToColor();
                    }
                }
                else
                    if ((listView.Items[i].BackColor != _settings.Fields.ColorRowError.ToColor()) && (listView.Items[i].BackColor != _settings.Fields.ColorRowNotExist.ToColor()))
                    {

                        listView.Items[i].BackColor = _settings.Fields.ColorRow2.ToColor();
                    }
            }
        }

        private void lvDuplicates_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (AllowListChkUpdate)
            {
                //Зачернкнутый шрифт
                Font fontRegular = new Font(e.Item.Font, FontStyle.Regular);
                Font fontStrikeout = new Font(e.Item.Font, FontStyle.Strikeout);

                if (e.Item.Checked)
                {
                    e.Item.Font = fontStrikeout;
                    //проверка не выделены ли все файлы в группе
                    if (!_settings.Fields.IsAllowDelAllFiles) //если разрешено удаление то не проверяем 
                    {
                        if (e.Item.Group != null)
                        {
                            bool allChecked = true;
                            foreach (ListViewItem item in e.Item.Group.Items)
                            {
                                if (!item.Checked)
                                {
                                    allChecked = false;
                                    break;
                                }
                            }
                            if (allChecked)
                            {
                                foreach (ListViewItem item in e.Item.Group.Items)
                                {
                                    item.BackColor = _settings.Fields.ColorRowError.ToColor();
                                }
                            }
                        }
                    }
                }
                else
                {
                    e.Item.Font = fontRegular;
                    if (e.Item.BackColor == _settings.Fields.ColorRowError.ToColor())
                    {
                        e.Item.BackColor = _settings.Fields.ColorRow1.ToColor();
                        if (e.Item.Group != null)
                        {
                            foreach (ListViewItem item in e.Item.Group.Items)
                            {
                                item.BackColor = _settings.Fields.ColorRow1.ToColor();
                            }
                        }
                        GroupColoring(ref lvDuplicates);
                    }
                }
                showDuplicateInfoSelected();
            }//if (AllowListChkUpdate)
        }
        #endregion
      
        #region Rename file
        /// <summary>
        /// Переместить файл с переименованием
        /// </summary>
        private string MoveAndRename(String sourcePath, String targetPath)
        {
            ulong dig = 0;
            string digname = String.Empty;
            int leadingZero = 0;

            dig = GetDigit(Path.GetFileNameWithoutExtension(targetPath), out digname);

            if (dig == 0)
                targetPath = GetNewNameForFileAdd(targetPath, 2);
            else
                targetPath = GetNewNameForFileDig(Path.Combine(Directory.GetParent(targetPath).ToString() + "\\", digname),
                                leadingZero,
                                dig + 1,
                                Path.GetExtension(targetPath),
                                targetPath);
            new FileInfo(sourcePath).MoveTo(targetPath);
            return targetPath;
        }

        /// <summary>
        /// Проверка есть ли в имени файла число отделенное "_". Возврашает число.
        /// </summary>
        /// <param name="name">имя файла</param>
        /// <param name="digname">имя файла без числа и "_"</param>
        /// <returns>0 или число полученное</returns>
        private ulong GetDigit(string name, out string digname)
        {
            int len = name.Length;
            int sym = name.LastIndexOf('_');
            bool ren = true;
            for (int u = sym + 1; u < len; u++)
                if (!char.IsDigit(name[u]))
                    ren = false;

            ulong result = 0;
            if (ren)
            {
                string intPar = name.Substring(sym + 1);
                ulong.TryParse(intPar, out result);
            }

            digname = name.Substring(0, sym + 1);
            return result;
        }

        /// <summary>
        /// Добавление к имени файла числа в случае когда в нем его не было
        /// </summary>
        /// <param name="oldname">Старое имя</param>
        /// <param name="i">Число</param>
        /// <returns>Новое имя</returns>
        private string GetNewNameForFileAdd(string oldname, ulong i)
        {
            string newname = string.Format("{0}\\{1}_{2}{3}", Directory.GetParent(oldname).ToString(), Path.GetFileNameWithoutExtension(oldname), i, Path.GetExtension(oldname));
            if (File.Exists(newname))
            {
                i = i + 1;
                newname = GetNewNameForFileAdd(oldname, i);
            }
            return newname;
        }

        /// <summary>
        ///  Добавление к имени файла числа в случае когда в нем было число, с лидирующими нулями
        /// </summary>
        /// <param name="oldname">Старое имя</param>
        /// <param name="zero">Количество лидирующих нулей</param>
        /// <param name="i">Число</param>
        /// <param name="ext">Расширение файла</param>
        /// <param name="sourceName">Исходное имя</param>
        /// <returns></returns>
        private string GetNewNameForFileDig(string oldname, int zero, ulong i, string ext, string sourceName)
        {
            string newname = String.Empty;
            StringBuilder builder = new StringBuilder(oldname);
            for (int j = 0; j < zero; j++)
                builder.Append("0");
            builder.Append(i);
            builder.Append(ext);
            newname = builder.ToString();

            if (File.Exists(newname))
            {
                newname = GetNewNameForFileAdd(sourceName, 2);
            }
            return newname;
        }
        #endregion

        private void showDuplicateInfo()
        {
            bool ClearSelected = false;
            ulong nSizes = 0;

            if (!_settings.Fields.IsDontUpdateSize)
            {
                foreach (ListViewItem item in lvDuplicates.Items)
                {
                    nSizes += ulong.Parse(item.SubItems["Size"].Text.Replace(" ", string.Empty));
                }
                SetStatusDuplicate(lvDuplicates.Items.Count, nSizes, ClearSelected);
            }
            else
            {
                SetStatusDuplicate(lvDuplicates.Items.Count);
            }
        }

        private void showDuplicateInfoSelected()
        {
            ulong nSizes = 0;
            string bytesName = null;
            ulong dSizes = 0;

            if (!_settings.Fields.IsDontUpdateSize)
            {
                foreach (ListViewItem item in lvDuplicates.CheckedItems)
                {
                    nSizes += ulong.Parse(item.SubItems["Size"].Text.Replace(" ", string.Empty));
                    //nSizes += double.Parse(item.SubItems["Size"].Text.Replace(" ", string.Empty));
                }
                dSizes = nSizes;
                getSizeAndNameByte(ref dSizes, ref bytesName);
                SetStatusDuplicate(lvDuplicates.CheckedItems.Count.ToString() +
                    LanguageManager.GetString("DuplicateFilesTotalSize") +
                    dSizes.ToString("F03") + bytesName);
            }
            else
            {
                SetStatusDuplicate(lvDuplicates.CheckedItems.Count.ToString() +
                    LanguageManager.GetString("DuplicateFilesTotalSize"));
            }
        }

        /// <summary>
        /// Переводит числовое представление размера в округленное и с соответствующим текстом единицы измерения
        /// </summary>
        /// <param name="size">Размер</param>
        /// <param name="sbytes">Еденица измерения</param>
        private void getSizeAndNameByte(ref ulong size, ref string sbytes)
        {
            if (size < 1024)
            {
                //sbytes = " Byte";
                sbytes = LanguageManager.GetProperty(this, "rdbLBytes.Text");
            }
            else if (size < (1024*1024))
            {
                size = size / 1024;
                //sbytes = " Kb";
                sbytes = " " + LanguageManager.GetProperty(this, "rdbLKilo.Text");
            }
            else if (size < (1024*1024*1024))
            {
                size = size / (1024 * 1024);
                //sbytes = " Mb";
                sbytes = " " + LanguageManager.GetProperty(this, "rdbLMega.Text");
            }
            else
            {
                size = size / (1024 * 1024 * 1024);
                //sbytes = " Gb";
                sbytes = " " + LanguageManager.GetProperty(this, "rdbLGiga.Text");
            }
        }

        private void buttonSelectBy_Click(object sender, EventArgs e)
        {
            cmsSelectBy.Show(buttonSelectBy, 5,5);
        }

        private void tmsi_Select_ByNewestFilesInEachGroup_Click(object sender, EventArgs e)
        {
            DateTime smallDate, currentDate;
            AllowListChkUpdate = false;
            Font fontRegular = new Font(lvDuplicates.Font, FontStyle.Regular);
            Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
            if (lvDuplicates.SelectedIndices.Count > 1)
            {
                string prevGroup = String.Empty;
                progressBar1.Maximum = lvDuplicates.SelectedItems.Count;
                for (int j = 0; j < lvDuplicates.SelectedItems.Count; j++)
                {
                    if (lvDuplicates.SelectedItems[j].Group.Name != prevGroup)
                    {
                        smallDate = Convert.ToDateTime(
                                lvDuplicates.SelectedItems[j].Group.Items[0].SubItems["LastAccessed"].Text);
                        lvDuplicates.SelectedItems[j].Group.Items[0].Checked = false;
                        lvDuplicates.SelectedItems[j].Group.Items[0].Font = fontRegular;
                        int index = 0;
                        for (int i = 1; i < lvDuplicates.SelectedItems[j].Group.Items.Count; i++)
                        {
                            lvDuplicates.SelectedItems[j].Group.Items[i].Checked = false;
                            lvDuplicates.SelectedItems[j].Group.Items[i].Font = fontRegular;
                            currentDate =
                                Convert.ToDateTime(
                                    lvDuplicates.SelectedItems[j].Group.Items[i].SubItems["LastAccessed"].Text);
                            if (DateTime.Compare(currentDate, smallDate) < 0)
                            {
                                smallDate = currentDate;
                                index = i;
                            }
                        }
                        for (int i = 0; i < lvDuplicates.SelectedItems[j].Group.Items.Count; i++)
                        {
                            if (i != index)
                            {
                                lvDuplicates.SelectedItems[j].Group.Items[i].Checked = true;
                                lvDuplicates.SelectedItems[j].Group.Items[i].Font = fontStrikeout;
                            }
                        }
                        prevGroup = lvDuplicates.SelectedItems[j].Group.Name;
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
            else
            {
                progressBar1.Maximum = lvDuplicates.Groups.Count;
                foreach (ListViewGroup group in lvDuplicates.Groups)
                {
                    smallDate = Convert.ToDateTime(group.Items[0].SubItems["LastAccessed"].Text);
                    group.Items[0].Checked = false;
                    group.Items[0].Font = fontRegular;
                    int index = 0;
                    for (int i = 1; i < group.Items.Count; i++)
                    {
                        group.Items[i].Checked = false;
                        group.Items[i].Font = fontRegular;
                        currentDate = Convert.ToDateTime(group.Items[i].SubItems["LastAccessed"].Text);
                        if (DateTime.Compare(currentDate, smallDate) < 0)
                        {
                            smallDate = currentDate;
                            index = i; //самый старый
                        }
                    }
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        if (i != index)
                        {
                            group.Items[i].Checked = true;
                            group.Items[i].Font = fontStrikeout;
                        }
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
            AllowListChkUpdate = true;
            showDuplicateInfoSelected();
        }

        private void tmsi_Select_ByOldestFileInEachGroup_Click(object sender, EventArgs e)
        {
            DateTime bigDate, currentDate;
            AllowListChkUpdate = false;
            Font fontRegular = new Font(lvDuplicates.Font, FontStyle.Regular);
            Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
            if (lvDuplicates.SelectedIndices.Count > 1)  //выделенные
            {
                string prevGroup = String.Empty;
                progressBar1.Maximum = lvDuplicates.SelectedItems.Count;
                for (int j = 0; j < lvDuplicates.SelectedItems.Count; j++)
                {
                    if (lvDuplicates.SelectedItems[j].Group.Name != prevGroup)
                    {
                        bigDate = Convert.ToDateTime(lvDuplicates.SelectedItems[j].Group.Items[0].SubItems["LastAccessed"].Text);
                        lvDuplicates.SelectedItems[j].Group.Items[0].Checked = false;
                        lvDuplicates.SelectedItems[j].Group.Items[0].Font = fontRegular;
                        int index = 0;
                        for (int i = 1; i < lvDuplicates.SelectedItems[j].Group.Items.Count; i++)
                        {
                            lvDuplicates.SelectedItems[j].Group.Items[i].Checked = false;
                            currentDate = Convert.ToDateTime(lvDuplicates.SelectedItems[j].Group.Items[i].SubItems["LastAccessed"].Text);
                            if (DateTime.Compare(currentDate, bigDate) > 0)
                            {
                                bigDate = currentDate;
                                index = i;
                            }
                        }
                        for (int i = 0; i < lvDuplicates.SelectedItems[j].Group.Items.Count; i++)
                        {
                            if (i != index)
                            {
                                lvDuplicates.SelectedItems[j].Group.Items[i].Checked = true;
                                lvDuplicates.SelectedItems[j].Group.Items[i].Font = fontStrikeout;
                            }
                        }
                        prevGroup = lvDuplicates.SelectedItems[j].Group.Name;
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
            else
            {
                progressBar1.Maximum = lvDuplicates.Groups.Count;
                foreach (ListViewGroup group in lvDuplicates.Groups)
                {
                    bigDate = Convert.ToDateTime(group.Items[0].SubItems["LastAccessed"].Text);
                    group.Items[0].Checked = false;
                    group.Items[0].Font = fontRegular;
                    int index = 0;
                    for (int i = 1; i < group.Items.Count; i++)
                    {
                        group.Items[i].Checked = false;
                        group.Items[i].Font = fontRegular;
                        currentDate = Convert.ToDateTime(group.Items[i].SubItems["LastAccessed"].Text);
                        if (DateTime.Compare(currentDate, bigDate) > 0)
                        {
                            bigDate = currentDate;
                            index = i;
                        }
                    }
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        if (i != index)
                        {
                            group.Items[i].Checked = true;
                            group.Items[i].Font = fontStrikeout;
                        }
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
            progressBar1.Value = 0;
            AllowListChkUpdate = true;
            showDuplicateInfoSelected();
        }

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать по дате - Старый файл в каждой группе
        /// </summary>
        private void tmsi_Dubli_SelectByDateOldestFiles_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem.Group != null)
            {
                List<string> list = new List<string>(lvDuplicates.FocusedItem.Group.Items.Count);
                //Font fontStrikeout = new Font(lvDuplicates.FocusedItem.Font, FontStyle.Strikeout);
                //Font fontRegular = new Font(lvDuplicates.FocusedItem.Font, FontStyle.Regular);
                Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
                Font fontRegular = new Font(lvDuplicates.Font, FontStyle.Regular);
                foreach (ListViewItem item in lvDuplicates.FocusedItem.Group.Items)
                {
                    list.Add(item.SubItems["path"].Text);
                }

                progressBar1.Maximum = lvDuplicates.Groups.Count;
                foreach (ListViewGroup group in lvDuplicates.Groups)
                {
                    if (group.Items.Count >= list.Count)
                    {
                        bool check = true;
                        foreach (ListViewItem item in group.Items)
                        {
                            bool checkGroup = false;
                            if (list.Contains(item.SubItems["path"].Text))
                            {
                                checkGroup = true;
                            }
                            if (!checkGroup)
                            {
                                check = false; //группа не подходит
                                break; //выход из общего цикла
                            }
                        }

                        if (check)
                        {
                            DateTime comparDate, currentDate;
                            AllowListChkUpdate = false;

                            comparDate = Convert.ToDateTime(group.Items[0].SubItems["LastAccessed"].Text);
                            group.Items[0].Checked = true;
                            int index = 0;
                            for (int i = 1; i < group.Items.Count; i++)
                            {
                                group.Items[i].Checked = true;
                                group.Items[i].Font = fontStrikeout;
                                currentDate = Convert.ToDateTime(group.Items[i].SubItems["LastAccessed"].Text);
                                if (DateTime.Compare(currentDate, comparDate) > 0)
                                {
                                    comparDate = currentDate;
                                    index = i;
                                }
                            }
                            group.Items[index].Checked = false;
                            group.Items[index].Font = fontRegular;

                            AllowListChkUpdate = true;
                            showDuplicateInfoSelected();
                        }
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
        }

        /// <summary>
        /// Контекстное меню дубликатов - Выбрать по дате - Новый файл в каждой группе
        /// </summary>
        private void tmsi_Dubli_SelectByDateNewestFiles_Click(object sender, EventArgs e)
        {
            if (lvDuplicates.FocusedItem.Group != null)
            {
                List<string> list = new List<string>(lvDuplicates.FocusedItem.Group.Items.Count);
                //Font fontStrikeout = new Font(lvDuplicates.FocusedItem.Font, FontStyle.Strikeout);
                //Font fontRegular = new Font(lvDuplicates.FocusedItem.Font, FontStyle.Regular);
                Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
                Font fontRegular = new Font(lvDuplicates.Font, FontStyle.Regular);
                foreach (ListViewItem item in lvDuplicates.FocusedItem.Group.Items)
                {
                    list.Add(item.SubItems["path"].Text);
                }

                progressBar1.Maximum = lvDuplicates.Groups.Count;
                foreach (ListViewGroup group in lvDuplicates.Groups)
                {
                    if (group.Items.Count >= list.Count)
                    {
                        bool check = true;
                        foreach (ListViewItem item in group.Items)
                        {
                            bool checkGroup = false;
                            if (list.Contains(item.SubItems["path"].Text))
                            {
                                checkGroup = true;
                            }
                            if (!checkGroup)
                            {
                                check = false; //группа не подходит
                                break; //выход из общего цикла
                            }
                        }

                        if (check)
                        {
                            DateTime comparDate, currentDate;
                            AllowListChkUpdate = false;

                            comparDate = Convert.ToDateTime(group.Items[0].SubItems["LastAccessed"].Text);
                            group.Items[0].Checked = true;
                            int index = 0;
                            for (int i = 1; i < group.Items.Count; i++)
                            {
                                group.Items[i].Checked = true;
                                group.Items[i].Font = fontStrikeout;
                                currentDate = Convert.ToDateTime(group.Items[i].SubItems["LastAccessed"].Text);
                                if (DateTime.Compare(currentDate, comparDate) < 0)
                                {
                                    comparDate = currentDate;
                                    index = i;
                                }
                            }
                            group.Items[index].Checked = false;
                            group.Items[index].Font = fontRegular;

                            AllowListChkUpdate = true;
                            showDuplicateInfoSelected();
                        }
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            fileOpen();
        }

        private void buttonMove_Click(object sender, EventArgs e)
        {
            MoveToFolder();
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
                Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);

                if (lvDuplicates.SelectedIndices.Count > 1) //в выбранных
                {
                    AllowListChkUpdate = false;
                    progressBar1.Maximum = lvDuplicates.SelectedItems.Count;
                    for (int i = 0; i < lvDuplicates.SelectedItems.Count; i++)
                    {
                        if (lvDuplicates.SelectedItems[i].SubItems["FileName"].Text.ToLower().Contains(ffns.SelectedName))
                        {
                            lvDuplicates.SelectedItems[i].Checked = true;
                            lvDuplicates.SelectedItems[i].Font = fontStrikeout;
                        }
                        progressBar1.PerformStep();
                    }
                    progressBar1.Value = 0;
                    if (!_settings.Fields.IsAllowDelAllFiles)
                        AllCheckedColoring(ref lvDuplicates);
                    GroupColoring(ref lvDuplicates);
                    AllowListChkUpdate = true;
                    showDuplicateInfoSelected();
                }
                else
                {
                    AllowListChkUpdate = false;
                    progressBar1.Maximum = lvDuplicates.Items.Count;
                    for (int i = 0; i < lvDuplicates.Items.Count; i++)
                    {
                        if (lvDuplicates.Items[i].SubItems["FileName"].Text.ToLower().Contains(ffns.SelectedName))
                        {
                            lvDuplicates.Items[i].Checked = true;
                            lvDuplicates.Items[i].Font = fontStrikeout;
                        }
                        progressBar1.PerformStep();
                    }
                    progressBar1.Value = 0;
                    if (!_settings.Fields.IsAllowDelAllFiles)
                        AllCheckedColoring(ref lvDuplicates);
                    GroupColoring(ref lvDuplicates);
                    AllowListChkUpdate = true;
                    showDuplicateInfoSelected();
                }
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

        private void tmsi_Select_ByShorterFileNameInEachGroup_Click(object sender, EventArgs e)
        {
            string shortName, currentName;
            AllowListChkUpdate = false;
            Font fontRegular = new Font(lvDuplicates.Font, FontStyle.Regular);
            Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
            if (lvDuplicates.SelectedIndices.Count > 1) //в выбранных
            {
                string prevGroup = String.Empty;
                progressBar1.Maximum = lvDuplicates.SelectedItems.Count;
                for (int i = 0; i < lvDuplicates.SelectedItems.Count; i++)
                {
                    if (lvDuplicates.SelectedItems[i].Group.Name != prevGroup)
                    {
                        shortName = lvDuplicates.SelectedItems[i].Group.Items[0].SubItems["FileName"].Text;
                        lvDuplicates.SelectedItems[i].Group.Items[0].Checked = false;
                        lvDuplicates.SelectedItems[i].Group.Items[0].Font = fontRegular;
                        int index = 0;
                        for (int j = 1; j < lvDuplicates.SelectedItems[i].Group.Items.Count; j++)
                        {
                            lvDuplicates.SelectedItems[i].Group.Items[j].Checked = false;
                            currentName = lvDuplicates.SelectedItems[i].Group.Items[j].SubItems["FileName"].Text;
                            if (shortName.Length < currentName.Length)
                            {
                                shortName = currentName;
                                index = j;
                            }
                        }
                        for (int j = 0; j < lvDuplicates.SelectedItems[i].Group.Items.Count; j++)
                        {
                            if (j != index)
                            {
                                lvDuplicates.SelectedItems[i].Group.Items[j].Checked = true;
                                lvDuplicates.SelectedItems[i].Group.Items[j].Font = fontStrikeout;
                            }
                        }

                        prevGroup = lvDuplicates.SelectedItems[i].Group.Name;
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
            else //во всех
            {
                progressBar1.Maximum = lvDuplicates.Groups.Count;
                foreach (ListViewGroup group in lvDuplicates.Groups)
                {
                    shortName = group.Items[0].SubItems["FileName"].Text;
                    group.Items[0].Checked = false;
                    group.Items[0].Font = fontRegular;
                    int index = 0;
                    for (int i = 1; i < group.Items.Count; i++)
                    {
                        group.Items[i].Checked = false;
                        group.Items[i].Font = fontRegular;
                        currentName = group.Items[i].SubItems["FileName"].Text;
                        if (shortName.Length < currentName.Length)
                        {
                            shortName = currentName;
                            index = i;
                        }
                    }
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        if (i != index)
                        {
                            group.Items[i].Checked = true;
                            group.Items[i].Font = fontStrikeout;
                        }
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
            AllowListChkUpdate = true;
            showDuplicateInfoSelected();
        }

        private void tmsi_Select_ByLongerFileNameInEachGroup_Click(object sender, EventArgs e)
        {
            string longerName, currentName;
            Font fontRegular = new Font(lvDuplicates.Font, FontStyle.Regular);
            Font fontStrikeout = new Font(lvDuplicates.Font, FontStyle.Strikeout);
            AllowListChkUpdate = false;
            //выделенные только обрабатываем
            if (lvDuplicates.SelectedIndices.Count > 1)
            {
                string prevGroup = String.Empty;
                progressBar1.Maximum = lvDuplicates.SelectedItems.Count;
                for (int i = 0; i < lvDuplicates.SelectedItems.Count; i++)
                {
                    if (lvDuplicates.SelectedItems[i].Group.Name != prevGroup)
                    {
                        longerName = lvDuplicates.SelectedItems[i].Group.Items[0].SubItems["FileName"].Text;
                        lvDuplicates.SelectedItems[i].Group.Items[0].Checked = false;
                        lvDuplicates.SelectedItems[i].Group.Items[0].Font = fontRegular;
                        int index = 0;
                        for (int j = 1; j < lvDuplicates.SelectedItems[i].Group.Items.Count; j++)
                        {
                            lvDuplicates.SelectedItems[i].Group.Items[j].Checked = false;
                            currentName = lvDuplicates.SelectedItems[i].Group.Items[j].SubItems["FileName"].Text;
                            if (longerName.Length > currentName.Length)
                            {
                                longerName = currentName;
                                index = j;
                            }
                        }
                        for (int j = 0; j < lvDuplicates.SelectedItems[i].Group.Items.Count; j++)
                        {
                            if (j != index)
                            {
                                lvDuplicates.SelectedItems[i].Group.Items[j].Checked = true;
                                lvDuplicates.SelectedItems[i].Group.Items[j].Font = fontStrikeout;
                            }
                        }

                        prevGroup = lvDuplicates.SelectedItems[i].Group.Name;
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
            else
            {
                progressBar1.Maximum = lvDuplicates.Groups.Count;
                foreach (ListViewGroup group in lvDuplicates.Groups)
                {
                    longerName = group.Items[0].SubItems["FileName"].Text;
                    group.Items[0].Checked = false;
                    group.Items[0].Font = fontRegular;
                    int index = 0;
                    for (int i = 1; i < group.Items.Count; i++)
                    {
                        group.Items[i].Checked = false;
                        group.Items[i].Font = fontRegular;
                        currentName = group.Items[i].SubItems["FileName"].Text;
                        if (longerName.Length > currentName.Length)
                        {
                            longerName = currentName;
                            index = i;
                            //group.Items[i].Checked = false;
                        }
                    }
                    for (int i = 0; i < group.Items.Count; i++)
                    {
                        if (i != index)
                        {
                            group.Items[i].Checked = true;
                            group.Items[i].Font = fontStrikeout;
                        }
                    }
                    progressBar1.PerformStep();
                }
                progressBar1.Value = 0;
            }
            AllowListChkUpdate = true;
            showDuplicateInfoSelected();
        }

        /// <summary>
        /// Контекстное меню дубликатов - Удалить группу из списка
        /// </summary>
        private void tmsi_Dubli_DeleteGroup_Click(object sender, EventArgs e)
        {
            //AllowListChkUpdate = false;
            lvDuplicates.BeginUpdate();
            if (lvDuplicates.SelectedIndices.Count > 1)
            {
                List<string> group = new List<string>(lvDuplicates.SelectedItems.Count);
                for (int j = 0; j < lvDuplicates.SelectedItems.Count; j++)
                {
                    if (!group.Contains(lvDuplicates.SelectedItems[j].Group.Name))
                    {
                        group.Add(lvDuplicates.SelectedItems[j].Group.Name);
                    }
                }
                progressBar1.Maximum = group.Count;
                for (int i = 0; i < group.Count; i++)
                {
                    for (int k = 0; k < lvDuplicates.Groups[group[i]].Items.Count; k++)
                    {
                        lvDuplicates.Items.RemoveAt(lvDuplicates.Groups[group[i]].Items[k].Index);
                    }
                    lvDuplicates.Groups.Remove(lvDuplicates.Groups[group[i]]);
                    progressBar1.PerformStep();
                }

                progressBar1.Value = 0;
                showDuplicateInfo();
                showDuplicateInfoSelected();
                GroupColoring(ref lvDuplicates);
            }
            else
            {
                if (lvDuplicates.FocusedItem.Group != null)
                {
                    string groupName = lvDuplicates.FocusedItem.Group.Name;

                    for (int i = 0; i < lvDuplicates.Groups[groupName].Items.Count; i++)
                    {
                        lvDuplicates.Items.RemoveAt(lvDuplicates.Groups[groupName].Items[i].Index);
                        //lvDuplicates.Groups[groupName].Items[i].Remove();
                    }
                    //lvDuplicates.Items.RemoveAt(lvDuplicates.Groups[groupName].Items[0].Index);
                    //lvDuplicates.Groups[groupName].Items.RemoveAt(0);
                    lvDuplicates.Groups.Remove(lvDuplicates.Groups[groupName]);

                    showDuplicateInfo();
                    showDuplicateInfoSelected();
                    GroupColoring(ref lvDuplicates);
                }
            }
            lvDuplicates.EndUpdate();
        }

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

                FormSetting fs = new FormSetting();
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

            //buttonDelete.Invalidate();
            SizeF size = TextRenderer.MeasureText(buttonDelete.Text, buttonDelete.Font);
            buttonDelete.Width = (int)size.Width + buttonDelete.Image.Width + 12;

            buttonDeleteSelectedFiles.Left = buttonDelete.Right + 20;
            size = TextRenderer.MeasureText(buttonDeleteSelectedFiles.Text, buttonDeleteSelectedFiles.Font);
            buttonDeleteSelectedFiles.Width = (int)size.Width + buttonDeleteSelectedFiles.Image.Width + 12;

            buttonMove.Left = buttonDeleteSelectedFiles.Right + 20;
            size = TextRenderer.MeasureText(buttonMove.Text, buttonMove.Font);
            buttonMove.Width = (int)size.Width + buttonMove.Image.Width + 12;

            buttonSelectBy.Left = buttonMove.Right + 20;
            size = TextRenderer.MeasureText(buttonSelectBy.Text, buttonSelectBy.Font);
            buttonSelectBy.Width = (int)size.Width + buttonSelectBy.Image.Width + 12;

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
                SetStatusDuplicate();
                showDuplicateInfo();
            }
            else
            {
                SetStatusSearch();
            }
        }

        private void LocalizeLVDirectorySearch()
        {
            foreach(ListViewItem lv in lvDirectorySearch.Items)
            {
                //object o = lv.SubItems["SubDir"].Tag; //["SubDir"].Tag;
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

        private void FormMain_FontChanged(object sender, EventArgs e)
        {
            menuMain.Font = cmsDuplicates.Font = cmsDirectorySearch.Font = cmsSelectBy.Font = statusStrip1.Font = this.Font;
        }

        public void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            new CrashReport("ThreadException", e.Exception, _settings, lvDuplicates).ShowDialog();
        }

        private void toolStripMenuItem_Save_Click(object sender, EventArgs e)
        {
            FormFileNameSelect ffns = new FormFileNameSelect();
            ffns.Owner = this;
            ffns.StartPosition = FormStartPosition.CenterParent;
            ffns.Font = _settings.Fields.ProgramFont.ToFont();
            ffns.Icon = Properties.Resources.TerminatorIco32;
            //ffns._settings = _settings;
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
            if (IsDirectory(_settings.Fields.LastJob))
                fjl.SelectedJob = _settings.Fields.LastJob;

            //fjl._settings = _settings;

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

        private void buttonEdit_Click(object sender, EventArgs e)
        {
            EditDirectory();
        }

        private void EditDirectory()
        {
            FormFolderSelect ffs = new FormFolderSelect();
            ffs.Owner = this;
            ffs.StartPosition = FormStartPosition.CenterParent;
            ffs.settings = _settings;
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

                            if (!ListViewContainPath(lvDirectorySearch, path))
                            {
                                ListViewItem.ListViewSubItem lvsi;
                                ListViewItem lvi = new ListViewItem();
                                lvi.Text = path;
                                lvi.Tag = path;
                                lvi.Name = "Directory";
                                lvi.Checked = true;

                                lvsi = new ListViewItem.ListViewSubItem();
                                lvsi.Text = LanguageManager.GetString("Yes");
                                lvsi.Tag = 1;
                                lvsi.Name = "SubDir";
                                lvi.SubItems.Add(lvsi);

                                lvDirectorySearch.Items.Add(lvi);

                                lvi = null;
                                lvsi = null;
                            }
                            else
                                MessageBox.Show(LanguageManager.GetString("Directory") + path + Environment.NewLine + LanguageManager.GetString("AlreadyInList"));
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


    }
    
}
