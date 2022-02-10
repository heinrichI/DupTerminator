namespace DupTerminator.View
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.LabelStaStrip1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.LabelStaStrip2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.LabelStaStrip3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.LabelStaStrip4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.LabelStaStrip5 = new System.Windows.Forms.ToolStripStatusLabel();
            this.cmsDuplicates = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tmsi_Dubli_SelectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_DeSelectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_SelectAllButOne = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_SelectByDate = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_SelectByDateOldestFiles = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_SelectByDateNewestFiles = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_SelectAllInThisFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tmsi_Dubli_CopyPath = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tmsi_Dubli_RenameFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_RenameFileLikeNeighbour = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_MoveSelectedFilesToFolder = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_MoveFileToNeighbour = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tmsi_Dubli_DeleteGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Dubli_AddDirectoryToSkipped = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.tmsi_Dubli_Delete = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsDirectorySearch = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmi_Search_Add = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmi_Search_Edit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmi_Search_Delete = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator8 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmi_Search_SelectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmi_Search_DeselectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.menuMain = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItem_Job = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Save = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Load = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_CurrentText = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Current = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Options = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Language = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_View = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Orientation = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Horizontal = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Vertical = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_FullScreen = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Help = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_CheckForUpdate = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_About = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_VersionInfo = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageSearchCriteria = new System.Windows.Forms.TabPage();
            this.checkBoxSameName = new System.Windows.Forms.CheckBox();
            this.groupBoxFileFilter = new System.Windows.Forms.GroupBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.comboBoxIncludeExtension = new System.Windows.Forms.ComboBox();
            this.buttonSelectExclude = new System.Windows.Forms.Button();
            this.comboBoxExcludeExtension = new System.Windows.Forms.ComboBox();
            this.buttonSelectInclude = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBoxLessThan = new System.Windows.Forms.GroupBox();
            this.nmMin = new System.Windows.Forms.NumericUpDown();
            this.rdbLGiga = new System.Windows.Forms.RadioButton();
            this.rdbLMega = new System.Windows.Forms.RadioButton();
            this.rdbLKilo = new System.Windows.Forms.RadioButton();
            this.rdbLBytes = new System.Windows.Forms.RadioButton();
            this.groupBoxMoreThan = new System.Windows.Forms.GroupBox();
            this.nmMax = new System.Windows.Forms.NumericUpDown();
            this.rdbMGiga = new System.Windows.Forms.RadioButton();
            this.rdbMMega = new System.Windows.Forms.RadioButton();
            this.rdbMKilo = new System.Windows.Forms.RadioButton();
            this.rdbMBytes = new System.Windows.Forms.RadioButton();
            this.tabPageSearchableFolders = new System.Windows.Forms.TabPage();
            this.tabControlFolder = new System.Windows.Forms.TabControl();
            this.tabPageSearchFolder = new System.Windows.Forms.TabPage();
            this.lvDirectorySearch = new System.Windows.Forms.ListView();
            this.tabPageSkipFolder = new System.Windows.Forms.TabPage();
            this.checkedListBoxSkipFolder = new System.Windows.Forms.CheckedListBox();
            this.panel4 = new System.Windows.Forms.Panel();
            this.buttonClearNonExistent = new System.Windows.Forms.Button();
            this.buttonAddDirectory = new System.Windows.Forms.Button();
            this.buttonClearAll = new System.Windows.Forms.Button();
            this.buttonEdit = new System.Windows.Forms.Button();
            this.buttonDel = new System.Windows.Forms.Button();
            this.tabPageDuplicate = new System.Windows.Forms.TabPage();
            this.lvDuplicates = new System.Windows.Forms.ListView();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.cmsSelectBy = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.selectByFileSizeToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.smallestFilesInEachGroupToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.largestFilesInEachGroupToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Select_ByLastAcess = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Select_ByOldestFileInEachGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Select_ByNewestFilesInEachGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Select_ByFileName = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Select_ByFileNameLength = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Select_ByShorterFileNameInEachGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.tmsi_Select_ByLongerFileNameInEachGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.selectByNumberToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmi_Select_biggestNumberInEachGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmi_Select_lowestNumberInEachGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonStart = new Control.BindableToolStripButton();
            this.toolStripButtonCancel = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonUndo = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRedo = new System.Windows.Forms.ToolStripButton();
            this.progressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonDeleteSelectedFiles = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonMove = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSelectBy = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSettings = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRefresh = new System.Windows.Forms.ToolStripButton();
            this.tmsi_Dubli_SelectGroup = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsDuplicates.SuspendLayout();
            this.cmsDirectorySearch.SuspendLayout();
            this.menuMain.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageSearchCriteria.SuspendLayout();
            this.groupBoxFileFilter.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.groupBoxLessThan.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMin)).BeginInit();
            this.groupBoxMoreThan.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMax)).BeginInit();
            this.tabPageSearchableFolders.SuspendLayout();
            this.tabControlFolder.SuspendLayout();
            this.tabPageSearchFolder.SuspendLayout();
            this.tabPageSkipFolder.SuspendLayout();
            this.panel4.SuspendLayout();
            this.tabPageDuplicate.SuspendLayout();
            this.cmsSelectBy.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 541);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(843, 22);
            this.statusStrip1.TabIndex = 13;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // LabelStaStrip1
            // 
            this.LabelStaStrip1.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.LabelStaStrip1.Name = "LabelStaStrip1";
            this.LabelStaStrip1.Size = new System.Drawing.Size(67, 19);
            this.LabelStaStrip1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LabelStaStrip2
            // 
            this.LabelStaStrip2.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.LabelStaStrip2.Name = "LabelStaStrip2";
            this.LabelStaStrip2.Size = new System.Drawing.Size(99, 19);
            this.LabelStaStrip2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LabelStaStrip3
            // 
            this.LabelStaStrip3.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.LabelStaStrip3.Name = "LabelStaStrip3";
            this.LabelStaStrip3.Size = new System.Drawing.Size(86, 19);
            this.LabelStaStrip3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LabelStaStrip4
            // 
            this.LabelStaStrip4.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.LabelStaStrip4.Name = "LabelStaStrip4";
            this.LabelStaStrip4.Size = new System.Drawing.Size(81, 19);
            this.LabelStaStrip4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // LabelStaStrip5
            // 
            this.LabelStaStrip5.Name = "LabelStaStrip5";
            this.LabelStaStrip5.Size = new System.Drawing.Size(495, 19);
            this.LabelStaStrip5.Spring = true;
            this.LabelStaStrip5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cmsDuplicates
            // 
            this.cmsDuplicates.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmsi_Dubli_SelectAll,
            this.tmsi_Dubli_DeSelectAll,
            this.tmsi_Dubli_SelectAllButOne,
            this.ToolStripMenuItem_SelectByDate,
            this.tmsi_Dubli_SelectAllInThisFolder,
            this.tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder,
            this.tmsi_Dubli_SelectGroup,
            this.toolStripSeparator2,
            this.tmsi_Dubli_CopyPath,
            this.toolStripSeparator1,
            this.tmsi_Dubli_RenameFile,
            this.tmsi_Dubli_RenameFileLikeNeighbour,
            this.tmsi_Dubli_MoveSelectedFilesToFolder,
            this.tmsi_Dubli_MoveFileToNeighbour,
            this.toolStripSeparator3,
            this.tmsi_Dubli_DeleteGroup,
            this.tmsi_Dubli_AddDirectoryToSkipped,
            this.toolStripSeparator5,
            this.tmsi_Dubli_Delete});
            this.cmsDuplicates.Name = "cmsDuplicates";
            this.cmsDuplicates.Size = new System.Drawing.Size(359, 380);
            this.cmsDuplicates.Opening += new System.ComponentModel.CancelEventHandler(this.cmsDuplicates_Opening);
            // 
            // tmsi_Dubli_SelectAll
            // 
            this.tmsi_Dubli_SelectAll.Name = "tmsi_Dubli_SelectAll";
            this.tmsi_Dubli_SelectAll.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_SelectAll.Text = "Select All";
            this.tmsi_Dubli_SelectAll.Click += new System.EventHandler(this.tmsi_Dubli_SelectAll_Click);
            // 
            // tmsi_Dubli_DeSelectAll
            // 
            this.tmsi_Dubli_DeSelectAll.Name = "tmsi_Dubli_DeSelectAll";
            this.tmsi_Dubli_DeSelectAll.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_DeSelectAll.Text = "Unselect All";
            this.tmsi_Dubli_DeSelectAll.Click += new System.EventHandler(this.tmsi_Dubli_DeSelectAll_Click);
            // 
            // tmsi_Dubli_SelectAllButOne
            // 
            this.tmsi_Dubli_SelectAllButOne.Name = "tmsi_Dubli_SelectAllButOne";
            this.tmsi_Dubli_SelectAllButOne.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_SelectAllButOne.Text = "Select All But One";
            this.tmsi_Dubli_SelectAllButOne.Click += new System.EventHandler(this.tmsi_Dubli_SelectAllButOne_Click);
            // 
            // ToolStripMenuItem_SelectByDate
            // 
            this.ToolStripMenuItem_SelectByDate.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmsi_Dubli_SelectByDateOldestFiles,
            this.tmsi_Dubli_SelectByDateNewestFiles});
            this.ToolStripMenuItem_SelectByDate.Name = "ToolStripMenuItem_SelectByDate";
            this.ToolStripMenuItem_SelectByDate.Size = new System.Drawing.Size(358, 22);
            this.ToolStripMenuItem_SelectByDate.Text = "Select By Date in Group with This Folders";
            // 
            // tmsi_Dubli_SelectByDateOldestFiles
            // 
            this.tmsi_Dubli_SelectByDateOldestFiles.Name = "tmsi_Dubli_SelectByDateOldestFiles";
            this.tmsi_Dubli_SelectByDateOldestFiles.Size = new System.Drawing.Size(185, 22);
            this.tmsi_Dubli_SelectByDateOldestFiles.Text = "Oldest files in group";
            this.tmsi_Dubli_SelectByDateOldestFiles.Click += new System.EventHandler(this.tmsi_Dubli_SelectByDateOldestFiles_Click);
            // 
            // tmsi_Dubli_SelectByDateNewestFiles
            // 
            this.tmsi_Dubli_SelectByDateNewestFiles.Name = "tmsi_Dubli_SelectByDateNewestFiles";
            this.tmsi_Dubli_SelectByDateNewestFiles.Size = new System.Drawing.Size(185, 22);
            this.tmsi_Dubli_SelectByDateNewestFiles.Text = "Newest files in group";
            this.tmsi_Dubli_SelectByDateNewestFiles.Click += new System.EventHandler(this.tmsi_Dubli_SelectByDateNewestFiles_Click);
            // 
            // tmsi_Dubli_SelectAllInThisFolder
            // 
            this.tmsi_Dubli_SelectAllInThisFolder.Name = "tmsi_Dubli_SelectAllInThisFolder";
            this.tmsi_Dubli_SelectAllInThisFolder.ShortcutKeys = System.Windows.Forms.Keys.F6;
            this.tmsi_Dubli_SelectAllInThisFolder.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_SelectAllInThisFolder.Text = "Select All in This Folder";
            this.tmsi_Dubli_SelectAllInThisFolder.Click += new System.EventHandler(this.tmsi_Dubli_SelectAllInThisFolder_Click);
            // 
            // tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder
            // 
            this.tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder.Name = "tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder";
            this.tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder.ShortcutKeys = System.Windows.Forms.Keys.F5;
            this.tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder.Text = "Select All in This Folder (in group with this folders)";
            this.tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder.Click += new System.EventHandler(this.tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolders_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(355, 6);
            // 
            // tmsi_Dubli_CopyPath
            // 
            this.tmsi_Dubli_CopyPath.Name = "tmsi_Dubli_CopyPath";
            this.tmsi_Dubli_CopyPath.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B)));
            this.tmsi_Dubli_CopyPath.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_CopyPath.Text = "Copy Path";
            this.tmsi_Dubli_CopyPath.Click += new System.EventHandler(this.tmsi_Dubli_CopyPath_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(355, 6);
            // 
            // tmsi_Dubli_RenameFile
            // 
            this.tmsi_Dubli_RenameFile.Name = "tmsi_Dubli_RenameFile";
            this.tmsi_Dubli_RenameFile.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.tmsi_Dubli_RenameFile.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_RenameFile.Text = "Rename File";
            this.tmsi_Dubli_RenameFile.Click += new System.EventHandler(this.tmsi_Dubli_RenameFile_Click);
            // 
            // tmsi_Dubli_RenameFileLikeNeighbour
            // 
            this.tmsi_Dubli_RenameFileLikeNeighbour.Name = "tmsi_Dubli_RenameFileLikeNeighbour";
            this.tmsi_Dubli_RenameFileLikeNeighbour.ShortcutKeys = System.Windows.Forms.Keys.F3;
            this.tmsi_Dubli_RenameFileLikeNeighbour.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_RenameFileLikeNeighbour.Text = "Rename File like Neighbour";
            this.tmsi_Dubli_RenameFileLikeNeighbour.Click += new System.EventHandler(this.tmsi_Dubli_RenameFileLikeNeighbour_Click);
            // 
            // tmsi_Dubli_MoveSelectedFilesToFolder
            // 
            this.tmsi_Dubli_MoveSelectedFilesToFolder.Image = ((System.Drawing.Image)(resources.GetObject("tmsi_Dubli_MoveSelectedFilesToFolder.Image")));
            this.tmsi_Dubli_MoveSelectedFilesToFolder.Name = "tmsi_Dubli_MoveSelectedFilesToFolder";
            this.tmsi_Dubli_MoveSelectedFilesToFolder.ShortcutKeys = System.Windows.Forms.Keys.F8;
            this.tmsi_Dubli_MoveSelectedFilesToFolder.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_MoveSelectedFilesToFolder.Text = "Move Selected Files to Folder";
            this.tmsi_Dubli_MoveSelectedFilesToFolder.Click += new System.EventHandler(this.tmsi_Dubli_MoveSelectedFilesToFolder_Click);
            // 
            // tmsi_Dubli_MoveFileToNeighbour
            // 
            this.tmsi_Dubli_MoveFileToNeighbour.Name = "tmsi_Dubli_MoveFileToNeighbour";
            this.tmsi_Dubli_MoveFileToNeighbour.ShortcutKeys = System.Windows.Forms.Keys.F7;
            this.tmsi_Dubli_MoveFileToNeighbour.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_MoveFileToNeighbour.Text = "Move File to Neighbour";
            this.tmsi_Dubli_MoveFileToNeighbour.Click += new System.EventHandler(this.tmsi_Dubli_MoveFileToNeighbour_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(355, 6);
            // 
            // tmsi_Dubli_DeleteGroup
            // 
            this.tmsi_Dubli_DeleteGroup.Name = "tmsi_Dubli_DeleteGroup";
            this.tmsi_Dubli_DeleteGroup.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_DeleteGroup.Text = "Delete Group from List";
            this.tmsi_Dubli_DeleteGroup.Click += new System.EventHandler(this.tmsi_Dubli_DeleteGroup_Click);
            // 
            // tmsi_Dubli_AddDirectoryToSkipped
            // 
            this.tmsi_Dubli_AddDirectoryToSkipped.Name = "tmsi_Dubli_AddDirectoryToSkipped";
            this.tmsi_Dubli_AddDirectoryToSkipped.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_AddDirectoryToSkipped.Text = "Add directory to skipped";
            this.tmsi_Dubli_AddDirectoryToSkipped.Click += new System.EventHandler(this.tmsi_Dubli_AddDirectoryToSkipped_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(355, 6);
            // 
            // tmsi_Dubli_Delete
            // 
            this.tmsi_Dubli_Delete.Image = global::DupTerminator.Properties.Resources.DeleteIcon16;
            this.tmsi_Dubli_Delete.Name = "tmsi_Dubli_Delete";
            this.tmsi_Dubli_Delete.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_Delete.Text = "Delete";
            this.tmsi_Dubli_Delete.Click += new System.EventHandler(this.tmsi_Dubli_Delete_Click);
            // 
            // cmsDirectorySearch
            // 
            this.cmsDirectorySearch.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmi_Search_Add,
            this.tsmi_Search_Edit,
            this.tsmi_Search_Delete,
            this.toolStripSeparator8,
            this.tsmi_Search_SelectAll,
            this.tsmi_Search_DeselectAll});
            this.cmsDirectorySearch.Name = "cmsDirectorySearch";
            this.cmsDirectorySearch.Size = new System.Drawing.Size(127, 120);
            // 
            // tsmi_Search_Add
            // 
            this.tsmi_Search_Add.Name = "tsmi_Search_Add";
            this.tsmi_Search_Add.Size = new System.Drawing.Size(126, 22);
            this.tsmi_Search_Add.Text = "Add";
            this.tsmi_Search_Add.Click += new System.EventHandler(this.tsmi_Search_Add_Click);
            // 
            // tsmi_Search_Edit
            // 
            this.tsmi_Search_Edit.Name = "tsmi_Search_Edit";
            this.tsmi_Search_Edit.Size = new System.Drawing.Size(126, 22);
            this.tsmi_Search_Edit.Text = "Edit";
            this.tsmi_Search_Edit.Click += new System.EventHandler(this.tsmi_Search_Edit_Click);
            // 
            // tsmi_Search_Delete
            // 
            this.tsmi_Search_Delete.Name = "tsmi_Search_Delete";
            this.tsmi_Search_Delete.Size = new System.Drawing.Size(126, 22);
            this.tsmi_Search_Delete.Text = "Delete";
            this.tsmi_Search_Delete.Click += new System.EventHandler(this.tsmi_Search_Delete_Click);
            // 
            // toolStripSeparator8
            // 
            this.toolStripSeparator8.Name = "toolStripSeparator8";
            this.toolStripSeparator8.Size = new System.Drawing.Size(123, 6);
            // 
            // tsmi_Search_SelectAll
            // 
            this.tsmi_Search_SelectAll.Name = "tsmi_Search_SelectAll";
            this.tsmi_Search_SelectAll.Size = new System.Drawing.Size(126, 22);
            this.tsmi_Search_SelectAll.Text = "SelectAll";
            this.tsmi_Search_SelectAll.Click += new System.EventHandler(this.tsmi_Search_SelectAll_Click);
            // 
            // tsmi_Search_DeselectAll
            // 
            this.tsmi_Search_DeselectAll.Name = "tsmi_Search_DeselectAll";
            this.tsmi_Search_DeselectAll.Size = new System.Drawing.Size(126, 22);
            this.tsmi_Search_DeselectAll.Text = "DeselctAll";
            this.tsmi_Search_DeselectAll.Click += new System.EventHandler(this.tsmi_Search_DeselectAll_Click);
            // 
            // menuMain
            // 
            this.menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Job,
            this.toolStripMenuItem_Options,
            this.toolStripMenuItem_View,
            this.toolStripMenuItem_Help,
            this.toolStripMenuItem_VersionInfo});
            this.menuMain.Location = new System.Drawing.Point(0, 0);
            this.menuMain.Name = "menuMain";
            this.menuMain.Size = new System.Drawing.Size(843, 24);
            this.menuMain.Stretch = false;
            this.menuMain.TabIndex = 24;
            this.menuMain.Text = "menuStrip1";
            this.menuMain.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Generic_MouseMove);
            // 
            // toolStripMenuItem_Job
            // 
            this.toolStripMenuItem_Job.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Save,
            this.toolStripMenuItem_Load,
            this.toolStripMenuItem_CurrentText,
            this.toolStripMenuItem_Current});
            this.toolStripMenuItem_Job.Name = "toolStripMenuItem_Job";
            this.toolStripMenuItem_Job.Size = new System.Drawing.Size(37, 20);
            this.toolStripMenuItem_Job.Text = "Job";
            // 
            // toolStripMenuItem_Save
            // 
            this.toolStripMenuItem_Save.Name = "toolStripMenuItem_Save";
            this.toolStripMenuItem_Save.Size = new System.Drawing.Size(117, 22);
            this.toolStripMenuItem_Save.Text = "Save";
            this.toolStripMenuItem_Save.Click += new System.EventHandler(this.toolStripMenuItem_Save_Click);
            // 
            // toolStripMenuItem_Load
            // 
            this.toolStripMenuItem_Load.Name = "toolStripMenuItem_Load";
            this.toolStripMenuItem_Load.Size = new System.Drawing.Size(117, 22);
            this.toolStripMenuItem_Load.Text = "Load";
            this.toolStripMenuItem_Load.Click += new System.EventHandler(this.toolStripMenuItem_Load_Click);
            // 
            // toolStripMenuItem_CurrentText
            // 
            this.toolStripMenuItem_CurrentText.Enabled = false;
            this.toolStripMenuItem_CurrentText.Name = "toolStripMenuItem_CurrentText";
            this.toolStripMenuItem_CurrentText.Size = new System.Drawing.Size(117, 22);
            this.toolStripMenuItem_CurrentText.Text = "Current:";
            // 
            // toolStripMenuItem_Current
            // 
            this.toolStripMenuItem_Current.Enabled = false;
            this.toolStripMenuItem_Current.Name = "toolStripMenuItem_Current";
            this.toolStripMenuItem_Current.Size = new System.Drawing.Size(117, 22);
            this.toolStripMenuItem_Current.Text = "Job";
            // 
            // toolStripMenuItem_Options
            // 
            this.toolStripMenuItem_Options.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Language,
            this.toolStripMenuItem_Settings});
            this.toolStripMenuItem_Options.Name = "toolStripMenuItem_Options";
            this.toolStripMenuItem_Options.Size = new System.Drawing.Size(61, 20);
            this.toolStripMenuItem_Options.Text = "Options";
            // 
            // toolStripMenuItem_Language
            // 
            this.toolStripMenuItem_Language.Name = "toolStripMenuItem_Language";
            this.toolStripMenuItem_Language.Size = new System.Drawing.Size(126, 22);
            this.toolStripMenuItem_Language.Text = "Language";
            // 
            // toolStripMenuItem_Settings
            // 
            this.toolStripMenuItem_Settings.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItem_Settings.Image")));
            this.toolStripMenuItem_Settings.Name = "toolStripMenuItem_Settings";
            this.toolStripMenuItem_Settings.Size = new System.Drawing.Size(126, 22);
            this.toolStripMenuItem_Settings.Text = "Settings";
            this.toolStripMenuItem_Settings.Click += new System.EventHandler(this.MainMenuItem_Click_Setting);
            // 
            // toolStripMenuItem_View
            // 
            this.toolStripMenuItem_View.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Orientation,
            this.toolStripMenuItem_FullScreen});
            this.toolStripMenuItem_View.Name = "toolStripMenuItem_View";
            this.toolStripMenuItem_View.Size = new System.Drawing.Size(44, 20);
            this.toolStripMenuItem_View.Text = "View";
            // 
            // toolStripMenuItem_Orientation
            // 
            this.toolStripMenuItem_Orientation.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Horizontal,
            this.toolStripMenuItem_Vertical});
            this.toolStripMenuItem_Orientation.Name = "toolStripMenuItem_Orientation";
            this.toolStripMenuItem_Orientation.Size = new System.Drawing.Size(158, 22);
            this.toolStripMenuItem_Orientation.Text = "&Orientation";
            // 
            // toolStripMenuItem_Horizontal
            // 
            this.toolStripMenuItem_Horizontal.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItem_Horizontal.Image")));
            this.toolStripMenuItem_Horizontal.Name = "toolStripMenuItem_Horizontal";
            this.toolStripMenuItem_Horizontal.Size = new System.Drawing.Size(129, 22);
            this.toolStripMenuItem_Horizontal.Text = "&Horizontal";
            this.toolStripMenuItem_Horizontal.Click += new System.EventHandler(this.MainMenuItem_Click_Horizontal);
            // 
            // toolStripMenuItem_Vertical
            // 
            this.toolStripMenuItem_Vertical.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItem_Vertical.Image")));
            this.toolStripMenuItem_Vertical.Name = "toolStripMenuItem_Vertical";
            this.toolStripMenuItem_Vertical.Size = new System.Drawing.Size(129, 22);
            this.toolStripMenuItem_Vertical.Text = "&Vertical";
            this.toolStripMenuItem_Vertical.Click += new System.EventHandler(this.MainMenuItem_Click_Vertical);
            // 
            // toolStripMenuItem_FullScreen
            // 
            this.toolStripMenuItem_FullScreen.CheckOnClick = true;
            this.toolStripMenuItem_FullScreen.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItem_FullScreen.Image")));
            this.toolStripMenuItem_FullScreen.Name = "toolStripMenuItem_FullScreen";
            this.toolStripMenuItem_FullScreen.ShortcutKeys = System.Windows.Forms.Keys.F12;
            this.toolStripMenuItem_FullScreen.Size = new System.Drawing.Size(158, 22);
            this.toolStripMenuItem_FullScreen.Text = "&Full-Screen";
            this.toolStripMenuItem_FullScreen.CheckedChanged += new System.EventHandler(this.MainMenuItem_CheckedChanged_FullScreen);
            // 
            // toolStripMenuItem_Help
            // 
            this.toolStripMenuItem_Help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_CheckForUpdate,
            this.toolStripMenuItem_About});
            this.toolStripMenuItem_Help.Name = "toolStripMenuItem_Help";
            this.toolStripMenuItem_Help.Size = new System.Drawing.Size(44, 20);
            this.toolStripMenuItem_Help.Text = "&Help";
            // 
            // toolStripMenuItem_CheckForUpdate
            // 
            this.toolStripMenuItem_CheckForUpdate.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItem_CheckForUpdate.Image")));
            this.toolStripMenuItem_CheckForUpdate.Name = "toolStripMenuItem_CheckForUpdate";
            this.toolStripMenuItem_CheckForUpdate.Size = new System.Drawing.Size(166, 22);
            this.toolStripMenuItem_CheckForUpdate.Text = "Check for &Update";
            this.toolStripMenuItem_CheckForUpdate.Click += new System.EventHandler(this.MainMenuItem_CheckForUpdate_Click);
            // 
            // toolStripMenuItem_About
            // 
            this.toolStripMenuItem_About.Name = "toolStripMenuItem_About";
            this.toolStripMenuItem_About.Size = new System.Drawing.Size(166, 22);
            this.toolStripMenuItem_About.Text = "&About";
            // 
            // toolStripMenuItem_VersionInfo
            // 
            this.toolStripMenuItem_VersionInfo.Image = ((System.Drawing.Image)(resources.GetObject("toolStripMenuItem_VersionInfo.Image")));
            this.toolStripMenuItem_VersionInfo.Name = "toolStripMenuItem_VersionInfo";
            this.toolStripMenuItem_VersionInfo.Size = new System.Drawing.Size(98, 20);
            this.toolStripMenuItem_VersionInfo.Text = "Version Info";
            this.toolStripMenuItem_VersionInfo.Visible = false;
            this.toolStripMenuItem_VersionInfo.Click += new System.EventHandler(this.MainMenuItem_VersionInfo_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Location = new System.Drawing.Point(0, 52);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer1.Panel2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Generic_MouseMove);
            this.splitContainer1.Size = new System.Drawing.Size(843, 486);
            this.splitContainer1.SplitterDistance = 150;
            this.splitContainer1.SplitterWidth = 3;
            this.splitContainer1.TabIndex = 6;
            this.splitContainer1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Generic_MouseMove);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageSearchCriteria);
            this.tabControl1.Controls.Add(this.tabPageSearchableFolders);
            this.tabControl1.Controls.Add(this.tabPageDuplicate);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(686, 482);
            this.tabControl1.TabIndex = 14;
            // 
            // tabPageSearchCriteria
            // 
            this.tabPageSearchCriteria.Controls.Add(this.checkBoxSameName);
            this.tabPageSearchCriteria.Controls.Add(this.groupBoxFileFilter);
            this.tabPageSearchCriteria.Controls.Add(this.groupBoxLessThan);
            this.tabPageSearchCriteria.Controls.Add(this.groupBoxMoreThan);
            this.tabPageSearchCriteria.Location = new System.Drawing.Point(4, 22);
            this.tabPageSearchCriteria.Name = "tabPageSearchCriteria";
            this.tabPageSearchCriteria.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSearchCriteria.Size = new System.Drawing.Size(678, 456);
            this.tabPageSearchCriteria.TabIndex = 3;
            this.tabPageSearchCriteria.Text = "Search Criteria";
            this.tabPageSearchCriteria.UseVisualStyleBackColor = true;
            // 
            // checkBoxSameName
            // 
            this.checkBoxSameName.AutoSize = true;
            this.checkBoxSameName.Location = new System.Drawing.Point(307, 30);
            this.checkBoxSameName.Name = "checkBoxSameName";
            this.checkBoxSameName.Size = new System.Drawing.Size(103, 17);
            this.checkBoxSameName.TabIndex = 0;
            this.checkBoxSameName.Text = "Same File Name";
            this.checkBoxSameName.UseVisualStyleBackColor = true;
            this.checkBoxSameName.CheckedChanged += new System.EventHandler(this.checkBoxSameName_CheckedChanged);
            // 
            // groupBoxFileFilter
            // 
            this.groupBoxFileFilter.Controls.Add(this.panel3);
            this.groupBoxFileFilter.Controls.Add(this.panel2);
            this.groupBoxFileFilter.Location = new System.Drawing.Point(12, 128);
            this.groupBoxFileFilter.Name = "groupBoxFileFilter";
            this.groupBoxFileFilter.Size = new System.Drawing.Size(411, 90);
            this.groupBoxFileFilter.TabIndex = 21;
            this.groupBoxFileFilter.TabStop = false;
            this.groupBoxFileFilter.Text = "File Filter";
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.comboBoxIncludeExtension);
            this.panel3.Controls.Add(this.buttonSelectExclude);
            this.panel3.Controls.Add(this.comboBoxExcludeExtension);
            this.panel3.Controls.Add(this.buttonSelectInclude);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(66, 16);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(342, 71);
            this.panel3.TabIndex = 22;
            // 
            // comboBoxIncludeExtension
            // 
            this.comboBoxIncludeExtension.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxIncludeExtension.FormattingEnabled = true;
            this.comboBoxIncludeExtension.Location = new System.Drawing.Point(3, 12);
            this.comboBoxIncludeExtension.Name = "comboBoxIncludeExtension";
            this.comboBoxIncludeExtension.Size = new System.Drawing.Size(261, 21);
            this.comboBoxIncludeExtension.TabIndex = 0;
            // 
            // buttonSelectExclude
            // 
            this.buttonSelectExclude.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectExclude.Location = new System.Drawing.Point(271, 40);
            this.buttonSelectExclude.Name = "buttonSelectExclude";
            this.buttonSelectExclude.Size = new System.Drawing.Size(66, 23);
            this.buttonSelectExclude.TabIndex = 2;
            this.buttonSelectExclude.Text = "Select";
            this.buttonSelectExclude.UseVisualStyleBackColor = true;
            this.buttonSelectExclude.Click += new System.EventHandler(this.buttonSelectExclude_Click);
            // 
            // comboBoxExcludeExtension
            // 
            this.comboBoxExcludeExtension.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxExcludeExtension.FormattingEnabled = true;
            this.comboBoxExcludeExtension.Location = new System.Drawing.Point(3, 43);
            this.comboBoxExcludeExtension.Name = "comboBoxExcludeExtension";
            this.comboBoxExcludeExtension.Size = new System.Drawing.Size(261, 21);
            this.comboBoxExcludeExtension.TabIndex = 0;
            // 
            // buttonSelectInclude
            // 
            this.buttonSelectInclude.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectInclude.Location = new System.Drawing.Point(271, 10);
            this.buttonSelectInclude.Name = "buttonSelectInclude";
            this.buttonSelectInclude.Size = new System.Drawing.Size(66, 23);
            this.buttonSelectInclude.TabIndex = 2;
            this.buttonSelectInclude.Text = "Select";
            this.buttonSelectInclude.UseVisualStyleBackColor = true;
            this.buttonSelectInclude.Click += new System.EventHandler(this.buttonSelectInclude_Click);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.Transparent;
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.label3);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(3, 16);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(63, 71);
            this.panel2.TabIndex = 22;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 15);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Included:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 46);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Excluded:";
            // 
            // groupBoxLessThan
            // 
            this.groupBoxLessThan.Controls.Add(this.nmMin);
            this.groupBoxLessThan.Controls.Add(this.rdbLGiga);
            this.groupBoxLessThan.Controls.Add(this.rdbLMega);
            this.groupBoxLessThan.Controls.Add(this.rdbLKilo);
            this.groupBoxLessThan.Controls.Add(this.rdbLBytes);
            this.groupBoxLessThan.Location = new System.Drawing.Point(12, 12);
            this.groupBoxLessThan.Name = "groupBoxLessThan";
            this.groupBoxLessThan.Size = new System.Drawing.Size(275, 52);
            this.groupBoxLessThan.TabIndex = 18;
            this.groupBoxLessThan.TabStop = false;
            this.groupBoxLessThan.Text = "Exclude sizes Less than";
            // 
            // nmMin
            // 
            this.nmMin.Location = new System.Drawing.Point(6, 18);
            this.nmMin.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.nmMin.Name = "nmMin";
            this.nmMin.Size = new System.Drawing.Size(58, 20);
            this.nmMin.TabIndex = 2;
            // 
            // rdbLGiga
            // 
            this.rdbLGiga.AutoSize = true;
            this.rdbLGiga.Location = new System.Drawing.Point(219, 19);
            this.rdbLGiga.Name = "rdbLGiga";
            this.rdbLGiga.Size = new System.Drawing.Size(39, 17);
            this.rdbLGiga.TabIndex = 1;
            this.rdbLGiga.Text = "Gb";
            this.rdbLGiga.UseVisualStyleBackColor = true;
            // 
            // rdbLMega
            // 
            this.rdbLMega.AutoSize = true;
            this.rdbLMega.Location = new System.Drawing.Point(172, 18);
            this.rdbLMega.Name = "rdbLMega";
            this.rdbLMega.Size = new System.Drawing.Size(40, 17);
            this.rdbLMega.TabIndex = 1;
            this.rdbLMega.Text = "Mb";
            this.rdbLMega.UseVisualStyleBackColor = true;
            // 
            // rdbLKilo
            // 
            this.rdbLKilo.AutoSize = true;
            this.rdbLKilo.Location = new System.Drawing.Point(127, 18);
            this.rdbLKilo.Name = "rdbLKilo";
            this.rdbLKilo.Size = new System.Drawing.Size(38, 17);
            this.rdbLKilo.TabIndex = 1;
            this.rdbLKilo.Text = "Kb";
            this.rdbLKilo.UseVisualStyleBackColor = true;
            // 
            // rdbLBytes
            // 
            this.rdbLBytes.AutoSize = true;
            this.rdbLBytes.Checked = true;
            this.rdbLBytes.Location = new System.Drawing.Point(70, 18);
            this.rdbLBytes.Name = "rdbLBytes";
            this.rdbLBytes.Size = new System.Drawing.Size(51, 17);
            this.rdbLBytes.TabIndex = 1;
            this.rdbLBytes.TabStop = true;
            this.rdbLBytes.Text = "Bytes";
            this.rdbLBytes.UseVisualStyleBackColor = true;
            // 
            // groupBoxMoreThan
            // 
            this.groupBoxMoreThan.Controls.Add(this.nmMax);
            this.groupBoxMoreThan.Controls.Add(this.rdbMGiga);
            this.groupBoxMoreThan.Controls.Add(this.rdbMMega);
            this.groupBoxMoreThan.Controls.Add(this.rdbMKilo);
            this.groupBoxMoreThan.Controls.Add(this.rdbMBytes);
            this.groupBoxMoreThan.Location = new System.Drawing.Point(12, 70);
            this.groupBoxMoreThan.Name = "groupBoxMoreThan";
            this.groupBoxMoreThan.Size = new System.Drawing.Size(275, 52);
            this.groupBoxMoreThan.TabIndex = 17;
            this.groupBoxMoreThan.TabStop = false;
            this.groupBoxMoreThan.Text = "Exclude sizes More than";
            // 
            // nmMax
            // 
            this.nmMax.Location = new System.Drawing.Point(6, 18);
            this.nmMax.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.nmMax.Name = "nmMax";
            this.nmMax.Size = new System.Drawing.Size(58, 20);
            this.nmMax.TabIndex = 2;
            // 
            // rdbMGiga
            // 
            this.rdbMGiga.AutoSize = true;
            this.rdbMGiga.Location = new System.Drawing.Point(218, 18);
            this.rdbMGiga.Name = "rdbMGiga";
            this.rdbMGiga.Size = new System.Drawing.Size(39, 17);
            this.rdbMGiga.TabIndex = 1;
            this.rdbMGiga.Text = "Gb";
            this.rdbMGiga.UseVisualStyleBackColor = true;
            // 
            // rdbMMega
            // 
            this.rdbMMega.AutoSize = true;
            this.rdbMMega.Location = new System.Drawing.Point(172, 18);
            this.rdbMMega.Name = "rdbMMega";
            this.rdbMMega.Size = new System.Drawing.Size(40, 17);
            this.rdbMMega.TabIndex = 1;
            this.rdbMMega.Text = "Mb";
            this.rdbMMega.UseVisualStyleBackColor = true;
            // 
            // rdbMKilo
            // 
            this.rdbMKilo.AutoSize = true;
            this.rdbMKilo.Location = new System.Drawing.Point(127, 18);
            this.rdbMKilo.Name = "rdbMKilo";
            this.rdbMKilo.Size = new System.Drawing.Size(38, 17);
            this.rdbMKilo.TabIndex = 1;
            this.rdbMKilo.Text = "Kb";
            this.rdbMKilo.UseVisualStyleBackColor = true;
            // 
            // rdbMBytes
            // 
            this.rdbMBytes.AutoSize = true;
            this.rdbMBytes.Checked = true;
            this.rdbMBytes.Location = new System.Drawing.Point(70, 18);
            this.rdbMBytes.Name = "rdbMBytes";
            this.rdbMBytes.Size = new System.Drawing.Size(51, 17);
            this.rdbMBytes.TabIndex = 1;
            this.rdbMBytes.TabStop = true;
            this.rdbMBytes.Text = "Bytes";
            this.rdbMBytes.UseVisualStyleBackColor = true;
            // 
            // tabPageSearchableFolders
            // 
            this.tabPageSearchableFolders.Controls.Add(this.tabControlFolder);
            this.tabPageSearchableFolders.Controls.Add(this.panel4);
            this.tabPageSearchableFolders.Location = new System.Drawing.Point(4, 22);
            this.tabPageSearchableFolders.Name = "tabPageSearchableFolders";
            this.tabPageSearchableFolders.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSearchableFolders.Size = new System.Drawing.Size(678, 456);
            this.tabPageSearchableFolders.TabIndex = 0;
            this.tabPageSearchableFolders.Text = "Searchable Folders";
            this.tabPageSearchableFolders.UseVisualStyleBackColor = true;
            // 
            // tabControlFolder
            // 
            this.tabControlFolder.Controls.Add(this.tabPageSearchFolder);
            this.tabControlFolder.Controls.Add(this.tabPageSkipFolder);
            this.tabControlFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlFolder.Location = new System.Drawing.Point(136, 3);
            this.tabControlFolder.Name = "tabControlFolder";
            this.tabControlFolder.SelectedIndex = 0;
            this.tabControlFolder.Size = new System.Drawing.Size(539, 450);
            this.tabControlFolder.TabIndex = 6;
            // 
            // tabPageSearchFolder
            // 
            this.tabPageSearchFolder.Controls.Add(this.lvDirectorySearch);
            this.tabPageSearchFolder.Location = new System.Drawing.Point(4, 22);
            this.tabPageSearchFolder.Name = "tabPageSearchFolder";
            this.tabPageSearchFolder.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSearchFolder.Size = new System.Drawing.Size(531, 424);
            this.tabPageSearchFolder.TabIndex = 0;
            this.tabPageSearchFolder.Text = "Searchable Folders";
            this.tabPageSearchFolder.UseVisualStyleBackColor = true;
            // 
            // lvDirectorySearch
            // 
            this.lvDirectorySearch.AllowDrop = true;
            this.lvDirectorySearch.CheckBoxes = true;
            this.lvDirectorySearch.ContextMenuStrip = this.cmsDirectorySearch;
            this.lvDirectorySearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvDirectorySearch.Location = new System.Drawing.Point(3, 3);
            this.lvDirectorySearch.Name = "lvDirectorySearch";
            this.lvDirectorySearch.Size = new System.Drawing.Size(525, 418);
            this.lvDirectorySearch.TabIndex = 6;
            this.lvDirectorySearch.UseCompatibleStateImageBehavior = false;
            this.lvDirectorySearch.View = System.Windows.Forms.View.Details;
            this.lvDirectorySearch.DragDrop += new System.Windows.Forms.DragEventHandler(this.lvDirectorySearch_DragDrop);
            this.lvDirectorySearch.DragEnter += new System.Windows.Forms.DragEventHandler(this.lvDirectorySearch_DragEnter);
            this.lvDirectorySearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvDirectorySearch_KeyDown);
            // 
            // tabPageSkipFolder
            // 
            this.tabPageSkipFolder.Controls.Add(this.checkedListBoxSkipFolder);
            this.tabPageSkipFolder.Location = new System.Drawing.Point(4, 22);
            this.tabPageSkipFolder.Name = "tabPageSkipFolder";
            this.tabPageSkipFolder.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSkipFolder.Size = new System.Drawing.Size(531, 424);
            this.tabPageSkipFolder.TabIndex = 1;
            this.tabPageSkipFolder.Text = "Skip Folder";
            this.tabPageSkipFolder.UseVisualStyleBackColor = true;
            // 
            // checkedListBoxSkipFolder
            // 
            this.checkedListBoxSkipFolder.AllowDrop = true;
            this.checkedListBoxSkipFolder.ContextMenuStrip = this.cmsDirectorySearch;
            this.checkedListBoxSkipFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBoxSkipFolder.FormattingEnabled = true;
            this.checkedListBoxSkipFolder.Location = new System.Drawing.Point(3, 3);
            this.checkedListBoxSkipFolder.Name = "checkedListBoxSkipFolder";
            this.checkedListBoxSkipFolder.Size = new System.Drawing.Size(525, 409);
            this.checkedListBoxSkipFolder.TabIndex = 7;
            this.checkedListBoxSkipFolder.DragDrop += new System.Windows.Forms.DragEventHandler(this.checkedListBoxSkipFolder_DragDrop);
            this.checkedListBoxSkipFolder.DragEnter += new System.Windows.Forms.DragEventHandler(this.checkedListBoxSkipFolder_DragEnter);
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.buttonClearNonExistent);
            this.panel4.Controls.Add(this.buttonAddDirectory);
            this.panel4.Controls.Add(this.buttonClearAll);
            this.panel4.Controls.Add(this.buttonEdit);
            this.panel4.Controls.Add(this.buttonDel);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel4.Location = new System.Drawing.Point(3, 3);
            this.panel4.MinimumSize = new System.Drawing.Size(133, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(133, 450);
            this.panel4.TabIndex = 7;
            // 
            // buttonClearNonExistent
            // 
            this.buttonClearNonExistent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClearNonExistent.AutoSize = true;
            this.buttonClearNonExistent.Location = new System.Drawing.Point(3, 146);
            this.buttonClearNonExistent.Name = "buttonClearNonExistent";
            this.buttonClearNonExistent.Size = new System.Drawing.Size(121, 25);
            this.buttonClearNonExistent.TabIndex = 26;
            this.buttonClearNonExistent.Text = "Clear nonexistent";
            this.buttonClearNonExistent.UseVisualStyleBackColor = true;
            this.buttonClearNonExistent.Click += new System.EventHandler(this.buttonClearNonExistent_Click);
            // 
            // buttonAddDirectory
            // 
            this.buttonAddDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddDirectory.Location = new System.Drawing.Point(3, 22);
            this.buttonAddDirectory.Name = "buttonAddDirectory";
            this.buttonAddDirectory.Size = new System.Drawing.Size(121, 25);
            this.buttonAddDirectory.TabIndex = 2;
            this.buttonAddDirectory.Text = "&Add Folder";
            this.buttonAddDirectory.UseVisualStyleBackColor = true;
            this.buttonAddDirectory.Click += new System.EventHandler(this.buttonAddDirectory_Click);
            // 
            // buttonClearAll
            // 
            this.buttonClearAll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClearAll.Location = new System.Drawing.Point(3, 115);
            this.buttonClearAll.Name = "buttonClearAll";
            this.buttonClearAll.Size = new System.Drawing.Size(121, 25);
            this.buttonClearAll.TabIndex = 2;
            this.buttonClearAll.Text = "&Clear all";
            this.buttonClearAll.UseVisualStyleBackColor = true;
            this.buttonClearAll.Click += new System.EventHandler(this.buttonClearAll_Click);
            // 
            // buttonEdit
            // 
            this.buttonEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEdit.Location = new System.Drawing.Point(3, 53);
            this.buttonEdit.Name = "buttonEdit";
            this.buttonEdit.Size = new System.Drawing.Size(121, 25);
            this.buttonEdit.TabIndex = 2;
            this.buttonEdit.Text = "&Edit";
            this.buttonEdit.UseVisualStyleBackColor = true;
            this.buttonEdit.Click += new System.EventHandler(this.buttonEdit_Click);
            // 
            // buttonDel
            // 
            this.buttonDel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDel.Location = new System.Drawing.Point(3, 84);
            this.buttonDel.Name = "buttonDel";
            this.buttonDel.Size = new System.Drawing.Size(121, 25);
            this.buttonDel.TabIndex = 2;
            this.buttonDel.Text = "&Delete";
            this.buttonDel.UseVisualStyleBackColor = true;
            this.buttonDel.Click += new System.EventHandler(this.buttonDel_Click);
            // 
            // tabPageDuplicate
            // 
            this.tabPageDuplicate.Controls.Add(this.lvDuplicates);
            this.tabPageDuplicate.Controls.Add(this.panelButtons);
            this.tabPageDuplicate.Location = new System.Drawing.Point(4, 22);
            this.tabPageDuplicate.Name = "tabPageDuplicate";
            this.tabPageDuplicate.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageDuplicate.Size = new System.Drawing.Size(678, 456);
            this.tabPageDuplicate.TabIndex = 2;
            this.tabPageDuplicate.Text = "Duplicate Results";
            this.tabPageDuplicate.UseVisualStyleBackColor = true;
            // 
            // lvDuplicates
            // 
            this.lvDuplicates.AllowColumnReorder = true;
            this.lvDuplicates.CheckBoxes = true;
            this.lvDuplicates.ContextMenuStrip = this.cmsDuplicates;
            this.lvDuplicates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvDuplicates.FullRowSelect = true;
            this.lvDuplicates.GridLines = true;
            this.lvDuplicates.Location = new System.Drawing.Point(3, 3);
            this.lvDuplicates.Name = "lvDuplicates";
            this.lvDuplicates.ShowGroups = false;
            this.lvDuplicates.Size = new System.Drawing.Size(672, 450);
            this.lvDuplicates.TabIndex = 0;
            this.lvDuplicates.UseCompatibleStateImageBehavior = false;
            this.lvDuplicates.VirtualMode = true;
            this.lvDuplicates.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lvDuplicates_ItemChecked);
            this.lvDuplicates.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lvDuplicates_MouseClick);
            this.lvDuplicates.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.lvDuplicates_AfterLabelEdit);
            this.lvDuplicates.SelectedIndexChanged += new System.EventHandler(this.lvDuplicates_SelectedIndexChanged);
            this.lvDuplicates.DoubleClick += new System.EventHandler(this.lvDuplicates_DoubleClick);
            this.lvDuplicates.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvDuplicates_ColumnClick);
            this.lvDuplicates.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.lvDuplicates_RetrieveVirtualItem);
            this.lvDuplicates.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvDuplicates_KeyDown);
            // 
            // panelButtons
            // 
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelButtons.Location = new System.Drawing.Point(3, 3);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(672, 450);
            this.panelButtons.TabIndex = 26;
            // 
            // cmsSelectBy
            // 
            this.cmsSelectBy.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectByFileSizeToolStripMenuItem1,
            this.tmsi_Select_ByLastAcess,
            this.tmsi_Select_ByFileName,
            this.tmsi_Select_ByFileNameLength,
            this.selectByNumberToolStripMenuItem});
            this.cmsSelectBy.Name = "contextMenuStrip1";
            this.cmsSelectBy.Size = new System.Drawing.Size(228, 114);
            // 
            // selectByFileSizeToolStripMenuItem1
            // 
            this.selectByFileSizeToolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.smallestFilesInEachGroupToolStripMenuItem1,
            this.largestFilesInEachGroupToolStripMenuItem1});
            this.selectByFileSizeToolStripMenuItem1.Enabled = false;
            this.selectByFileSizeToolStripMenuItem1.Name = "selectByFileSizeToolStripMenuItem1";
            this.selectByFileSizeToolStripMenuItem1.Size = new System.Drawing.Size(227, 22);
            this.selectByFileSizeToolStripMenuItem1.Text = "Select by File Size";
            this.selectByFileSizeToolStripMenuItem1.Visible = false;
            // 
            // smallestFilesInEachGroupToolStripMenuItem1
            // 
            this.smallestFilesInEachGroupToolStripMenuItem1.Name = "smallestFilesInEachGroupToolStripMenuItem1";
            this.smallestFilesInEachGroupToolStripMenuItem1.Size = new System.Drawing.Size(218, 22);
            this.smallestFilesInEachGroupToolStripMenuItem1.Text = "Smallest files in each group";
            // 
            // largestFilesInEachGroupToolStripMenuItem1
            // 
            this.largestFilesInEachGroupToolStripMenuItem1.Name = "largestFilesInEachGroupToolStripMenuItem1";
            this.largestFilesInEachGroupToolStripMenuItem1.Size = new System.Drawing.Size(218, 22);
            this.largestFilesInEachGroupToolStripMenuItem1.Text = "Largest files in each group";
            // 
            // tmsi_Select_ByLastAcess
            // 
            this.tmsi_Select_ByLastAcess.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmsi_Select_ByOldestFileInEachGroup,
            this.tmsi_Select_ByNewestFilesInEachGroup});
            this.tmsi_Select_ByLastAcess.Name = "tmsi_Select_ByLastAcess";
            this.tmsi_Select_ByLastAcess.Size = new System.Drawing.Size(227, 22);
            this.tmsi_Select_ByLastAcess.Text = "Select by Last Accessed Time";
            // 
            // tmsi_Select_ByOldestFileInEachGroup
            // 
            this.tmsi_Select_ByOldestFileInEachGroup.Name = "tmsi_Select_ByOldestFileInEachGroup";
            this.tmsi_Select_ByOldestFileInEachGroup.Size = new System.Drawing.Size(213, 22);
            this.tmsi_Select_ByOldestFileInEachGroup.Text = "Oldest files in each group";
            this.tmsi_Select_ByOldestFileInEachGroup.Click += new System.EventHandler(this.tmsi_Select_ByOldestFileInEachGroup_Click);
            // 
            // tmsi_Select_ByNewestFilesInEachGroup
            // 
            this.tmsi_Select_ByNewestFilesInEachGroup.Name = "tmsi_Select_ByNewestFilesInEachGroup";
            this.tmsi_Select_ByNewestFilesInEachGroup.Size = new System.Drawing.Size(213, 22);
            this.tmsi_Select_ByNewestFilesInEachGroup.Text = "Newest files in each group";
            this.tmsi_Select_ByNewestFilesInEachGroup.Click += new System.EventHandler(this.tmsi_Select_ByNewestFilesInEachGroup_Click);
            // 
            // tmsi_Select_ByFileName
            // 
            this.tmsi_Select_ByFileName.Name = "tmsi_Select_ByFileName";
            this.tmsi_Select_ByFileName.Size = new System.Drawing.Size(227, 22);
            this.tmsi_Select_ByFileName.Text = "Select by File Name";
            this.tmsi_Select_ByFileName.Click += new System.EventHandler(this.tmsi_Select_ByFileName_Click);
            // 
            // tmsi_Select_ByFileNameLength
            // 
            this.tmsi_Select_ByFileNameLength.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmsi_Select_ByShorterFileNameInEachGroup,
            this.tmsi_Select_ByLongerFileNameInEachGroup});
            this.tmsi_Select_ByFileNameLength.Name = "tmsi_Select_ByFileNameLength";
            this.tmsi_Select_ByFileNameLength.Size = new System.Drawing.Size(227, 22);
            this.tmsi_Select_ByFileNameLength.Text = "Select by File Name Length";
            // 
            // tmsi_Select_ByShorterFileNameInEachGroup
            // 
            this.tmsi_Select_ByShorterFileNameInEachGroup.Name = "tmsi_Select_ByShorterFileNameInEachGroup";
            this.tmsi_Select_ByShorterFileNameInEachGroup.Size = new System.Drawing.Size(240, 22);
            this.tmsi_Select_ByShorterFileNameInEachGroup.Text = "Shorter file name in each group";
            this.tmsi_Select_ByShorterFileNameInEachGroup.Click += new System.EventHandler(this.tmsi_Select_ByShorterFileNameLength_Click);
            // 
            // tmsi_Select_ByLongerFileNameInEachGroup
            // 
            this.tmsi_Select_ByLongerFileNameInEachGroup.Name = "tmsi_Select_ByLongerFileNameInEachGroup";
            this.tmsi_Select_ByLongerFileNameInEachGroup.Size = new System.Drawing.Size(240, 22);
            this.tmsi_Select_ByLongerFileNameInEachGroup.Text = "Longer file name in each group";
            this.tmsi_Select_ByLongerFileNameInEachGroup.Click += new System.EventHandler(this.tmsi_Select_ByLongerFileNameLength_Click);
            // 
            // selectByNumberToolStripMenuItem
            // 
            this.selectByNumberToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmi_Select_biggestNumberInEachGroup,
            this.tsmi_Select_lowestNumberInEachGroup});
            this.selectByNumberToolStripMenuItem.Name = "selectByNumberToolStripMenuItem";
            this.selectByNumberToolStripMenuItem.Size = new System.Drawing.Size(227, 22);
            this.selectByNumberToolStripMenuItem.Text = "Select by Number";
            // 
            // tsmi_Select_biggestNumberInEachGroup
            // 
            this.tsmi_Select_biggestNumberInEachGroup.Name = "tsmi_Select_biggestNumberInEachGroup";
            this.tsmi_Select_biggestNumberInEachGroup.Size = new System.Drawing.Size(234, 22);
            this.tsmi_Select_biggestNumberInEachGroup.Text = "Biggest number in each group";
            this.tsmi_Select_biggestNumberInEachGroup.Click += new System.EventHandler(this.tsmi_Select_biggestNumberInEachGroup_Click);
            // 
            // tsmi_Select_lowestNumberInEachGroup
            // 
            this.tsmi_Select_lowestNumberInEachGroup.Name = "tsmi_Select_lowestNumberInEachGroup";
            this.tsmi_Select_lowestNumberInEachGroup.Size = new System.Drawing.Size(234, 22);
            this.tsmi_Select_lowestNumberInEachGroup.Text = "Lowest number in each group";
            this.tsmi_Select_lowestNumberInEachGroup.Click += new System.EventHandler(this.tsmi_Select_lowestNumberInEachGroup_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonStart,
            this.toolStripButtonCancel,
            this.toolStripSeparator7,
            this.toolStripButtonUndo,
            this.toolStripButtonRedo,
            this.progressBar1,
            this.toolStripSeparator6,
            this.toolStripButtonDeleteSelectedFiles,
            this.toolStripButtonMove,
            this.toolStripButtonSelectBy,
            this.toolStripButtonSettings,
            this.toolStripButtonRefresh});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(843, 25);
            this.toolStrip1.TabIndex = 25;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonStart
            // 
            this.toolStripButtonStart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonStart.Image = global::DupTerminator.Properties.Resources.play_32;
            this.toolStripButtonStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonStart.Name = "toolStripButtonStart";
            this.toolStripButtonStart.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonStart.Text = "toolStripButton1";
            this.toolStripButtonStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // toolStripButtonCancel
            // 
            this.toolStripButtonCancel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCancel.Enabled = false;
            this.toolStripButtonCancel.Image = global::DupTerminator.Properties.Resources.stop_32;
            this.toolStripButtonCancel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCancel.Name = "toolStripButtonCancel";
            this.toolStripButtonCancel.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonCancel.Text = "toolStripButton1";
            this.toolStripButtonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonUndo
            // 
            this.toolStripButtonUndo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonUndo.Enabled = false;
            this.toolStripButtonUndo.Image = global::DupTerminator.Properties.Resources.undo_3_32;
            this.toolStripButtonUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonUndo.Name = "toolStripButtonUndo";
            this.toolStripButtonUndo.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonUndo.Text = "toolStripButton1";
            this.toolStripButtonUndo.Click += new System.EventHandler(this.buttonUndo_Click);
            // 
            // toolStripButtonRedo
            // 
            this.toolStripButtonRedo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRedo.Enabled = false;
            this.toolStripButtonRedo.Image = global::DupTerminator.Properties.Resources.redo_3_32;
            this.toolStripButtonRedo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRedo.Name = "toolStripButtonRedo";
            this.toolStripButtonRedo.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRedo.Text = "toolStripButton1";
            this.toolStripButtonRedo.Click += new System.EventHandler(this.toolStripButtonRedo_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(100, 22);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonDeleteSelectedFiles
            // 
            this.toolStripButtonDeleteSelectedFiles.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDeleteSelectedFiles.Image = global::DupTerminator.Properties.Resources.DeleteSelected16;
            this.toolStripButtonDeleteSelectedFiles.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDeleteSelectedFiles.Name = "toolStripButtonDeleteSelectedFiles";
            this.toolStripButtonDeleteSelectedFiles.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonDeleteSelectedFiles.Text = "toolStripButton1";
            this.toolStripButtonDeleteSelectedFiles.Click += new System.EventHandler(this.buttonDeleteSelectedFiles_Click);
            // 
            // toolStripButtonMove
            // 
            this.toolStripButtonMove.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonMove.Image = global::DupTerminator.Properties.Resources.moveto16;
            this.toolStripButtonMove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonMove.Name = "toolStripButtonMove";
            this.toolStripButtonMove.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonMove.Text = "toolStripButton1";
            this.toolStripButtonMove.Click += new System.EventHandler(this.buttonMove_Click);
            // 
            // toolStripButtonSelectBy
            // 
            this.toolStripButtonSelectBy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSelectBy.Image = global::DupTerminator.Properties.Resources.Checked_checkbox16;
            this.toolStripButtonSelectBy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSelectBy.Name = "toolStripButtonSelectBy";
            this.toolStripButtonSelectBy.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSelectBy.Text = "toolStripButton1";
            this.toolStripButtonSelectBy.Click += new System.EventHandler(this.buttonSelectBy_Click);
            // 
            // toolStripButtonSettings
            // 
            this.toolStripButtonSettings.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSettings.Image = global::DupTerminator.Properties.Resources.Setting;
            this.toolStripButtonSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSettings.Name = "toolStripButtonSettings";
            this.toolStripButtonSettings.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSettings.Text = "toolStripButton1";
            this.toolStripButtonSettings.Click += new System.EventHandler(this.toolStripButtonSettings_Click);
            // 
            // toolStripButtonRefresh
            // 
            this.toolStripButtonRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRefresh.Image = global::DupTerminator.Properties.Resources.sinchronize_32;
            this.toolStripButtonRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRefresh.Name = "toolStripButtonRefresh";
            this.toolStripButtonRefresh.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRefresh.Text = "toolStripButton1";
            this.toolStripButtonRefresh.Click += new System.EventHandler(this.toolStripButtonRefresh_Click);
            // 
            // tmsi_Dubli_SelectGroup
            // 
            this.tmsi_Dubli_SelectGroup.Name = "tmsi_Dubli_SelectGroup";
            this.tmsi_Dubli_SelectGroup.Size = new System.Drawing.Size(358, 22);
            this.tmsi_Dubli_SelectGroup.Text = "Select group";
            this.tmsi_Dubli_SelectGroup.Click += new System.EventHandler(this.tmsi_Dubli_SelectGroup_Click);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(843, 563);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuMain);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuMain;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FormMain";
            this.Text = "DupTerminator";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.FormMain_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormMain_KeyDown);
            this.FontChanged += new System.EventHandler(this.FormMain_FontChanged);
            this.cmsDuplicates.ResumeLayout(false);
            this.cmsDirectorySearch.ResumeLayout(false);
            this.menuMain.ResumeLayout(false);
            this.menuMain.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPageSearchCriteria.ResumeLayout(false);
            this.tabPageSearchCriteria.PerformLayout();
            this.groupBoxFileFilter.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.groupBoxLessThan.ResumeLayout(false);
            this.groupBoxLessThan.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMin)).EndInit();
            this.groupBoxMoreThan.ResumeLayout(false);
            this.groupBoxMoreThan.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmMax)).EndInit();
            this.tabPageSearchableFolders.ResumeLayout(false);
            this.tabControlFolder.ResumeLayout(false);
            this.tabPageSearchFolder.ResumeLayout(false);
            this.tabPageSkipFolder.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.tabPageDuplicate.ResumeLayout(false);
            this.cmsSelectBy.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ContextMenuStrip cmsDuplicates;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_SelectAllButOne;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_SelectAll;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_DeSelectAll;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_Delete;
        private System.Windows.Forms.ContextMenuStrip cmsDirectorySearch;
        private System.Windows.Forms.ToolStripMenuItem tsmi_Search_Delete;
        private System.Windows.Forms.MenuStrip menuMain;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_SelectAllInThisFolder;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_SelectAllInThisFolderinGroupWithThisFolder;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_RenameFile;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_MoveSelectedFilesToFolder;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_RenameFileLikeNeighbour;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_MoveFileToNeighbour;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripStatusLabel LabelStaStrip1;
        private System.Windows.Forms.ToolStripStatusLabel LabelStaStrip2;
        private System.Windows.Forms.ToolStripStatusLabel LabelStaStrip3;
        private System.Windows.Forms.ToolStripStatusLabel LabelStaStrip4;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageSearchCriteria;
        private System.Windows.Forms.GroupBox groupBoxFileFilter;
        private System.Windows.Forms.Button buttonSelectExclude;
        private System.Windows.Forms.Button buttonSelectInclude;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBoxExcludeExtension;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxIncludeExtension;
        private System.Windows.Forms.CheckBox checkBoxSameName;
        private System.Windows.Forms.GroupBox groupBoxLessThan;
        private System.Windows.Forms.NumericUpDown nmMin;
        private System.Windows.Forms.RadioButton rdbLGiga;
        private System.Windows.Forms.RadioButton rdbLMega;
        private System.Windows.Forms.RadioButton rdbLKilo;
        private System.Windows.Forms.RadioButton rdbLBytes;
        private System.Windows.Forms.GroupBox groupBoxMoreThan;
        private System.Windows.Forms.NumericUpDown nmMax;
        private System.Windows.Forms.RadioButton rdbMGiga;
        private System.Windows.Forms.RadioButton rdbMMega;
        private System.Windows.Forms.RadioButton rdbMKilo;
        private System.Windows.Forms.RadioButton rdbMBytes;
        private System.Windows.Forms.TabPage tabPageSearchableFolders;
        private System.Windows.Forms.TabControl tabControlFolder;
        private System.Windows.Forms.TabPage tabPageSearchFolder;
        private System.Windows.Forms.TabPage tabPageSkipFolder;
        private System.Windows.Forms.Button buttonAddDirectory;
        private System.Windows.Forms.TabPage tabPageDuplicate;
        private System.Windows.Forms.ListView lvDuplicates;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Options;
        private System.Windows.Forms.Button buttonClearAll;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_View;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Orientation;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Horizontal;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Vertical;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_FullScreen;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Help;
        private System.Windows.Forms.Button buttonDel;
        private System.Windows.Forms.ToolStripStatusLabel LabelStaStrip5;
        private System.Windows.Forms.ContextMenuStrip cmsSelectBy;
        private System.Windows.Forms.ToolStripMenuItem selectByFileSizeToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem smallestFilesInEachGroupToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem largestFilesInEachGroupToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Select_ByLastAcess;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Select_ByOldestFileInEachGroup;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Select_ByNewestFilesInEachGroup;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_CopyPath;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_VersionInfo;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_CheckForUpdate;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_About;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_SelectByDate;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_SelectByDateNewestFiles;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_SelectByDateOldestFiles;
        private System.Windows.Forms.ToolStripMenuItem tsmi_Search_Add;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Select_ByFileName;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Select_ByFileNameLength;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Select_ByShorterFileNameInEachGroup;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Select_ByLongerFileNameInEachGroup;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_DeleteGroup;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Language;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Settings;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.ListView lvDirectorySearch;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Job;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Save;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Load;
        private System.Windows.Forms.Button buttonEdit;
        private System.Windows.Forms.ToolStripMenuItem tsmi_Search_Edit;
        private System.Windows.Forms.CheckedListBox checkedListBoxSkipFolder;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_CurrentText;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Current;
        private System.Windows.Forms.Button buttonClearNonExistent;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private Control.BindableToolStripButton toolStripButtonStart;
        private System.Windows.Forms.ToolStripButton toolStripButtonCancel;
        private System.Windows.Forms.ToolStripButton toolStripButtonUndo;
        private System.Windows.Forms.ToolStripButton toolStripButtonRedo;
        private System.Windows.Forms.ToolStripProgressBar progressBar1;
        private System.Windows.Forms.ToolStripButton toolStripButtonDeleteSelectedFiles;
        private System.Windows.Forms.ToolStripButton toolStripButtonMove;
        private System.Windows.Forms.ToolStripButton toolStripButtonSelectBy;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
        private System.Windows.Forms.ToolStripButton toolStripButtonSettings;
        private System.Windows.Forms.ToolStripButton toolStripButtonRefresh;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator8;
        private System.Windows.Forms.ToolStripMenuItem tsmi_Search_SelectAll;
        private System.Windows.Forms.ToolStripMenuItem tsmi_Search_DeselectAll;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_AddDirectoryToSkipped;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem selectByNumberToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmi_Select_biggestNumberInEachGroup;
        private System.Windows.Forms.ToolStripMenuItem tsmi_Select_lowestNumberInEachGroup;
        private System.Windows.Forms.ToolStripMenuItem tmsi_Dubli_SelectGroup;
    }
}

