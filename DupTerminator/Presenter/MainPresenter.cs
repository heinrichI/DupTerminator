using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DupTerminator.Views;
using DupTerminator.Models;
using System.IO;
using System.Windows.Forms;
using DupTerminator.ObjectModel;

namespace DupTerminator.Presenter
{
    public class MainPresenter
    {
        private IMainView _view;
        private MainModel _model;

        public MainPresenter()
        {
            _view = new FormMain();
            _model = new MainModel();

            _view.AddFolderEvent += new EventHandler<AddFolderEventArgs>(viewAddFolder);
        }

        public MainPresenter(IMainView view, MainModel model)
        {
            _view = view;
            _model = model;

            _view.AddFolderEvent += new EventHandler<AddFolderEventArgs>(viewAddFolder);
        }

        /*public void ShowView()
        {
            _view.Show();
        }*/

        public void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            //new CrashReport("ThreadException", e.Exception, _settings, lvDuplicates, _undoRedoEngine.ListDuplicates.Items).ShowDialog();
            new CrashReport("ThreadException", e.Exception, _model).ShowDialog();
        }


        /*public ApplicationContext CreateContext()
        {
            return new ApplicationContext(_view);
        }*/

        private void viewAddFolder(object sender, AddFolderEventArgs e)
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
