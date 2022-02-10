using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace DupTerminator.View
{
    internal partial class FormJobLoad : BaseForm
    {
        public FormJobLoad()
        {
            InitializeComponent();
            textBoxJobName.Text = Application.StartupPath;
            if (string.IsNullOrEmpty(folderBrowserDialog1.SelectedPath))
                if (checkDirOnJobFiles(folderBrowserDialog1.SelectedPath))
                    m_btnOK.Enabled = true;
        }

        public String SelectedJob
        {
            get { return textBoxJobName.Text; }
            set { textBoxJobName.Text = value; }
        }

        private void m_btnOK_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxJobName.Text))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = textBoxJobName.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxJobName.Text = folderBrowserDialog1.SelectedPath;
                if (checkDirOnJobFiles(folderBrowserDialog1.SelectedPath))
                    m_btnOK.Enabled = true;
            }
        }

        private bool checkDirOnJobFiles(string dir)
        {
            if (File.Exists(Path.Combine(dir, Const.fileNameDirectorySearch)))// &&
                //File.Exists(Path.Combine(dir, Const.fileNameListDuplicate)))
                return true;
            return false;
        }

    }
}
