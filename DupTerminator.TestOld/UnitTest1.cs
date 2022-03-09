using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DupTerminator.Presenter;
using DupTerminator.Models;
using DupTerminator.Views;
using DupTerminator.ObjectModel;

namespace DupTerminator.Test
{
    /// <summary>
    /// Сводное описание для UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestInvalidFileName()
        {
            // Организация (настройка сценария)
            MainModel model = new MainModel();
            TestMainView view = new TestMainView();
            MainPresenter presenter = new MainPresenter(view, model);

            // Действие (попытка выполнения операции)
            view.RiseAddFolderEvent(new AddFolderEventArgs(new DuplicateDirectory("D:\\TestPa", true, TypeFolder.Search, true)));
            view.RiseAddFolderEvent(new AddFolderEventArgs(new DuplicateDirectory("D:\\Test<Pa\\yy|yy", true, TypeFolder.Search, true)));

            // Утверждение (проверка результатов)
            Assert.AreEqual(1, model.PathOfSearch.Count);
            Assert.AreEqual("D:\\TestPa", model.PathOfSearch[0].Path);
        }

        [TestMethod]
        public void TestAddDuplicateSearchPath()
        {
            // Организация (настройка сценария)
            MainModel model = new MainModel();
            TestMainView view = new TestMainView();
            MainPresenter presenter = new MainPresenter(view, model);

            // Действие (попытка выполнения операции)
            view.RiseAddFolderEvent(new AddFolderEventArgs(new DuplicateDirectory("D:\\TestPath", true, TypeFolder.Search, true)));
            view.RiseAddFolderEvent(new AddFolderEventArgs(new DuplicateDirectory("D:\\TestPath", true, TypeFolder.Search, true)));
            view.RiseAddFolderEvent(new AddFolderEventArgs(new DuplicateDirectory("D:\\TestPath", false, TypeFolder.Search, true)));
            view.RiseAddFolderEvent(new AddFolderEventArgs(new DuplicateDirectory("D:\\TestPath", true, TypeFolder.Skip, true)));

            // Утверждение (проверка результатов)
            Assert.AreEqual(1, model.PathOfSearch.Count);
            Assert.AreEqual("D:\\TestPath", model.PathOfSearch[0].Path);
            Assert.AreEqual(1, model.PathOfSkip.Count);
            Assert.AreEqual("D:\\TestPath", model.PathOfSkip[0].Path);
        }
    }
}
