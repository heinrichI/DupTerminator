using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DupTerminator.View
{
    public partial class FormProgress : BaseForm
    {
        public delegate void CancelDelegate();
        public event CancelDelegate CancelEvent;

        private int _max;

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
        public void SetProgressMax(int count)
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

        public delegate void PerformStepDelegate();
        //public void PerformStepEventHandler()
        public void PerformStepEventHandler()
        { 
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                object[] eventArgs = { };
                Invoke(new PerformStepDelegate(PerformStepEventHandler), eventArgs);
                return;
            }

            progressBar.PerformStep();
            labelStatus.Text = progressBar.Value + " / " + _max;
            //Application.DoEvents();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            //dbManager.CancelDeleting();
            CancelEvent();
        }

        public void Finish()
        {
            Close();
        }

        /// <summary>
        /// Устанавливет значение переданное значение для progressBar.
        /// </summary>
        /// <param name="count"></param>
        private delegate void ProgressChangedDelegate(int count);
        public void ProgressChangedEventHandler(int count)
        {
            if (InvokeRequired) // Проверяем в этом ли потоке нахождится созданый обьект 
            {
                object[] eventArgs = { count };
                Invoke(new ProgressChangedDelegate(ProgressChangedEventHandler), eventArgs);
                return;
            }

            SetCurrentProgress(count);
        }

        /// <summary> 
        /// Reports the progress to the UI thread, and waits for the UI thread to process
        /// the update before returning. This method should be called from the task. 
        /// </summary> 
        /// <param name="action">The action to perform in the context of the UI thread.</param> 
        public void ReportProgress(Action action)
        {
            //this.ReportProgressAsync(action).Wait();
            action.DynamicInvoke();
        }

        /// <summary> 
        /// Reports the progress to the UI thread. This method should be called from the task.
        /// Note that the progress update is asynchronous with respect to the reporting Task.
        /// For a synchronous progress update, wait on the returned <see cref="Task"/>. 
        /// </summary> 
        /// <param name="action">The action to perform in the context of the UI thread.
        /// Note that this action is run asynchronously on the UI thread.</param> 
        /// <returns>The task queued to the UI thread.</returns> 
        /*public Task ReportProgressAsync(Action action)
        {
            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, this.scheduler);
        }*/
    }
}
