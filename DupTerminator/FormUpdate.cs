using System.Windows.Forms;

namespace DupTerminator
{
    internal partial class FormUpdate : BaseForm
    {
        public FormUpdate()
        {
            InitializeComponent();
        }

        public string Version
        {
            get { return this.labelVersion.Text;}
            set { labelVersion.Text = value; }
        }

        public string Changes
        {
            get { return this.textBoxChanges.Text; }
            set { textBoxChanges.Text = value; }
        }

        public string BuildDate
        {
            get { return this.labelBuild.Text; }
            set { labelBuild.Text = value; }
        }
    }
}
