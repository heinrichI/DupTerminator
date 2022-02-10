using DupTerminator.BusinessLogic.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.View
{
    internal class MainPresenter
    {
        private IMainView _view;
        private MainViewModel _model;
        private readonly Settings _settings;

        public MainPresenter(IMainView view,
            MainViewModel model,
            Settings settings)
        {
            _view = view;
            _model = model;
            _settings = settings;

            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);

            _view.AddFolderEvent += new EventHandler<AddFolderEventArgs>(OnAddFolder);
            _view.AboutClick += _view_AboutClick; ;
        }

        private void _view_AboutClick(object? sender, EventArgs e)
        {
            FormAbout ab = new FormAbout();
            ab.Font = _settings.Fields.ProgramFont.ToFont();
            ab.ShowDialog();
            try
            {
                ab.Dispose();
            }
            catch (Exception ex)
            {
                new CrashReport(ex, _settings).ShowDialog();
            }
        }


        /*public void ShowView()
        {
            _view.Show();
        }*/

        public void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            //new CrashReport("ThreadException", e.Exception, _settings, lvDuplicates, _undoRedoEngine.ListDuplicates.Items).ShowDialog();
            new CrashReport("ThreadException", e.Exception).ShowDialog();
        }


        /*public ApplicationContext CreateContext()
        {
            return new ApplicationContext(_view);
        }*/

        private void OnAddFolder(object sender, AddFolderEventArgs e)
        {
            switch (e.Directory.Type)
            {
                case TypeFolder.Search:
                    if (CheckFilePath(e.Directory.Path))
                        if (!_model.PathOfSearch.Contains(e.Directory))
                        {
                            _model.PathOfSearch.Add(e.Directory);
                            _view.AddToSearchFolders(e.Directory);
                        }
                    break;
                case TypeFolder.Skip:
                    if (CheckFilePath(e.Directory.Path))
                        if (!_model.PathOfSkip.Contains(e.Directory))
                            _model.PathOfSkip.Add(e.Directory);
                    break;
            }
        }

        private bool CheckFilePath(string targetFilePath)
        {
            string invalid = new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
            {
                if (targetFilePath.IndexOf(c) != -1)
                {
                    MessageBox.Show(targetFilePath + " has invalid symbol in path!");
                    return false;
                }
            }
            return true;
        }


        private bool CheckFileName(string targetFileName)
        {
            //string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string invalid = new string(Path.GetInvalidFileNameChars());
            foreach (char c in invalid)
            {
                if (targetFileName.IndexOf(c) != -1)
                {
                    MessageBox.Show(targetFileName + " has invalid symbol in filename!");
                    return false;
                }
                //Debug.WriteLine(targetFileName + " has invalid symbol in filename!");
                //targetFileName = targetFileName.Replace(c.ToString(), "");
                /*var invalidChars = Path.GetInvalidFileNameChars();
                var invalidCharsRemoved = stringWithInvalidChars
                .Where(x => !invalidChars.Contains(x))
                .ToArray();*/
            }
            return true;
            //return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
        }


        public void Run()
        {
            _view.Show();
            //Application.Run(_view);
        }
    }
}
