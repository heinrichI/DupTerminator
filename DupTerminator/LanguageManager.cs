/* ********************************************************************************
 * 
 * Copyright (c) 2010 Alexandr Vodorez (alexandr.vodorez@gmail.com)
 * This software licensed under GPL (http://www.opensource.org/licenses/gpl-license.php) license.
 * 
 ***********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace DupTerminator
{
    class LanguageManager
    {
        private static XmlLocalizer localizer;

        internal static string GetProperty(Form form, string key)
        {
            return GetLocalizer().GetProperty(form.GetType().FullName, key);
        }

        internal static string GetString(string key)
        {
            return GetLocalizer().GetString(key);
        }

        internal static string GetString(string key, params object[] args)
        {
            return GetLocalizer().GetString(key, args);
        }

        internal static void Localize(Form form)
        {
            GetLocalizer().Localize(form);
        }

        //internal static void Localize(UserControl userControl)
        //{
        //    GetLocalizer().Localize(userControl);
        //}

        internal static IEnumerable<string> Languages
        {
            get { return GetLocalizer().Languages; }

        }

        internal static void SetLanguage(string lang)
        {
            GetLocalizer().CurrentLanguage = lang;
        }

        internal static XmlLocalizer GetLocalizer()
        {
            if (localizer != null)
                return localizer;

            string startDirectory = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            string languageDirectory = Path.Combine(startDirectory, "Languages");
            localizer = new XmlLocalizer(languageDirectory);
            return localizer;
        }

        internal static string GetNativeName(string lang)
        {
            return GetLocalizer().GetNativeName(lang);
        }

        internal static XmlLocalizer.Language GetLanguageInfo(string lang)
        {
            return GetLocalizer().GetLanguageInfo(lang);
        }
    }
}
