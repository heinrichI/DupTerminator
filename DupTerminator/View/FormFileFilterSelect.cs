using DupTerminator.Localize;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DupTerminator.View
{
    internal partial class FormFileFilterSelect : BaseForm
    {
        private List<classTypes> types;
        private static string string_Picture = "*.bmp;*.jp*g;*.gif;*.tif;*.png;*.pcx;*.tga;*.ico";
        private static string string_Audio = "*.mp3;*.ogg;*.wma;*.wav;*.ape;*.flac;*.m4p;*.m4a;*.aac;";
        private static string string_Video = "*.mov;*.avi;*.mp4;*.wmv;*.mkv;*.3gp";
        private static string string_Documents = "*.doc;*.xls;*.ppt;*.mdb;*.accdb;*.pdf;*.txt";
        private static string string_SavedSites = "*.css;*.htm;*.js;*.php;*.swf";

        public FormFileFilterSelect()
        {
            InitializeComponent();
        }

        public string GetSelectedExtension
        {
            get
            {
                return labelExten.Text;
            }
        }

        private void FormFileFilterSelect_Load(object sender, EventArgs e)
        {
            types = new List<classTypes>(5);
            types.Add(new classTypes(LanguageManager.GetString("classTypes_Picture"), string_Picture));
            types.Add(new classTypes(LanguageManager.GetString("classTypes_Audio"), string_Audio));
            types.Add(new classTypes(LanguageManager.GetString("classTypes_Video"), string_Video));
            types.Add(new classTypes(LanguageManager.GetString("classTypes_Documents"), string_Documents));
            types.Add(new classTypes(LanguageManager.GetString("classTypes_Saved sites"), string_SavedSites));

            for (int i = 0; i < types.Count; i++ )
            {
                listBoxFilters.Items.Insert(i, types[i].Name);
            }
        }

        private class classTypes
        {
            public string Name { get; set; }
            public string Types { get; set; }

            public classTypes(string s1, string s2)
            {
                Name = s1;
                Types = s2;
            }
        }

        private void listBoxFilters_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxFilters.SelectedIndex >= 0)
            {
                labelExten.Text = types[listBoxFilters.SelectedIndex].Types;
            }
        }


    }
}
