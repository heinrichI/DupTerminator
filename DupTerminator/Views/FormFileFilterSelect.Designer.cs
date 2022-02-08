namespace DupTerminator.Views
{
    partial class FormFileFilterSelect
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBoxFilters = new System.Windows.Forms.ListBox();
            this.labelExten = new System.Windows.Forms.Label();
            this.buttonSelect = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listBoxFilters
            // 
            this.listBoxFilters.FormattingEnabled = true;
            this.listBoxFilters.Location = new System.Drawing.Point(12, 12);
            this.listBoxFilters.Name = "listBoxFilters";
            this.listBoxFilters.Size = new System.Drawing.Size(329, 95);
            this.listBoxFilters.TabIndex = 0;
            this.listBoxFilters.SelectedIndexChanged += new System.EventHandler(this.listBoxFilters_SelectedIndexChanged);
            // 
            // labelExten
            // 
            this.labelExten.Location = new System.Drawing.Point(12, 117);
            this.labelExten.Name = "labelExten";
            this.labelExten.Size = new System.Drawing.Size(326, 27);
            this.labelExten.TabIndex = 1;
            // 
            // buttonSelect
            // 
            this.buttonSelect.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonSelect.Location = new System.Drawing.Point(47, 151);
            this.buttonSelect.Name = "buttonSelect";
            this.buttonSelect.Size = new System.Drawing.Size(110, 31);
            this.buttonSelect.TabIndex = 2;
            this.buttonSelect.Text = "&Select";
            this.buttonSelect.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(192, 151);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(110, 32);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // FormFileFilterSelect
            // 
            this.AcceptButton = this.buttonSelect;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(351, 194);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSelect);
            this.Controls.Add(this.labelExten);
            this.Controls.Add(this.listBoxFilters);
            this.Name = "FormFileFilterSelect";
            this.Text = "File Filter Select";
            this.Load += new System.EventHandler(this.FormFileFilterSelect_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxFilters;
        private System.Windows.Forms.Label labelExten;
        private System.Windows.Forms.Button buttonSelect;
        private System.Windows.Forms.Button buttonCancel;
    }
}