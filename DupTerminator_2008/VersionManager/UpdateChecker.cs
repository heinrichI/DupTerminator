using System;
using System.Collections.Generic;
using System.Text;
using System.Net; //WebClient
using System.IO; //File
using System.Threading;
using System.Windows.Forms;


namespace DupTerminator.VersionManager
{
    class UpdateChecker
    {
        //public Settings settings = new Settings(); //экземпляр класса с настройками
        private Settings settings;
        private bool m_downloadingFinished;
        private System.Windows.Forms.Timer m_timer;
        private VersionInfo onlineVersion = null;
        private VersionInfo localVersion = null;
        private bool ShowMessage = false;

        public delegate void NewVersionCheckedHandler(bool newVersion, VersionInfo versionInfo, bool showFormVersion);
        public event NewVersionCheckedHandler VersionChecked;

        private const string urlVersion = "http://sourceforge.net/projects/dupterminator/files/version.xml";

        //#region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateChecker"/> class.
        /// </summary>
        public UpdateChecker(bool showMessage)
        {
            ShowMessage = showMessage;
            settings = Settings.GetInstance();
            InitializeComponents();
            InitializeVersions();
        }

        private void InitializeComponents()
        {
            this.m_timer = new System.Windows.Forms.Timer();
            this.m_timer.Interval = 1000;
            this.m_timer.Tick += new EventHandler(this.TimerCallback);
            this.m_timer.Start();
        }

        private void InitializeVersions()
        {
            localVersion = new VersionManager.VersionInfo(true);
            new Thread(new ThreadStart(this.OnlineVersionDownloadThreadTask)).Start();
        }

        private void OnlineVersionDownloadThreadTask()
        {
            try
            {
                byte[] buffer = new WebClient().DownloadData(urlVersion);
                onlineVersion = VersionInfo.LoadXml(new MemoryStream(buffer));
            }
            catch (Exception ex)
            {
                if (ShowMessage)
                    MessageBox.Show(ex.Message);

                onlineVersion = null;
            }
            this.m_downloadingFinished = true;
        }

        private void TimerCallback(object obj, EventArgs eventArgs)
        {
            if (this.m_downloadingFinished)
            {
                this.m_timer.Stop();
                if (onlineVersion != null)
                {
                    if (!VersionManager.VersionInfo.Compatible(localVersion, onlineVersion))
                    {
                        VersionChecked(true, onlineVersion, ShowMessage);
                    }
                    else
                    {
                        VersionChecked(false, onlineVersion, ShowMessage);
                    }
               }
               else
                    VersionChecked(false, null, ShowMessage);
            }
        }

    }
}
