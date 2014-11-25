using System;
using System.Windows.Forms;

namespace DupTerminator
{
    partial class FormAbout : BaseForm
    {
        public FormAbout()
        {
            InitializeComponent();

            //  Initialize the FormAbout to display the product information from the assembly information.
            //  Change assembly information settings for your application through either:
            //  - Project->Properties->Application->Assembly Information
            //  - AssemblyInfo.cs

            this.Text = String.Format(LanguageManager.GetString("About"), AssemblyHelper.AssemblyTitle);
            this.lblProductName.Text = AssemblyHelper.AssemblyProduct; //+GC.GetTotalMemory(true).ToString(" Memory:0,0 byte");
            this.labelVersion.Text = String.Format(LanguageManager.GetString("Version2"), AssemblyHelper.AssemblyVersion);
            this.labelBuildDate.Text = AssemblyHelper.AssemblyBuildDate;
            this.labelCopyright.Text = AssemblyHelper.AssemblyCopyright;
            this.labelCompanyName.Text = AssemblyHelper.AssemblyCompany;
        }
               
       
        private void okButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(this.linkLabel1.Text);
            this.linkLabel1.LinkVisited = true;
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("mailto:" + linkLabel2.Text);
            this.linkLabel2.LinkVisited = true;
        }
    }
}
