using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DupTerminator.Views
{
    internal partial class FormFolderSelect : BaseForm
    {
        //public Settings settings = new Settings(); //экземпляр класса с настройками
        private Settings settings;
        private ToolTip ttForm;

        //private String _SelectedPath;
        public String SelectedPath 
        {
            get { return comboBoxPath.Text; }
            set { comboBoxPath.Text = value; }
        }

        public bool IsSubDir
        {
            get { return checkBoxSubDir.Checked; }
            set { checkBoxSubDir.Checked = value; }
        }

        public bool ShowSubDirCheck
        {
            get { return checkBoxSubDir.Visible; }
            set { checkBoxSubDir.Visible = value; }
        }

        public FormFolderSelect()
        {
            InitializeComponent();
            settings = Settings.GetInstance();
        }

        public FormFolderSelect(String path)
        {
            InitializeComponent();

            ttForm = new ToolTip();
            ttForm.SetToolTip(checkBoxSubDir, LanguageManager.GetString("toolTip_chkRecurse"));

            comboBoxPath.Text = path;
        }

        private void FormFolderSelect_Load(object sender, EventArgs e)
        {
            if (settings.Fields.PathHistory != null)
            {
                comboBoxPath.Items.Clear();
                try
                {
                    foreach (string sCur in settings.Fields.PathHistory)
                        comboBoxPath.Items.Add(sCur);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = comboBoxPath.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
               comboBoxPath.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void m_btnOK_Click(object sender, EventArgs e)
        {
            if (comboBoxPath.Text.Length > 0)
            {
                //обрезка последнего \
                /*int lastIndex = comboBoxPath.Text.LastIndexOf('\\');
                if (lastIndex == comboBoxPath.Text.Length - 1)
                    comboBoxPath.Text = comboBoxPath.Text.Remove(lastIndex);*/
                //добавление \ если нет
                int lastIndex = comboBoxPath.Text.LastIndexOf('\\');
                if (lastIndex != comboBoxPath.Text.Length - 1)
                    comboBoxPath.Text = comboBoxPath.Text + '\\';

                int nMax = settings.Fields.PathHistoryLength;
                List<string> lsTmp = SaveUserEntry_MoveToTopRelative(comboBoxPath, nMax);
                settings.Fields.PathHistory = lsTmp;
            }
        }

        //FileSearch
        List<string> SaveUserEntry_MoveToTopRelative(ComboBox cbxCur, int nNbMaxLines)
        {
            List<string> lsOnTop = new List<string>();
            List<string> lsOthers = new List<string>();

            string sItem = cbxCur.Text;
            if (!cbxCur.Items.Contains(sItem))
                lsOnTop.Add(sItem);
            //string sLabel = null;
            //if (bUseLabels)
            //    sLabel = GetUserLabel(sItem);

            foreach (string sCurItem in cbxCur.Items)
            {
                if (sCurItem == sItem)
                    lsOnTop.Add(sCurItem);
                else
                    lsOthers.Add(sCurItem);
            }

            List<string> lsReturn = new List<string>();
            lsReturn.AddRange(lsOnTop.ToArray());
            lsReturn.AddRange(lsOthers.ToArray());
            while (lsReturn.Count > nNbMaxLines)
                lsReturn.RemoveAt(nNbMaxLines);

            cbxCur.Items.Clear();
            cbxCur.Items.AddRange(lsReturn.ToArray());
            return lsReturn;
        }

    }
}
