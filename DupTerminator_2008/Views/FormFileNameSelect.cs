using System;
using System.Windows.Forms;

namespace DupTerminator.Views
{
    internal partial class FormFileNameSelect : BaseForm
    {
        public FormFileNameSelect()
        {
            InitializeComponent();
        }

        public String SelectedName
        {
            get { return textBoxFileName.Text.ToLower(); }
            set { textBoxFileName.Text = value; }
        }

        private void m_btnOK_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxFileName.Text))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
