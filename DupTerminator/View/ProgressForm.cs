using DupTerminator.BusinessLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DupTerminator.View
{
    public partial class ProgressForm : Form
    {
        public event EventHandler Cancelled;

        Dictionary<string, ProgressModel> _models = new Dictionary<string, ProgressModel>();

        public ProgressForm()
        {
            InitializeComponent();

            buttonCancel.Click += (s, e) => Cancelled?.Invoke(s, e);
        }

        internal void UpdateProgress(ProgressDto progressDto)
        {
            if (progressDto.PhisicalDrive is null)
            {
                if (!_models.ContainsKey("Unknown"))
                {
                    var progressModel = new ProgressModel { Status = progressDto.Status, PhisicalDrive = "Unknown" };
                    _models.Add("Unknown", progressModel);

                    CreatePanel(progressModel);
                }
                else
                {
                    _models["Unknown"].Status = progressDto.Status;
                }
            }
            else if (!_models.ContainsKey(progressDto.PhisicalDrive))
            {
                var progressModel = new ProgressModel { Status = progressDto.Status, PhisicalDrive = progressDto.PhisicalDrive };
                _models.Add(progressDto.PhisicalDrive, progressModel);

                CreatePanel(progressModel);
            }
            else
            {
                _models[progressDto.PhisicalDrive].Status = progressDto.Status;
            }
        }

        private void CreatePanel(ProgressModel progressModel)
        {
            AddRowToPanel(tableLayoutPanelStatuses, new string[2]);

            var label = new Label();
            label.Dock = DockStyle.Fill;
            label.DataBindings.Add("Text", progressModel, nameof(ProgressModel.Status));
            tableLayoutPanelStatuses.Controls.Add(new Label() 
            { 
                Text = progressModel.PhisicalDrive, 
                //AutoSize = true 
            }, 0, tableLayoutPanelStatuses.RowCount - 1);
            tableLayoutPanelStatuses.Controls.Add(label, 1, tableLayoutPanelStatuses.RowCount - 1);
        }

        class ProgressModel : BasePropertyChanged
        {

            private string _status;

            public string Status
            {
                get { return _status; }
                set 
                {
                    if (value != _status)
                    {
                        _status = value;
                        RaisePropertyChangedEvent();
                    }
                }
            }


            private string _phisicalDrive;

            public string PhisicalDrive
            {
                get { return _phisicalDrive; }
                set
                {
                    if (value != _phisicalDrive)
                    {
                        _phisicalDrive = value;
                        RaisePropertyChangedEvent();
                    }
                }
            }
        }

        private static void AddRowToPanel(TableLayoutPanel panel, string[] rowElements)
        {
            if (panel.ColumnCount != rowElements.Length)
                throw new ArgumentException("Elements number doesn't match!");
            //get a reference to the previous existent row
            RowStyle temp = panel.RowStyles[panel.RowCount - 1];
            //increase panel rows count by one
            panel.RowCount++;
            //add a new RowStyle as a copy of the previous one
            panel.RowStyles.Add(new RowStyle(temp.SizeType, temp.Height));
            //add the control
            //for (int i = 0; i < rowElements.Length; i++)
            //{
            //    panel.Controls.Add(new Label() { Text = rowElements[i] }, i, panel.RowCount - 1);
            //}
        }
    }
}
