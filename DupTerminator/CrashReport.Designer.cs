namespace DupTerminator
{
    partial class CrashReport
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CrashReport));
            this.label2 = new System.Windows.Forms.Label();
            this.userBox = new System.Windows.Forms.TextBox();
            this.buttonDontSend = new System.Windows.Forms.Button();
            this.buttonSend = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox = new System.Windows.Forms.TextBox();
            this.labelError = new System.Windows.Forms.Label();
            this.checkBoxRestart = new System.Windows.Forms.CheckBox();
            this.pictureBoxErr = new System.Windows.Forms.PictureBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.linkLabelView = new System.Windows.Forms.LinkLabel();
            this.groupBoxScreenshot = new System.Windows.Forms.GroupBox();
            this.pictureBoxScreenshot = new System.Windows.Forms.PictureBox();
            this.checkBoxIncludeScreenshot = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxErr)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.groupBoxScreenshot.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxScreenshot)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(9, 275);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(588, 55);
            this.label2.TabIndex = 14;
            this.label2.Text = resources.GetString("label2.Text");
            // 
            // userBox
            // 
            this.userBox.AcceptsReturn = true;
            this.userBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.userBox.Location = new System.Drawing.Point(7, 332);
            this.userBox.Margin = new System.Windows.Forms.Padding(2);
            this.userBox.Multiline = true;
            this.userBox.Name = "userBox";
            this.userBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.userBox.Size = new System.Drawing.Size(589, 64);
            this.userBox.TabIndex = 13;
            // 
            // buttonDontSend
            // 
            this.buttonDontSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDontSend.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonDontSend.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonDontSend.Location = new System.Drawing.Point(524, 432);
            this.buttonDontSend.Margin = new System.Windows.Forms.Padding(2);
            this.buttonDontSend.Name = "buttonDontSend";
            this.buttonDontSend.Size = new System.Drawing.Size(84, 38);
            this.buttonDontSend.TabIndex = 12;
            this.buttonDontSend.Text = "Don\'t Send";
            this.buttonDontSend.Click += new System.EventHandler(this.buttonDontSend_Click);
            // 
            // buttonSend
            // 
            this.buttonSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSend.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonSend.Location = new System.Drawing.Point(394, 432);
            this.buttonSend.Margin = new System.Windows.Forms.Padding(2);
            this.buttonSend.Name = "buttonSend";
            this.buttonSend.Size = new System.Drawing.Size(118, 38);
            this.buttonSend.TabIndex = 11;
            this.buttonSend.Text = "Send Error Report";
            this.buttonSend.Click += new System.EventHandler(this.buttonSend_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(41, 42);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(567, 55);
            this.label1.TabIndex = 10;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // textBox
            // 
            this.textBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox.Location = new System.Drawing.Point(5, 116);
            this.textBox.Margin = new System.Windows.Forms.Padding(2);
            this.textBox.Multiline = true;
            this.textBox.Name = "textBox";
            this.textBox.ReadOnly = true;
            this.textBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox.Size = new System.Drawing.Size(589, 144);
            this.textBox.TabIndex = 9;
            this.textBox.WordWrap = false;
            // 
            // labelError
            // 
            this.labelError.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelError.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelError.Location = new System.Drawing.Point(7, 3);
            this.labelError.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelError.Name = "labelError";
            this.labelError.Size = new System.Drawing.Size(589, 24);
            this.labelError.TabIndex = 8;
            this.labelError.Text = "Error";
            this.labelError.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // checkBoxRestart
            // 
            this.checkBoxRestart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxRestart.AutoSize = true;
            this.checkBoxRestart.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.checkBoxRestart.Location = new System.Drawing.Point(11, 451);
            this.checkBoxRestart.Name = "checkBoxRestart";
            this.checkBoxRestart.Size = new System.Drawing.Size(102, 17);
            this.checkBoxRestart.TabIndex = 15;
            this.checkBoxRestart.Text = "Restart Program";
            this.checkBoxRestart.UseVisualStyleBackColor = true;
            // 
            // pictureBoxErr
            // 
            this.pictureBoxErr.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pictureBoxErr.Location = new System.Drawing.Point(0, 45);
            this.pictureBoxErr.Name = "pictureBoxErr";
            this.pictureBoxErr.Size = new System.Drawing.Size(32, 32);
            this.pictureBoxErr.TabIndex = 16;
            this.pictureBoxErr.TabStop = false;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(616, 427);
            this.tabControl1.TabIndex = 17;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.pictureBoxErr);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.labelError);
            this.tabPage1.Controls.Add(this.userBox);
            this.tabPage1.Controls.Add(this.textBox);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(608, 401);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Exception";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.linkLabelView);
            this.tabPage2.Controls.Add(this.groupBoxScreenshot);
            this.tabPage2.Controls.Add(this.checkBoxIncludeScreenshot);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(608, 401);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Screenshot";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // linkLabelView
            // 
            this.linkLabelView.AutoSize = true;
            this.linkLabelView.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.linkLabelView.Location = new System.Drawing.Point(456, 9);
            this.linkLabelView.Name = "linkLabelView";
            this.linkLabelView.Size = new System.Drawing.Size(106, 13);
            this.linkLabelView.TabIndex = 6;
            this.linkLabelView.TabStop = true;
            this.linkLabelView.Text = "View Full Screenshot";
            this.linkLabelView.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelView_LinkClicked);
            // 
            // groupBoxScreenshot
            // 
            this.groupBoxScreenshot.Controls.Add(this.pictureBoxScreenshot);
            this.groupBoxScreenshot.Location = new System.Drawing.Point(8, 32);
            this.groupBoxScreenshot.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBoxScreenshot.Name = "groupBoxScreenshot";
            this.groupBoxScreenshot.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBoxScreenshot.Size = new System.Drawing.Size(579, 365);
            this.groupBoxScreenshot.TabIndex = 5;
            this.groupBoxScreenshot.TabStop = false;
            this.groupBoxScreenshot.Text = "Screenshot";
            // 
            // pictureBoxScreenshot
            // 
            this.pictureBoxScreenshot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxScreenshot.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pictureBoxScreenshot.Location = new System.Drawing.Point(3, 17);
            this.pictureBoxScreenshot.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBoxScreenshot.Name = "pictureBoxScreenshot";
            this.pictureBoxScreenshot.Size = new System.Drawing.Size(573, 344);
            this.pictureBoxScreenshot.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxScreenshot.TabIndex = 0;
            this.pictureBoxScreenshot.TabStop = false;
            // 
            // checkBoxIncludeScreenshot
            // 
            this.checkBoxIncludeScreenshot.AutoSize = true;
            this.checkBoxIncludeScreenshot.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.checkBoxIncludeScreenshot.Checked = true;
            this.checkBoxIncludeScreenshot.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIncludeScreenshot.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.checkBoxIncludeScreenshot.Location = new System.Drawing.Point(8, 7);
            this.checkBoxIncludeScreenshot.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.checkBoxIncludeScreenshot.Name = "checkBoxIncludeScreenshot";
            this.checkBoxIncludeScreenshot.Size = new System.Drawing.Size(118, 17);
            this.checkBoxIncludeScreenshot.TabIndex = 4;
            this.checkBoxIncludeScreenshot.Text = "Include Screenshot";
            this.checkBoxIncludeScreenshot.UseVisualStyleBackColor = true;
            // 
            // CrashReport
            // 
            this.AcceptButton = this.buttonSend;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonDontSend;
            this.ClientSize = new System.Drawing.Size(617, 478);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.checkBoxRestart);
            this.Controls.Add(this.buttonDontSend);
            this.Controls.Add(this.buttonSend);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "CrashReport";
            this.Text = "CrashReport";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ErrorReport_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxErr)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.groupBoxScreenshot.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxScreenshot)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox userBox;
        private System.Windows.Forms.Button buttonDontSend;
        private System.Windows.Forms.Button buttonSend;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.Label labelError;
        private System.Windows.Forms.CheckBox checkBoxRestart;
        private System.Windows.Forms.PictureBox pictureBoxErr;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.LinkLabel linkLabelView;
        private System.Windows.Forms.GroupBox groupBoxScreenshot;
        private System.Windows.Forms.PictureBox pictureBoxScreenshot;
        private System.Windows.Forms.CheckBox checkBoxIncludeScreenshot;
    }
}