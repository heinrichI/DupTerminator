using System;
//using System.Collections.Generic;
using System.Windows.Forms; //MessageBox
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Reflection; //Assembly

namespace DupTerminator.VersionManager
{
    [Serializable]
    public class VersionInfo
    {
        private int _major = 0;
        private int _minor = 0;
        private int _build = 0;
        private int _revision = 0;
        private string downloadWebPageAddress = string.Empty;
        private string changes = string.Empty;

        private const string curDownloadWebPageAddress = "http://sourceforge.net/projects/dupterminator/files/1.4/DupTerminator1.4.5639_Exe.zip";
        private const string curChanges =
@"Version 1.4
ListView is transferred to a virtual mode that means increased speed.";

        public VersionInfo()
        {
        }

        public VersionInfo(bool IsLocal) //Local
        {
            if (IsLocal)
            {
                this._major = Assembly.GetExecutingAssembly().GetName().Version.Major;
                this._minor = Assembly.GetExecutingAssembly().GetName().Version.Minor;
                this._build = Assembly.GetExecutingAssembly().GetName().Version.Build;
                this._revision = Assembly.GetExecutingAssembly().GetName().Version.Revision;
                this.downloadWebPageAddress = curDownloadWebPageAddress;
                this.changes = curChanges;
            }
        }

        public VersionInfo(string strVer, string WebPageAddress, string changes)
        {
            string[] strArray = strVer.Split(new char[] { '.' });
            this._major = Convert.ToInt32(strArray[0]);
            this._minor = Convert.ToInt32(strArray[1]);
            this._build = Convert.ToInt32(strArray[2]);
            this._revision = Convert.ToInt32(strArray[3]);

            this.downloadWebPageAddress = WebPageAddress;
            this.changes = changes;
        }

        /// <summary>
        /// Gets or sets the _major version number.
        /// </summary>
        /// <value>The _major version number.</value>
        public int Major
        {
            get { return _major; }
            set { _major = value; }
        }

        /// <summary>
        /// Gets or sets the _minor version number.
        /// </summary>
        /// <value>The _minor.</value>
        public int Minor
        {
            get { return _minor; }
            set { _minor = value; }
        }

        /// <summary>
        /// Gets or sets the _build version number.
        /// </summary>
        /// <value>The _build version number.</value>
        public int Build
        {
            get { return _build; }
            set { _build = value; }
        }

        /// <summary>
        /// Gets or sets the _revision version number.
        /// </summary>
        /// <value>The _revision version number.</value>
        public int Revision
        {
            get { return _revision; }
            set { _revision = value; }
        }

        /// <summary>
        /// Gets or sets the download web page address.
        /// </summary>
        /// <value>The download web page address.</value>
        public string DownloadWebPageAddress
        {
            get { return downloadWebPageAddress; }
            set { downloadWebPageAddress = value; }
        }

        public string Changes
        {
            get { return changes; }
            set { changes = value; }
        }

        public string VersionString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(_major.ToString());
            builder.Append(".");
            builder.Append(_minor.ToString());
            builder.Append(".");
            builder.Append(_build.ToString());
            builder.Append(".");
            builder.Append(_revision.ToString());

            return builder.ToString();
        }

        public string BuildDate()
        {
            Version ver = Assembly.GetExecutingAssembly().GetName().Version;
            int build = Convert.ToInt32(ver.Build);
            int rev = Convert.ToInt32(ver.Revision);
            DateTime buildDate = new DateTime(2000, 1, 1);
            buildDate = buildDate.AddDays(build);
            buildDate = buildDate.AddSeconds(rev * 2);
            return String.Format(LanguageManager.GetString("BuildOn"), buildDate.ToShortDateString(), buildDate.ToLongTimeString());
        }

        public static bool Compatible(VersionInfo v1, VersionInfo v2)
        {
            return ((v1._major == v2._major) &&
                    (v1._minor == v2._minor) &&
                    (v1.Build == v2.Build) &&
                    (v1.Revision == v2.Revision));
        }

        /// <summary>
        /// Saves VersionInfo into specified file.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public void SaveXml(string filename)
        {
            try
            {
                XmlWriterSettings ws = new XmlWriterSettings();
                ws.NewLineHandling = NewLineHandling.Entitize;
                // включаем отступ для элементов XML документа
                ws.Indent = true;
                // задаем переход на новую строку
                //ws.NewLineChars = "\n";

                XmlSerializer ser = new XmlSerializer(typeof(VersionInfo));
                using (XmlWriter wr = XmlWriter.Create(filename, ws))
                {
                     ser.Serialize(wr, this);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
 }

        /// <summary>
        /// Loads VersionInfo from specified file.
        /// </summary>
        /// <param name="filename">The filename.</param>
        //public static VersionInfo Load(string filename)
        public static VersionInfo LoadXml (Stream stream)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(VersionInfo));
                return (VersionInfo)serializer.Deserialize(stream);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                //throw ex;
                return null;
            }
        }
    }
}
