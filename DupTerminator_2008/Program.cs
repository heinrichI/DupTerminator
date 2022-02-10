using System;
using System.Windows.Forms;
using DupTerminator.Views;
using DupTerminator.Presenter;

namespace DupTerminator
{
    static class Program
    {
        private static bool GetParameter(string[] args, string name, ref string value)
        {
            for (int i = 0; i < (args.Length); i++)
            {
                if (string.Compare(args[i], name) == 0)
                {
                    value = args[i];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string str = null;
            if (GetParameter(args, "-version", ref str))
            {
                VersionManager.VersionInfo vers = new VersionManager.VersionInfo(true);
                vers.SaveXml(System.IO.Path.Combine(Application.StartupPath, "version.xml"));
                System.Environment.Exit(1);
                //Application.Exit();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //FormMain formMain = new FormMain();
            MainPresenter presenter = new MainPresenter();
            // Add event handler for thread exceptions
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            //Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            //Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(formMain.Application_ThreadException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(presenter.Application_ThreadException);

            //Application.Run(presenter.CreateContext());
            presenter.Run();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            new CrashReport("UnhandledException", (Exception)e.ExceptionObject).ShowDialog();
        }

        /*static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            new CrashReport("ThreadException", e.Exception, null).ShowDialog();
        }*/
    }
}