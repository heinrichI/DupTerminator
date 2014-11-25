using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DupTerminator
{
    internal partial class FormProgress : BaseForm
    {
        private int _max;

        public DBManager dbManager;

        public FormProgress()
        {
            InitializeComponent();
        }

        private void FormProgress_Shown(object sender, EventArgs e)
        {
            CenterToParent();
        }

        /// <summary>
        /// Set the progress message.
        /// </summary>
        /// <param name="message">Progress message shown in the form.</param>
        public void SetMessage(string message)
        {
            labelStatus.Text = message;
        }

        /// <summary>
        /// Set maximum value for the progress steps.
        /// </summary>
        /// <param name="count">Maximum progress step value.</param>
        public void SetMaxProgress(int count)
        {
            progressBar.Minimum = 0;
            progressBar.Maximum = count;
            _max = count;
        }

        /// <summary>
        /// Set current progress step/value.
        /// </summary>
        /// <param name="count">Current progress step/value.</param>
        public void SetCurrentProgress(int value)
        {
            //labelStatus.Text = String.Format("{0] / {0}", value, _max);
            labelStatus.Text = value + " / " + _max;
            progressBar.Value = value;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            dbManager.CancelDeleting();
        }

        public void Finish()
        {
            Close();
        }
    }
}
