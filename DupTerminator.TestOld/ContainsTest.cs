using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Diagnostics;

namespace DupTerminator.Test
{
    /// <summary>
    /// Сводное описание для UnitTest2
    /// </summary>
    [TestClass]
    public class ContainsTest
    {
        [TestMethod]
        public void TestRemoveFiles()
        {
            List<FileInfo> files = new List<FileInfo>();
            files.Add(new FileInfo("D:\\Test\\image1.jpg"));
            files.Add(new FileInfo("D:\\Test\\image2.jpg"));
            files.Add(new FileInfo("D:\\Test\\image3.jpg"));
            files.Add(new FileInfo("D:\\Test\\index.htm"));

            List<FileInfo> excludeFiles = new List<FileInfo>();
            excludeFiles.Add(new FileInfo("D:\\Test\\index.htm"));

            Debug.WriteLine(files[2] + " содержится в исключенных " + excludeFiles.Contains(files[2]));
            Debug.WriteLine(files[3] + " содержится в исключенных " + excludeFiles.Contains(files[3]));

            int deleted = files.RemoveAll(delegate(FileInfo file)
            {
                //Debug.WriteLine(file + " содержится в исключенных " + excludeFiles.Contains(file));
                Debug.WriteLine(file + " содержится в исключенных " + excludeFiles.Any(f => f.FullName == file.FullName));
                return (excludeFiles.Any(f => f.FullName == file.FullName));
            });



            Assert.AreEqual(3, files.Count);
            Assert.AreEqual(1, deleted);
            //Assert.IsTrue(excludeFiles.Contains(files[3]));
        }
    }
}
