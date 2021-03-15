/* ********************************************************************************
 * 
 * Copyright (c) 2010 Alexandr Vodorez (alexandr.vodorez@gmail.com)
 * This software licensed under GPL (http://www.opensource.org/licenses/gpl-license.php) license.
 * 
 ***********************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;

namespace DupTerminator
{
    internal class XmlLocalizer
    {
        private string directory;
        private string schemaFile;
        private XmlSchema schema;
        private readonly string defualtLanguage = "en";
        private string currentLanguage = null;

        private Dictionary<string, XmlDocument> languages;

        public XmlLocalizer()
        {
            var comparer = StringComparer.Create(CultureInfo.InvariantCulture, true);
            languages = new Dictionary<string, XmlDocument>(comparer);
        }

        public XmlLocalizer(string directory): this()
        {
            if (!Directory.Exists(directory))
            {
                //throw new Exception(String.Format("Directory for language files {0} not exists.", directory));
                MessageBox.Show(String.Format("Directory for language files {0} not exists.", directory));
                Process.GetCurrentProcess().Kill();
            }

            this.directory = directory;
            schemaFile = Path.Combine(directory, "language.xsd");

            if (!File.Exists(schemaFile))
                throw new FileNotFoundException(String.Format("Language validation schema file {0} not found.", schemaFile));

            schema = XmlSchema.Read(XmlReader.Create(schemaFile), null);

            ScanLanguages();
        }

        private void ScanLanguages()
        {
            string[] files = Directory.GetFiles(directory, "*.xml");
            foreach (string file in files)
                ValidateAndLoadLanguage(file);
            currentLanguage = defualtLanguage;
        }


        private bool ValidateAndLoadLanguage(string file)
        {
            try
            {
                var settings = new XmlReaderSettings();
                settings.Schemas.Add(schema);
                settings.ValidationType = ValidationType.Schema;

                var reader = XmlReader.Create(file, settings);

                var document = new XmlDocument();
                document.Load(reader);

                string culture = document.DocumentElement.GetAttribute("culture");
                string name = document.DocumentElement.GetAttribute("name");


                if (languages.ContainsKey(culture))
                    throw new Exception("Duplicate language in file " + Path.GetFileName(file));

                languages.Add(culture, document);

                return true;
            }
            catch (Exception ex)
            {
                //TraceOutput.Error("The language file {0} has invalid format.\n {1}", Path.GetFileName(file), ex.Message);
                MessageBox.Show(String.Format("The language file {0} has invalid format.\n {1}", Path.GetFileName(file), ex.Message));
            }

            return false;
        }

        public IEnumerable<string> Languages
        {
            get
            {
                foreach (KeyValuePair<string, XmlDocument> item in languages)
                    yield return item.Key;
            }
        }

        public IEnumerable<CultureInfo> Cultures
        {
            get
            {
                foreach (var item in languages)
                    yield return CultureInfo.GetCultureInfo(item.Key);
            }
        }

        public string GetNativeName(string lang)
        {
            if (languages.ContainsKey(lang))
            {
                var doc = languages[lang];
                string name = doc.DocumentElement.GetAttribute("name");
                if (!String.IsNullOrEmpty(name))
                    return name;
            }
            
            var cultureInfo = CultureInfo.GetCultureInfo(lang);
            return String.Concat(Char.ToUpper(cultureInfo.NativeName[0]), cultureInfo.NativeName.Substring(1));
        }


        public string CurrentLanguage
        {
            get { return currentLanguage; }
            set
            {
                CheckLanguage(value);
                currentLanguage = value;
            }
        }

        private void CheckLanguage(string lang)
        {
            if (!languages.ContainsKey(lang))
                throw new Exception(String.Format("The language {0} not found.", lang));
        }

        public void Localize(Form form)
        {
            string type = form.GetType().FullName;
            string path = String.Format("/*/forms/form[@type='{0}']", type);

            Localize(form, path);
        }

        //public void Localize(UserControl userControl)
        //{
        //    string type = userControl.GetType().FullName;
        //    string path = String.Format("/*/controls/control[@type='{0}']", type);

        //    Localize(userControl, path);
        //}
        

        private void Localize(Control control, string path)
        {
            CheckLanguage(currentLanguage);
            XmlDocument document = languages[currentLanguage];
            XmlNode controlNode = document.SelectSingleNode(path);

            if (controlNode == null)
                throw new Exception(String.Format("Element {0} not found for language {1}", path, currentLanguage));

            foreach (XmlNode propNode in controlNode.SelectNodes("property"))
            {
                string propName = propNode.Attributes["name"].Value.Trim();
                string propValue = propNode.Attributes["value"].Value;

                if (String.IsNullOrEmpty(propName))
                    continue;

                string[] parts = propName.Split(".".ToCharArray());

                if (parts.Length == 1)
                {
                    SetPropValue(control, propName, propValue);
                }
                else
                {
                    Component parent = control;
                    Component target = null;

                    propName = parts[parts.Length - 1];

                    for (int i = 0; i < parts.Length - 1; i++)
                    {

                        string targetName = parts[i];

                        if (string.IsNullOrEmpty(targetName))
                            break;

                        target = FindChild(parent, targetName);
                        if (target != null)
                        {
                            if (i == parts.Length - 2)
                            {
                                SetPropValue(target, propName, propValue);
                                break;
                            }
                            parent = target;
                            continue;
                        }
                        //TraceOutput.Warning("Control {0} not found in path {1} for {2}", targetName, propName, control.GetType().FullName);
                        MessageBox.Show(String.Format("Control {0} not found in path {1} for {2}", targetName, propName, control.GetType().FullName));
                    }
                }
            }
            //LocalizeUserControls(control);
        }

        private Component FindChild(Component parent, string childControlName)
        {
            if (parent is Control)
            {
                Control[] childs = ((Control) parent).Controls.Find(childControlName, false);
                if (childs.Length > 0)
                    return childs[0];
            }

            Type controlType = parent.GetType();

            FieldInfo fieldInfo = controlType.GetField(childControlName);

            if (fieldInfo == null)
                fieldInfo = controlType.GetField(childControlName,
                                 BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
                return null;

            if (fieldInfo.FieldType.IsSubclassOf(typeof(Component)))
                return (Component) fieldInfo.GetValue(parent);

            return null;
        }

        private void SetPropValue(Component target, string propName, string propValue)
        {
            PropertyInfo propInfo = target.GetType().GetProperty(propName);
            
            if (propInfo == null)
            {
                //TraceOutput.Warning("Property {0} not found in type {1}", propName, target.GetType().FullName);
                MessageBox.Show(String.Format("Property {0} not found in type {1}", propName, target.GetType().FullName));
                return;
            }

            if (propInfo.PropertyType == typeof(String))
            {
                propInfo.SetValue(target, propValue, new object[0]);
            }
            else
            {
                TypeConverter converter = TypeDescriptor.GetConverter(propInfo.PropertyType);
                if (converter.CanConvertFrom(typeof(String)))
                {
                    object value = converter.ConvertFrom(propValue);
                    propInfo.SetValue(target, value, new object[0]);
                }
            }
        }

        public string GetProperty(string form, string key)
        {
            CheckLanguage(currentLanguage);
            XmlDocument document = languages[currentLanguage];
            XmlNode propertyNode = document.SelectSingleNode(String.Format("/language/forms/form[@type='{0}']/property[@name='{1}']", form, key));
            if (propertyNode == null)
            {
                MessageBox.Show(String.Format("Property key '{0}' not found for language {1}", key, currentLanguage));
                return String.Empty;
            }

            XmlNode valueNode = propertyNode.Attributes.GetNamedItem("value");
            if (valueNode != null)
                return valueNode.Value;

            return propertyNode.InnerText;
        }

        public string GetString(string key)
        {
            CheckLanguage(currentLanguage);
            XmlDocument document = languages[currentLanguage];
            XmlNode messageNode = document.SelectSingleNode(String.Format("/language/messages/message[@name='{0}']", key));
            if (messageNode == null)
            {
                //TraceOutput.Warning("Message key '{0}' not found for language {1}", key, currentLanguage);
                MessageBox.Show(String.Format("Message key '{0}' not found for language {1}", key, currentLanguage));
                return String.Empty;
            }

            XmlNode valueNode = messageNode.Attributes.GetNamedItem("value");
            if (valueNode != null)
                return valueNode.Value;

            return messageNode.InnerText;
        }

        public string GetString(string key, params object[] args)
        {
            return String.Format(GetString(key), args);
        }

        public Language GetLanguageInfo(string lang)
        {
            CheckLanguage(lang);
            XmlDocument document = languages[lang];

            Language res = new Language();

            XmlNode node = document.SelectSingleNode("/language/@culture");
            if (node != null)
                res.Culture = node.InnerText;

            node = document.SelectSingleNode("/language/author");
            if (node != null)
                res.Author = node.InnerText;

            node = document.SelectSingleNode("/language/version");
            if (node != null)
                res.Version = node.InnerText;

            return res;
        }

        public class Language
        {
            public string Culture { get; set; }
            public string Author { get; set; }
            public string Version { get; set; }
        }
    }


}
