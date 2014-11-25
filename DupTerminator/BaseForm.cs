//#define ExtLang

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DupTerminator
{
    internal partial class BaseForm : Form
    {
        public BaseForm()
        {
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            if (!DesignMode)
            {
                //Utils.UpdateFont(this, Utils.GetDefaultFont());
                #if !ExtLang
                LanguageManager.Localize(this);
                #endif
            }
            base.OnShown(e);
        }

        protected void UpdatePanelWidth(Panel panel)
        {
            int maxWidth = 0;
            int width = 0;
            SizeF size;
            //SizeF size2;

            foreach (Control control in panel.Controls)
            {
                if (control is Label)
                {
                    Label label = (Label)control;
                    //size = g.MeasureString(label.Text, label.Font);
                    size = TextRenderer.MeasureText(label.Text, label.Font);
                    width = label.Left + (int)size.Width;
                    maxWidth = Math.Max(width, maxWidth);
                }
                else if (control is RadioButton)
                {
                    RadioButton radioButton = (RadioButton)control;
                    //size = g.MeasureString(radioButton.Text, radioButton.Font);
                    size = TextRenderer.MeasureText(radioButton.Text, radioButton.Font);
                    width = radioButton.Left + (int)size.Width + 12;
                }
                else if (control is CheckBox)
                {
                    CheckBox checkBox = (CheckBox)control;
                   // size = g.MeasureString(checkBox.Text, checkBox.Font);
                    size = TextRenderer.MeasureText(checkBox.Text, checkBox.Font);
                    width = checkBox.Left + (int)size.Width + 12;
                }
                /*else if (control is Button)
                {
                    Button button = (Button)control;
                    //size = g.MeasureString(button.Text, button.Font);
                    //width = button.Left + (int)size.Width;
                    maxWidth = Math.Max(button.Width + button.Margin.Left + button.Margin.Right, maxWidth);
                }*/
                maxWidth = Math.Max(width, maxWidth);
            }

            panel.Width = maxWidth + 20;
        }

        public static bool IsDirectory(string filename)
        {
            char[] sep = new char[2];
            sep[0] = System.IO.Path.DirectorySeparatorChar;
            sep[1] = System.IO.Path.AltDirectorySeparatorChar;
            if (filename.IndexOfAny(sep) == -1)
            {
                return false; 
            }
            return true;
        }
    }
}
