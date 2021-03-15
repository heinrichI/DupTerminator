namespace DupTerminator.Views
{
    partial class FormUpdate
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnDownload = new System.Windows.Forms.Button();
            this.labelBuild = new System.Windows.Forms.Label();
            this.labelVersion = new System.Windows.Forms.Label();
            this.LabelChanges = new System.Windows.Forms.Label();
            this.textBoxChanges = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnDownload);
            this.panel1.Controls.Add(this.labelBuild);
            this.panel1.Controls.Add(this.labelVersion);
            this.panel1.Controls.Add(this.LabelChanges);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(653, 111);
            this.panel1.TabIndex = 0;
            // 
            // btnDownload
            // 
            this.btnDownload.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnDownload.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnDownload.Location = new System.Drawing.Point(499, 10);
            this.btnDownload.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnDownload.Name = "btnDownload";
            this.btnDownload.Size = new System.Drawing.Size(139, 68);
            this.btnDownload.TabIndex = 6;
            this.btnDownload.Text = "Download";
            this.btnDownload.UseVisualStyleBackColor = true;
            // 
            // labelBuild
            // 
            this.labelBuild.AutoSize = true;
            this.labelBuild.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelBuild.Location = new System.Drawing.Point(20, 47);
            this.labelBuild.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelBuild.Name = "labelBuild";
            this.labelBuild.Size = new System.Drawing.Size(72, 17);
            this.labelBuild.TabIndex = 4;
            this.labelBuild.Text = "Build on:";
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelVersion.Location = new System.Drawing.Point(20, 16);
            this.labelVersion.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(68, 17);
            this.labelVersion.TabIndex = 4;
            this.labelVersion.Text = "Version:";
            // 
            // LabelChanges
            // 
            this.LabelChanges.AutoSize = true;
            this.LabelChanges.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.LabelChanges.ForeColor = System.Drawing.Color.Navy;
            this.LabelChanges.Location = new System.Drawing.Point(20, 79);
            this.LabelChanges.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.LabelChanges.Name = "LabelChanges";
            this.LabelChanges.Size = new System.Drawing.Size(73, 17);
            this.LabelChanges.TabIndex = 3;
            this.LabelChanges.Text = "Changes:";
            // 
            // textBoxChanges
            // 
            this.textBoxChanges.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxChanges.Location = new System.Drawing.Point(0, 111);
            this.textBoxChanges.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.textBoxChanges.Multiline = true;
            this.textBoxChanges.Name = "textBoxChanges";
            this.textBoxChanges.ReadOnly = true;
            this.textBoxChanges.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxChanges.Size = new System.Drawing.Size(653, 166);
            this.textBoxChanges.TabIndex = 1;
            // 
            // FormUpdate
            // 
            this.AcceptButton = this.btnDownload;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(653, 277);
            this.Controls.Add(this.textBoxChanges);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "FormUpdate";
            this.Text = "Update";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnDownload;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label LabelChanges;
        private System.Windows.Forms.TextBox textBoxChanges;
        private System.Windows.Forms.Label labelBuild;
    }
}