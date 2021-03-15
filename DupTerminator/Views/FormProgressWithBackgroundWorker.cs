using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DupTerminator.Views
{
    public partial class FormProgressWithBackgroundWorker : Form
    {
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private int _maxProgress;

        public FormProgressWithBackgroundWorker(string whyWeAreWaiting, int maxProgress, DoWorkEventHandler work)
        {
            InitializeComponent();

            progressBar1.Minimum = 0;
            progressBar1.Maximum = maxProgress;
            progressBar1.Step = 1;
            _maxProgress = maxProgress;

            this.Text = whyWeAreWaiting; // Show in title bar
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            backgroundWorker1.DoWork += work; // Event handler to be called in context of new thread.
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            label1.Text = "Cancel pending";
            backgroundWorker1.CancelAsync(); // Tell worker to abort.
            buttonCancel.Enabled = false;
        }

        private void Progress_Load(object sender, EventArgs e)
        {
            CenterToParent();
            backgroundWorker1.RunWorkerAsync();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            /*progressBar1.Value = e.ProgressPercentage;
            label1.Text = e.UserState as string;*/
            progressBar1.PerformStep();
            label1.Text = progressBar1.Value + " / " + _maxProgress;
            if (e.UserState != null)
                Text = e.UserState as string;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Close();
        }
    }
}
