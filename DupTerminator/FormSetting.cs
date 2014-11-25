using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace DupTerminator
{
    internal partial class FormSetting : BaseForm
    {
        public SettingsApp settings = new SettingsApp(); //экземпляр класса с настройками 
        public delegate void UpdateListViewHandler(Font font);
        public event UpdateListViewHandler UpdateListView;

        public DBManager dbManager;
        private FormProgress formProgress;

        public FormSetting()
        {
            InitializeComponent();
        }

        public FormSetting(Form form)
        {
            this.Owner = form;
            InitializeComponent();
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
            checkBoxDontUpdateSize.Checked = settings.Fields.IsDontUpdateSize;
            //textBoxHistoryLength.Text = settings.Fields.PathHistoryLength.ToString();
            numericUpDownHistoryLength.Value = settings.Fields.PathHistoryLength;
            checkBoxFastCheck.Checked = settings.Fields.FastCheck;
            numericUpDownFastCheckFileSize.Value = settings.Fields.FastCheckFileSizeMb;
            numericUpDownFastCheckBuffer.Value = settings.Fields.FastCheckBufferKb;
            checkBoxUseDB.Checked = settings.Fields.UseDB;
            if (settings.Fields.UseDB)
            {
                if (dbManager == null)
                    dbManager = new DBManager();
                dbManager.Active = true;
                buttonCleanDB.Enabled = true;
                buttonDeleteDB.Enabled = true;
            }
            else
            {
                if (dbManager != null)
                    dbManager.Active = true;
                buttonCleanDB.Enabled = false;
                buttonDeleteDB.Enabled = false;
            }
            if (dbManager != null)
                showSizeDB();

            labelProgramFontValue.Text = FontToString(settings.Fields.ProgramFont.ToFont());
            labelListRowFontValue.Text = FontToString(settings.Fields.ListRowFont.ToFont());
            buttonRow1Color.BackColor = settings.Fields.ColorRow1.ToColor();
            buttonRow2Color.BackColor = settings.Fields.ColorRow2.ToColor();
            buttonRowNotExist.BackColor = settings.Fields.ColorRowNotExist.ToColor();
            buttonRowError.BackColor = settings.Fields.ColorRowError.ToColor();
            checkBoxMaxFileToScan.Checked = settings.Fields.IsScanMax;
            numericUpDownMaxDup.Value = new Decimal(settings.Fields.MaxFile);
            this.ResumeLayout();
        }

        private void FormSetting_Shown(object sender, EventArgs e)
        {
            UpdatePanelWidth(panelColorLeft);
            UpdatePanelWidth(panelLeft);
            if (dbManager != null)
                showSizeDB();
        }

        private void showSizeDB()
        {
            labelSizeDatabase.Text = LanguageManager.GetProperty(this,"labelSizeDatabase.Text") + " " + dbManager.SizeDB();
        }

        private void m_btnOK_Click(object sender, EventArgs e)
        {
            settings.Fields.IsConfirmDelete = checkBoxConfirmDelete.Checked;
            settings.Fields.IsCheckUpdate = checkBoxUpdate.Checked;
            settings.Fields.IsSaveLoadListDub = checkBoxSaveListDubli.Checked;
            settings.Fields.IsCheckNonExistentOnLoad = checkBoxNonExistentOnLoad.Checked;
            settings.Fields.IsAllowDelAllFiles = checkBoxDelAll.Checked;
            settings.Fields.IsDontUpdateSize = checkBoxDontUpdateSize.Checked;
            settings.Fields.IsScanMax = checkBoxMaxFileToScan.Checked;
            settings.Fields.MaxFile = Decimal.ToInt32(numericUpDownMaxDup.Value);
            settings.Fields.FastCheck = checkBoxFastCheck.Checked;
            settings.Fields.FastCheckFileSizeMb = Decimal.ToUInt32(numericUpDownFastCheckFileSize.Value);
            settings.Fields.FastCheckBufferKb = Decimal.ToUInt32(numericUpDownFastCheckBuffer.Value);
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


        /*public bool IsConfirmDelete
        {
            get {  return checkBoxConfirmDelete.Checked; }
        }*/

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
                if (dbManager == null)
                    dbManager = new DBManager();
                dbManager.Active = true;
                dbManager.CreateDataBase();
                showSizeDB();
                buttonCleanDB.Enabled = true;
                buttonDeleteDB.Enabled = true;
            }
            else
            {
                dbManager.Active = false;
                buttonCleanDB.Enabled = false;
                buttonDeleteDB.Enabled = false;
            }
        }

        private void buttonDeleteDB_Click(object sender, EventArgs e)
        {
            if (dbManager != null)
            {
                dbManager.DeleteDB();
                showSizeDB();
            }
        }

        private void buttonCleanDB_Click(object sender, EventArgs e)
        {
            if (dbManager != null)
            {
                tabControl1.Enabled = false;
                dbManager.ProgressChangedEvent += new DBManager.ProgressChangedDelegate(ProgressChangedEventHandler);
                dbManager.DeletingCompletedEvent += new DBManager.DeletingCompletedDelegate(DeletingCompletedEventHandler);
                dbManager.SetMaxValueEvent += new DBManager.SetMaxValueDelegate(SetMaxValueEventHandler);
                
                
                formProgress = new FormProgress();
                formProgress.Owner = this;
                formProgress.Icon = Properties.Resources.SettingIco;
                //formProgress.StartPosition = FormStartPosition.Manual;
                //formProgress.Location = new Point(Location.X + (this.Width - formProgress.Width) / 2, this.Location.Y + (Height - formProgress.Height) / 2);
                formProgress.dbManager = dbManager;
                //formProgress.progressBar.Value = 0;
                //formProgress.textBoxCount.Text = "0";
                //formProgress.Refresh();
                formProgress.Show(this);
                //formProgress.ShowDialog();

                dbManager.CleanDB(); //separate thread
            }
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

            //formProgress.progressBar.Maximum = value;
            formProgress.SetMaxProgress(value);
        }

        /// <summary>
        /// Устанавливет значение переданное значение для progressBar.
        /// </summary>
        /// <param name="count"></param>
        private delegate void ProgressChangedDelegate(int count);
        private void ProgressChangedEventHandler(int count)
        {
            if (InvokeRequired)
            {
                object[] eventArgs = { count };
                Invoke(new ProgressChangedDelegate(ProgressChangedEventHandler), eventArgs);
                return;
            }

            //formProgress.progressBar.Value = count;
            formProgress.SetCurrentProgress(count);
        }
       

        private delegate void DeletingCompletedDelegate();
        private void DeletingCompletedEventHandler()
        {
            if (InvokeRequired)
            {
                object[] eventArgs = { };
                Invoke(new DeletingCompletedDelegate(DeletingCompletedEventHandler), eventArgs);
                return;
            }

            formProgress.Close();
            formProgress.Dispose();
            dbManager.ProgressChangedEvent -= new DBManager.ProgressChangedDelegate(ProgressChangedEventHandler);
            dbManager.DeletingCompletedEvent -= new DBManager.DeletingCompletedDelegate(DeletingCompletedEventHandler);
            dbManager.SetMaxValueEvent -= new DBManager.SetMaxValueDelegate(SetMaxValueEventHandler);
            showSizeDB();
            tabControl1.Enabled = true;
        }

    }
}
