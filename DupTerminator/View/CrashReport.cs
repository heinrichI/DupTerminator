//From ChronosXP
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Diagnostics; //StackTrace
using System.Net.Mail; //Mail
using System.Net;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;
using System.Threading;
using DupTerminator.Localize;

namespace DupTerminator.View
{
    internal partial class CrashReport : BaseForm
    {
        #region URLs, etc.
        // URL's used in various parts of the program
        public const string SMTPServer = "smtp.mail.ru";
        private const string feedbackAddress = "crashreportbd@mail.ru";
        private const string feedbackPassword = "crash@_0508";
        #endregion

        private string _screenShot;

        public CrashReport(Exception ex)                        : this(null, ex, null, null, null)        { }
        public CrashReport(string msg)                          : this(msg, null, null, null, null) { }
        public CrashReport(string msg, ListView lv)             : this(msg, null, null, lv, null) { }
        public CrashReport(string msg, Exception ex)            : this(msg, ex, null, null, null) { }
        public CrashReport(Exception ex, Settings settings)     : this(null, ex, settings, null, null) { }
        public CrashReport(Exception ex, Settings settings, ListView lv) : this(null, ex, settings, lv, null) { }

        public CrashReport(string msg, Settings settings)       : this(msg, null, settings, null, null) { }
        public CrashReport(string msg, Settings settings, ListView lv) : this(msg, null, settings, lv, null) { }
        public CrashReport(string msg, Settings settings, ListView lv, List<ListViewItemSave> items) : this(msg, null, settings, lv, items) { }
        //public ErrorReport(string msg, Exception ex, Config conf) : this(String.Concat("Error reading from registry: ", msg), ex, conf, null) 
        public CrashReport(string msg, Exception ex, Settings settings, ListView lv, List<ListViewItemSave> items)
        {
#if LOG
            Log.Write(LogLevel.Info, "ErrorReporting.");
#endif

            InitializeComponent();

#if !ExtLang
            LanguageManager.Localize(this);
#endif

            if (msg == null)
                labelError.Text = LanguageManager.GetString("Crash_UnknownError");//"An unknown error has occurred. WallpaperChanger cannot continue.";
            else
                labelError.Text = msg;

            // Set picturebox to error
            this.pictureBoxErr.Image = SystemIcons.Error.ToBitmap();

            try
            {
                CaptureScreenshot captureScreenshot = new CaptureScreenshot();
                _screenShot = string.Format(@"{0}\{1} Crash Screenshot.png", Path.GetTempPath(), AssemblyHelper.AssemblyTitle);
                captureScreenshot.CaptureScreenToFile(_screenShot, ImageFormat.Png);
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
            }
            if (File.Exists(_screenShot))
            {
                pictureBoxScreenshot.ImageLocation = _screenShot;
                pictureBoxScreenshot.Show();
            }

            CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture;
            if (oldCulture.Parent.Name != "ru")
            {
                //Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en");
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en");
                // throw new Exception here => Culture is in english 
                //MessageBox.Show(new FileNotFoundException().Message);
            }

            StringBuilder sb = new StringBuilder();

            if (ex != null)
                sb.AppendLine(ex.Message);
            sb.AppendLine();

            //programm and versions
            //sb.AppendLine(Application.ProductName);
            sb.AppendLine(AssemblyHelper.AssemblyTitle);
            //sb.AppendLine(Application.ProductVersion);
            sb.AppendLine(string.Format("Version: {0}", AssemblyHelper.AssemblyVersion));
            sb.AppendLine(AssemblyHelper.AssemblyBuildDate);

            Process proc = Process.GetCurrentProcess();
            // dates and time
            sb.AppendLine(string.Format("Current Date/Time: {0}", DateTime.Now.ToString()));
            sb.AppendLine(string.Format("Current UtcDate/Time: {0}", DateTime.UtcNow.ToString()));
            sb.AppendLine(string.Format("Process Date/Time: {0}", proc.StartTime.ToString()));
            //sb.AppendLine(string.Format("Build Date: {0}", Properties.Settings.Default.strBuildTime));
            // os info
            sb.AppendLine(string.Format("OS: {0}", Environment.OSVersion.VersionString));
            sb.AppendLine(string.Format("OS info: {0}", getOSInfo()));
            sb.AppendLine(string.Format(".NET Framework (CLR): {0}", Environment.Version.ToString()));
            sb.AppendLine(string.Format("Language: {0}", Application.CurrentCulture.ToString()));
            sb.AppendLine(string.Format("CurrentCulture: {0}", System.Threading.Thread.CurrentThread.CurrentCulture.ToString()));
            sb.AppendLine(string.Format("CurrentUICulture: {0}", System.Threading.Thread.CurrentThread.CurrentUICulture.ToString()));
            // uptime stats
            sb.AppendLine(string.Format("System Uptime: {0} Days {1} Hours {2} Mins {3} Secs", Math.Round((decimal)Environment.TickCount / 86400000), Math.Round((decimal)Environment.TickCount / 3600000 % 24), Math.Round((decimal)Environment.TickCount / 120000 % 60), Math.Round((decimal)Environment.TickCount / 1000 % 60)));
            sb.AppendLine(string.Format("Program Uptime: {0}", proc.TotalProcessorTime.ToString()));
            // process id
            sb.AppendLine(string.Format("PID: {0}", proc.Id));
            // exe name
            sb.AppendLine(string.Format("Executable: {0}", Application.ExecutablePath));
            sb.AppendLine(string.Format("Process Name: {0}", proc.ToString()));
            sb.AppendLine(string.Format("Main Module Name: {0}", proc.MainModule.ModuleName));
            // exe stats
            sb.AppendLine(string.Format("Module Count: {0}", proc.Modules.Count));
            sb.AppendLine(string.Format("Thread Count: {0}", proc.Threads.Count));
            sb.AppendLine(string.Format("Thread ID: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId));
            sb.AppendLine(string.Format("Is Debugged: {0}", Debugger.IsAttached));

            sb.AppendLine();
            sb.AppendLine("Running processes: ");
            foreach (System.Diagnostics.Process winProc in System.Diagnostics.Process.GetProcesses())
            {
                //sb.AppendLine(string.Format("{0}: {1} {2}", winProc.Id, winProc.ProcessName, winProc.MainWindowTitle));
                sb.Append(string.Format("{0}: {1}, ", winProc.Id, winProc.ProcessName));
            }
            sb.AppendLine();

            if (settings == null)
            {
                sb.AppendLine("Settings unknown.");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("Settings:");
                sb.AppendLine(string.Format("IsConfirmDelete: {0}", settings.Fields.IsConfirmDelete));
                sb.AppendLine(string.Format("IsCheckUpdate: {0}", settings.Fields.IsCheckUpdate));
                sb.AppendLine(string.Format("IsOrientationVert: {0}", settings.Fields.IsOrientationVert));
                sb.AppendLine(string.Format("IsSameFileName: {0}", settings.Fields.IsSameFileName));
                sb.AppendLine(string.Format("IsSaveLoadListDub: {0}", settings.Fields.IsSaveLoadListDub));
                sb.AppendLine(string.Format("IsAllowDelAllFiles: {0}", settings.Fields.IsAllowDelAllFiles));
                sb.AppendLine(string.Format("IsScanMax: {0}", settings.Fields.IsScanMax));
                sb.AppendLine(string.Format("MaxFile: {0}", settings.Fields.MaxFile));
                sb.AppendLine(string.Format("PathHistoryLength: {0}", settings.Fields.PathHistoryLength));
                sb.AppendLine(string.Format("limits: [{0}, {1}]", settings.Fields.limits[0], settings.Fields.limits[1]));
                sb.AppendLine(string.Format("IncludePattern: {0}", settings.Fields.IncludePattern));
                sb.AppendLine(string.Format("ExcludePattern: {0}", settings.Fields.ExcludePattern));
                sb.AppendLine(string.Format("ProgramFont: {0}", settings.Fields.ProgramFont.ToFont().ToString()));
                sb.AppendLine(string.Format("ListRowFont: {0}", settings.Fields.ListRowFont.ToFont().ToString()));
                sb.AppendLine(string.Format("ColorRow1: {0}", settings.Fields.ColorRow1.ToColor().ToString()));
                sb.AppendLine(string.Format("ColorRow2: {0}", settings.Fields.ColorRow2.ToColor().ToString()));
                sb.AppendLine(string.Format("ColorRowError: {0}", settings.Fields.ColorRowError.ToColor().ToString()));
                sb.AppendLine(string.Format("ColorColorRowNotExist: {0}", settings.Fields.ColorRowNotExist.ToColor().ToString()));
                sb.AppendLine(string.Format("Language: {0}", settings.Fields.Language));
                sb.AppendLine(string.Format("LastJob: {0}", settings.Fields.LastJob));
                sb.AppendLine(string.Format("FastCheck: {0}", settings.Fields.FastCheck));
                sb.AppendLine(string.Format("FastCheckFileSizeMb: {0}", settings.Fields.FastCheckFileSizeMb));
                sb.AppendLine(string.Format("FastCheckBufferKb: {0}", settings.Fields.FastCheckBufferKb));
                sb.AppendLine(string.Format("UseDB: {0}", settings.Fields.UseDB));
                sb.AppendLine();
            }

            if (lv != null)
            {
                sb.AppendLine("ListView:");
                sb.AppendLine(string.Format("Name: {0}", lv.Name));
                sb.AppendLine(string.Format("Items: {0}", lv.Items.Count));
                sb.AppendLine(string.Format("Groups: {0}", lv.Groups.Count));
                sb.AppendLine(string.Format("VirtualListSize: {0}", lv.VirtualListSize));
                sb.AppendLine();
            }

            if (items != null)
            {
                sb.AppendLine("ListViewItemSave:");
                sb.AppendLine(string.Format("Count: {0}", items.Count));
                sb.AppendLine();
            }

            sb.AppendLine(string.Format("User Error: {0}", labelError.Text));
            Exception exep = ex;
            for (int i = 0; exep != null; exep = exep.InnerException, i++)
            {
                sb.AppendLine();
                sb.AppendLine(string.Format("Type #{0} {1}", i, ex.GetType().ToString()));

                foreach (System.Reflection.PropertyInfo propInfo in ex.GetType().GetProperties())
                {
                    string fieldName = string.Format("{0} №{1}", propInfo.Name, i);
                    string fieldValue = string.Format("{0}", propInfo.GetValue(ex, null));

                    // Ignore stack trace + data
                    /*if (propInfo.Name == "StackTrace"
                        || propInfo.Name == "Data"
                        || string.IsNullOrEmpty(propInfo.Name)
                        || string.IsNullOrEmpty(fieldValue))
                        continue;*/
                    if (propInfo.Name == "StackTrace"
                        || string.IsNullOrEmpty(propInfo.Name)
                        || string.IsNullOrEmpty(fieldValue))
                        continue;

                    sb.AppendLine(string.Format("{0}: {1}", fieldName, fieldValue));
                }

                if (ex.Data != null)
                    foreach (System.Collections.DictionaryEntry de in ex.Data)
                        sb.AppendLine(string.Format("Dictionary Entry №{0}: Key: {1} Value: {2}", i, de.Key, de.Value));
            }

            string st;
            if (ex != null)
                if (ex.StackTrace == null)
                    st = new StackTrace(true).ToString();
                else
                    st = ex.StackTrace;
            else
                st = new StackTrace(true).ToString();
            sb.AppendLine("StackTrace:");
            sb.AppendLine(st);

            textBox.Text = sb.ToString();

            Thread.CurrentThread.CurrentCulture = oldCulture;
        }

        private void buttonDontSend_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void buttonSend_Click(object sender, System.EventArgs e)
        {
            Hide();
            Send();
            Close();
        }

        private void ErrorReport_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (File.Exists(_screenShot))
            {
                try
                {
                    File.Delete(_screenShot);
                }
                catch (Exception exception)
                {
                    Debug.Write(exception.Message);
                }
            }

	        string path = Path.Combine(Application.StartupPath, "CrashReport_" + DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + ".txt");
            File.WriteAllText(path, textBox.Text);

            if (this.checkBoxRestart.Checked)
            {
                Application.Restart();
                Process.GetCurrentProcess().Kill();
            }
        }

        private void Send()
        {
            try
            {
                MailAddress fromAddress = new MailAddress(feedbackAddress, Environment.UserName);
                MailAddress toAddress = new MailAddress(feedbackAddress, "Your name");

                SmtpClient smtp = new SmtpClient
                {
                    Host = SMTPServer,
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, feedbackPassword)
                };
                using (MailMessage message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "[" + AssemblyHelper.AssemblyTitle + " " + AssemblyHelper.AssemblyVersion + " Report] " + labelError.Text,
                    Body = textBox.Text + Environment.NewLine + "User message:" + Environment.NewLine + userBox.Text
                })
                {
                    if (File.Exists(_screenShot) && checkBoxIncludeScreenshot.Checked)
                    {
                        message.Attachments.Add(new Attachment(_screenShot));
                    }

                    //You can also use SendAsync method instead of Send so your application begin invoking instead of waiting for send mail to complete. SendAsync(MailMessage, Object) :- Sends the specified e-mail message to an SMTP server for delivery. This method does not block the calling thread and allows the caller to pass an object to the method that is invoked when the operation completes. 
                    smtp.Send(message);

                    MessageBox.Show(LanguageManager.GetString("Crash_ErrorReportSent") + Application.ProductName + "!",
                                Application.ProductName,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + LanguageManager.GetString("Crash_ SendingReportFailed"),
                                Application.ProductName,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private static string getOSInfo()
        {
            //Get Operating system information.
            OperatingSystem os = Environment.OSVersion;
            //Get version information about the os.
            Version vs = os.Version;

            //Variable to hold our return value
            string operatingSystem = String.Empty;

            if (os.Platform == PlatformID.Win32Windows)
            {
                //This is a pre-NT version of Windows
                switch (vs.Minor)
                {
                    case 0:
                        operatingSystem = "95";
                        break;
                    case 10:
                        if (vs.Revision.ToString() == "2222A")
                            operatingSystem = "98SE";
                        else
                            operatingSystem = "98";
                        break;
                    case 90:
                        operatingSystem = "Me";
                        break;
                    default:
                        break;
                }
            }
            else if (os.Platform == PlatformID.Win32NT)
            {
                switch (vs.Major)
                {
                    case 3:
                        operatingSystem = "NT 3.51";
                        break;
                    case 4:
                        operatingSystem = "NT 4.0";
                        break;
                    case 5:
                        if (vs.Minor == 0)
                            operatingSystem = "2000";
                        else
                            operatingSystem = "XP";
                        break;
                    case 6:
                        if (vs.Minor == 0)
                            operatingSystem = "Vista";
                        else if (vs.Minor == 1)
                            operatingSystem = "7";
                        else if (vs.Minor == 2)
                            operatingSystem = "8";
                        break;
                    default:
                        break;
                }
            }
            //Make sure we actually got something in our OS check
            //We don't want to just return " Service Pack 2" or " 32-bit"
            //That information is useless without the OS version.
            if (operatingSystem != "")
            {
                //Got something.  Let's prepend "Windows" and get more info.
                operatingSystem = "Windows " + operatingSystem;
                //See if there's a service pack installed.
                if (os.ServicePack != "")
                {
                    //Append it to the OS name.  i.e. "Windows XP Service Pack 3"
                    operatingSystem += " " + os.ServicePack;
                }
                //Append the OS architecture.  i.e. "Windows XP Service Pack 3 32-bit"
                //operatingSystem += " " + getOSArchitecture().ToString() + "-bit";
            }
            //Return the information we've gathered.
            return operatingSystem;
        }

        private void linkLabelView_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(_screenShot);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(
                    "ErrorCapturingImageMessage","ErrorCapturingImageCaption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //There is an error while capturing image. You can submit report report without screenshot.
                //Произошла ошибка во время захвата изображения. Вы можете представить отчет без скриншота.
                //No image captured
                //Изображение не снято
            }
            catch (Exception)
            {
                MessageBox.Show("NoImageShownMessage","NoImageShownCaption", MessageBoxButtons.OK, MessageBoxIcon.Information);
                //Error occured, no image will be shown.
                //Произошла ошибка, изображение не будет показано.
                //Error opening image
                //Ошибка при открытии изображения
            }
        }

        /*public void ShowOne()
        {
            bool createdNew;
            Mutex mutex = new Mutex(true, "CrashReport", out createdNew);
            if (createdNew)
            {
                //ShowDialog();
                Show();
            }
        }*/
    }
}
