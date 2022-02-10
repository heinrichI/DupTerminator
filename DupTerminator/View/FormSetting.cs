using DupTerminator.BusinessLogic;
using DupTerminator.DataBase;
using DupTerminator.Localize;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DupTerminator.View
{
    internal partial class FormSetting : BaseForm
    {
        //public Settings settings = new Settings(); //экземпляр класса с настройками 
        private Settings settings;
        public delegate void UpdateListViewHandler(Font font);
        public event UpdateListViewHandler UpdateListView;

        public delegate void UpdatePreviewHandler();
        public event UpdatePreviewHandler UpdatePreview;

        //public DBManager dbManager;
        private FormProgress _formProgress;
        private readonly IDBManager _dbManager;

        public FormSetting(IDBManager dbManager)
        {
            settings = Settings.GetInstance();
            InitializeComponent();
            _dbManager = dbManager ?? throw new ArgumentNullException(nameof(dbManager));
        }

        private void FormSetting_Load(object sender, EventArgs e)
        {
            UpdateControlStatesFromSetting();
            //labelProgramFont.Text = this.Owner.Font.ToString();
            labelProgramFontValue.Text = FontToString(this.Owner.Font);
        }

        private void UpdateControlStatesFromSetting()
        {
            this.SuspendLayout();
            checkBoxConfirmDelete.Checked = settings.Fields.IsConfirmDelete;
            checkBoxUpdate.Checked = settings.Fields.IsCheckUpdate;
            checkBoxSaveListDubli.Checked = settings.Fields.IsSaveLoadListDub;
            checkBoxNonExistentOnLoad.Checked = settings.Fields.IsCheckNonExistentOnLoad;
            checkBoxDelAll.Checked = settings.Fields.IsAllowDelAllFiles;
            //textBoxHistoryLength.Text = settings.Fields.PathHistoryLength.ToString();
            numericUpDownHistoryLength.Value = settings.Fields.PathHistoryLength;
            checkBoxFastCheck.Checked = settings.Fields.FastCheck;
            numericUpDownFastCheckFileSize.Value = settings.Fields.FastCheckFileSizeMb;
            numericUpDownFastCheckBuffer.Value = settings.Fields.FastCheckBufferKb;
            checkBoxUseDB.Checked = settings.Fields.UseDB;
            if (settings.Fields.UseDB)
            {
                _dbManager.Active = true;
                buttonCleanDB.Enabled = true;
                buttonDeleteDB.Enabled = true;
            }
            else
            {
                _dbManager.Active = true;
                buttonCleanDB.Enabled = false;
                buttonDeleteDB.Enabled = false;
            }
            //if (dbManager != null)
                showSizeDB();

            labelProgramFontValue.Text = FontToString(settings.Fields.ProgramFont.ToFont());
            labelListRowFontValue.Text = FontToString(settings.Fields.ListRowFont.ToFont());
            buttonRow1Color.BackColor = settings.Fields.ColorRow1.ToColor();
            buttonRow2Color.BackColor = settings.Fields.ColorRow2.ToColor();
            buttonRowNotExist.BackColor = settings.Fields.ColorRowNotExist.ToColor();
            buttonRowError.BackColor = settings.Fields.ColorRowError.ToColor();
            checkBoxMaxFileToScan.Checked = settings.Fields.IsScanMax;
            numericUpDownMaxDup.Value = new Decimal(settings.Fields.MaxFile);
            checkBoxShowNeighboringFiles.Checked = settings.Fields.ShowNeighboringFiles;
            numericUpDownMaxFilePreview.Value = settings.Fields.MaxFilePreviewMb;
            this.ResumeLayout();
        }

        private void FormSetting_Shown(object sender, EventArgs e)
        {
            UpdatePanelWidth(panelColorLeft);
            UpdatePanelWidth(panelLeft);
            //if (dbManager != null)
                showSizeDB();
        }

        private void showSizeDB()
        {
            labelSizeDatabase.Text = LanguageManager.GetProperty(this, "labelSizeDatabase.Text") + " " 
                + _dbManager.GetSizeDB();
        }

        private void m_btnOK_Click(object sender, EventArgs e)
        {
            settings.Fields.IsConfirmDelete = checkBoxConfirmDelete.Checked;
            settings.Fields.IsCheckUpdate = checkBoxUpdate.Checked;
            settings.Fields.IsSaveLoadListDub = checkBoxSaveListDubli.Checked;
            settings.Fields.IsCheckNonExistentOnLoad = checkBoxNonExistentOnLoad.Checked;
            settings.Fields.IsAllowDelAllFiles = checkBoxDelAll.Checked;
            settings.Fields.IsScanMax = checkBoxMaxFileToScan.Checked;
            settings.Fields.MaxFile = Decimal.ToInt32(numericUpDownMaxDup.Value);
            settings.Fields.FastCheck = checkBoxFastCheck.Checked;
            settings.Fields.FastCheckFileSizeMb = Decimal.ToUInt32(numericUpDownFastCheckFileSize.Value);
            settings.Fields.FastCheckBufferKb = Decimal.ToUInt32(numericUpDownFastCheckBuffer.Value);
            settings.Fields.MaxFilePreviewMb = Decimal.ToUInt32(numericUpDownMaxFilePreview.Value);
        }


        private void buttonFont_Click(object sender, EventArgs e)
        {
            //_settings.Fields.ProgramFont = SerializableFont.FromFont(this.Font);
            using (FontDialog fontDialog = new FontDialog())
            {
                fontDialog.Font = settings.Fields.ProgramFont.ToFont();

                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    settings.Fields.ProgramFont = SerializableFont.FromFont(fontDialog.Font);
                    this.Font = fontDialog.Font;
                    this.Owner.Font = fontDialog.Font;
                    labelProgramFontValue.Text = FontToString(fontDialog.Font);

                    //UpdatePanelWidth(panel5);
                }
            }
        }

        private void buttonListRowFont_Click(object sender, EventArgs e)
        {
            using (FontDialog fontDialog = new FontDialog())
            {
                fontDialog.Font = settings.Fields.ListRowFont.ToFont();

                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    settings.Fields.ListRowFont = SerializableFont.FromFont(fontDialog.Font);
                    labelListRowFontValue.Text = FontToString(fontDialog.Font);

                    UpdateListView(fontDialog.Font);
                }
            }
        }

        private string FontToString(Font font)
        {
            string strFont = string.Empty;
            if (font.Bold)
            {
                strFont = strFont + " Bold";
            }
            if (font.Italic)
            {
                strFont = strFont + " Italic";
            }
            return (font.Name + " (" + font.Size.ToString() + " " + font.Unit.ToString() + strFont + ")");
        }

        private void buttonRow1Color_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlgColor = new ColorDialog())
            {
                dlgColor.Color = settings.Fields.ColorRow1.ToColor();
                dlgColor.FullOpen = true;

                // This is used in all dialog boxes to present the user with all of the configurable
                // colors used in the applications UI
                // дополнительные цвета
                dlgColor.CustomColors = new int[] 
                { 
                    ConvertToRgb(settings.Fields.ColorRow1.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRow2.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRowNotExist.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRowError.ToColor())
                };

                if (dlgColor.ShowDialog() == DialogResult.OK)
                {
                    settings.Fields.ColorRow1 = SerializableColor.FromColor(dlgColor.Color);
                    buttonRow1Color.BackColor = dlgColor.Color;

                    /*for (int i = 0; i < _frmMain.PlainTextBoxes.Count; i++)
                    {
                        _frmMain.PlainTextBoxes[i].ForeColor = dlgColor.Color;
                    }*/
                }
            }
        }

        private void buttonRow2Color_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlgColor = new ColorDialog())
            {
                dlgColor.Color = settings.Fields.ColorRow1.ToColor();
                dlgColor.FullOpen = true;

                // This is used in all dialog boxes to present the user with all of the configurable
                // colors used in the applications UI
                // дополнительные цвета
                dlgColor.CustomColors = new int[] 
                { 
                    ConvertToRgb(settings.Fields.ColorRow1.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRow2.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRowNotExist.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRowError.ToColor())
                };

                if (dlgColor.ShowDialog() == DialogResult.OK)
                {
                    settings.Fields.ColorRow2 = SerializableColor.FromColor(dlgColor.Color);
                    buttonRow2Color.BackColor = dlgColor.Color;
                }
            }
        }

        private void buttonRowNotExist_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlgColor = new ColorDialog())
            {
                dlgColor.Color = settings.Fields.ColorRow1.ToColor();
                dlgColor.FullOpen = true;

                // This is used in all dialog boxes to present the user with all of the configurable
                // colors used in the applications UI
                // дополнительные цвета
                dlgColor.CustomColors = new int[] 
                { 
                    ConvertToRgb(settings.Fields.ColorRow1.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRow2.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRowNotExist.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRowError.ToColor())
                };

                if (dlgColor.ShowDialog() == DialogResult.OK)
                {
                    settings.Fields.ColorRowNotExist = SerializableColor.FromColor(dlgColor.Color);
                    buttonRowNotExist.BackColor = dlgColor.Color;
                }
            }
        }

        private void buttonRowError_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlgColor = new ColorDialog())
            {
                dlgColor.Color = settings.Fields.ColorRow1.ToColor();
                dlgColor.FullOpen = true;

                // This is used in all dialog boxes to present the user with all of the configurable
                // colors used in the applications UI
                // дополнительные цвета
                dlgColor.CustomColors = new int[] 
                { 
                    ConvertToRgb(settings.Fields.ColorRow1.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRow2.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRowNotExist.ToColor()),
                    ConvertToRgb(settings.Fields.ColorRowError.ToColor())
                };

                if (dlgColor.ShowDialog() == DialogResult.OK)
                {
                    settings.Fields.ColorRowError = SerializableColor.FromColor(dlgColor.Color);
                    buttonRowError.BackColor = dlgColor.Color;
                }
            }
        }

        /// <summary>
        /// Converts a color to solid color represented by an integer
        /// </summary>
        private int ConvertToRgb(Color color)
        {
            return (color.R) | (color.G << 8) | (color.B << 16);
        }

        /*#region validators
        bool ValidatorNumber(string sValue)
        {
            if (sValue.Length == 0) return false;
            int nOutValue;
            return !int.TryParse(sValue, out nOutValue);
        }
        void ValidationFlash(TextBox tbxCur)
        {
            Color cPrevious = tbxCur.BackColor;
            tbxCur.BackColor = Color.Red;
            Application.DoEvents();
            System.Threading.Thread.Sleep(200);
            tbxCur.BackColor = cPrevious;
            Application.DoEvents();
        }
        private void textBox1_Validating(object sender, CancelEventArgs e)
        {
            TextBox tbxCur = (TextBox)sender;
            if (e.Cancel = ValidatorNumber(tbxCur.Text))
                ValidationFlash(tbxCur);
        }

        private void textBoxHistoryLength_Validated(object sender, EventArgs e)
        {
            settings.Fields.PathHistoryLength = Int32.Parse(textBoxHistoryLength.Text);
        }
        #endregion*/

        private void numericUpDownHistoryLength_ValueChanged(object sender, EventArgs e)
        {
            settings.Fields.PathHistoryLength = Decimal.ToInt32(numericUpDownHistoryLength.Value);
        }


        private void checkBoxUseDB_Click(object sender, EventArgs e)
        {
            settings.Fields.UseDB = checkBoxUseDB.Checked;
            if (settings.Fields.UseDB)
            {
                _dbManager.Active = true;
                _dbManager.CreateDataBase();
                showSizeDB();
                buttonCleanDB.Enabled = true;
                buttonDeleteDB.Enabled = true;
            }
            else
            {
                _dbManager.Active = false;
                buttonCleanDB.Enabled = false;
                buttonDeleteDB.Enabled = false;
            }
        }

        private void buttonDeleteDB_Click(object sender, EventArgs e)
        {
            _dbManager.DeleteDB();
            showSizeDB();
        }

        private void buttonCleanDB_Click(object sender, EventArgs e)
        {
            tabControl1.Enabled = false;
            
            
            _formProgress = new FormProgress();
            _formProgress.Owner = this;
            _formProgress.Icon = Properties.Resources.SettingIco;
            //_formProgress.StartPosition = FormStartPosition.Manual;
            //_formProgress.Location = new Point(Location.X + (this.Width - _formProgress.Width) / 2, this.Location.Y + (Height - _formProgress.Height) / 2);
            //_formProgress.dbManager = dbManager;
            //_formProgress.progressBar.Value = 0;
            //_formProgress.textBoxCount.Text = "0";
            //_formProgress.Refresh();

            _dbManager.ProgressChangedEvent += _formProgress.ProgressChangedEventHandler;
            _dbManager.DeletingCompletedEvent += DeletingCompletedEventHandler;
            _dbManager.SetMaxValueEvent += SetMaxValueEventHandler;
            _formProgress.CancelEvent += new FormProgress.CancelDelegate(_dbManager.CancelDeletingEventHandler);

            _formProgress.Show(this);
            //_formProgress.ShowDialog();

            _dbManager.CleanDB(); //separate thread
        }

        /// <summary>
        /// Устанавливает максимальное значение для progressBar.
        /// </summary>
        /// <param name="value">Значение</param>
        private delegate void SetMaxValueDelegate(int value);
        private void SetMaxValueEventHandler(int value)
        {
            if (InvokeRequired)
            {
                object[] eventArgs = { value };
                Invoke(new SetMaxValueDelegate(SetMaxValueEventHandler), eventArgs);
                return;
            }

            //_formProgress.progressBar.Maximum = value;
            _formProgress.SetProgressMax(value);
        }

        private delegate void DeletingCompletedDelegate();
        private void DeletingCompletedEventHandler()
        {
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                object[] eventArgs = { };
                Invoke(new DeletingCompletedDelegate(DeletingCompletedEventHandler), eventArgs);
                return;
            }

            _dbManager.ProgressChangedEvent -= _formProgress.ProgressChangedEventHandler;
            _dbManager.DeletingCompletedEvent -= DeletingCompletedEventHandler;
            _dbManager.SetMaxValueEvent -= SetMaxValueEventHandler;
            _formProgress.Close();
            _formProgress.Dispose();
            showSizeDB();
            tabControl1.Enabled = true;
        }

        private void checkBoxShowNeighboringFiles_CheckStateChanged(object sender, EventArgs e)
        {
            if (settings.Fields.ShowNeighboringFiles != checkBoxShowNeighboringFiles.Checked)
            {
                settings.Fields.ShowNeighboringFiles = checkBoxShowNeighboringFiles.Checked;
                UpdatePreview();
            }
        }

    }
}
