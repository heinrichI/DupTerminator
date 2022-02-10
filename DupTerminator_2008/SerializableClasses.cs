using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Windows.Forms;

#if NUNIT
using NUnit.Framework;
using System.IO;
#endif

namespace DupTerminator
{
    [Serializable]
    public class ListViewItemSearchDir
    {
        public bool IsChecked;
        public bool IsSubDir;
        public string Path;

        public ListViewItemSearchDir()
        { }
    }
    
    /*class CheckedItems : IEnumerable<ListViewItemSave>, IEnumerator
    {
        int[] ints = { 12, 13, 1, 4 };
        int indexOfGroupWithAllChecked = -1;

        // Реализуем интерейс IEnumerable
        public IEnumerator GetEnumerator()
        {
            return this;
        }

        // Реализуем интерфейс IEnumerator
        public bool MoveNext()
        {
            if (indexOfGroupWithAllChecked == ints.Length - 1)
            {
                Reset();
                return false;
            }

            indexOfGroupWithAllChecked++;
            return true;
        }

        public void Reset()
        {
            indexOfGroupWithAllChecked = -1;
        }

        public object Current
        {
            get
            {
                return ints[indexOfGroupWithAllChecked];
            }
        }
    }*/

    [Serializable]
    public class ListViewItemSave //: ISerializable
    {
        public bool Checked;
        public string Group;
        public SerializableColor Color;
        //public int Index;
        public string Name;
        public string Text;
        public ListViewItemSaveSubItem[] SubItems;

        public ListViewItemSave()
        { }
        //public List<ListViewItemSaveSubItemCollection> SubItems = new List<ListViewItemSaveSubItemCollection>();

        public ListViewItemSave(int colSubItem)
        {
            SubItems = new ListViewItemSaveSubItem[(colSubItem)];
        }

        public string FileName
        {
            get
            {
                return SubItems[0].Text;
            }
            set
            {
                SubItems[0].Text = value;
            }
        }

        public string Directory
        {
            get
            {
                return SubItems[1].Text;
            }
            set
            {
                SubItems[1].Text = value;
            }
        }

        public DateTime DateTime
        {
            get
            {
                return Convert.ToDateTime(SubItems[4].Text);
            }
        }

        public string CheckSum
        {
            get
            {
                return SubItems[5].Text;
            }
        }
    }

    /// <summary>
    /// 0 - Name
    /// 1 - Directory
    /// 2 - Size
    /// 3 - FileType
    /// 4 - LastAccessed
    /// 5 - MD5Checksum
    /// </summary>
    [Serializable]
    public class ListViewItemSaveSubItem
    {
        public string Name;
        public string Text;

        public ListViewItemSaveSubItem()
        { }
    }

    //для Load_App_Settings
    [Serializable]
    public class CheckedItemList : ISerializable
    {
        bool _checked;
        string _Directory;

        public CheckedItemList(string dir, bool chk)
        {
            _checked = chk;
            _Directory = dir;
        }

        //Load
        public CheckedItemList(SerializationInfo info, StreamingContext context)
        {
            _checked = (bool)info.GetValue("Checked", typeof(bool));
            _Directory = (string)info.GetValue("Directory", typeof(string));
        }

        //Save
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Checked", _checked);
            info.AddValue("Directory", _Directory);
        }

        public bool IsChecked
        {
            get { return _checked; }
            set { _checked = (bool)value; }
        }

        public string Directory
        {
            get { return _Directory; }
            set { _Directory = (string)value; }
        }
    }


#if NUNIT
    [TestFixture]
    public class TestListViewSave
    {
        private ListViewSave lvs;
        private string testDir = @"e:\Sample C#\DupTerminator1.4\TestDir";
        /// <summary>
        /// Initialize testing, copy the test executable to correct folder.
        /// </summary>
        /*[SetUp]
        public void Setup()
        {
            lvs = new ListViewSave();
            /*System.IO.DirectoryInfo di = new DirectoryInfo("..\\..\\..\\TestDir");
            foreach (System.IO.FileInfo file in di.GetFiles())
                lvs.Add(new ExtendedFileInfo(file));
            //lvs.Add(new ExtendedFileInfo(new FileInfo("Chuck.txt")));
            System.IO.DirectoryInfo di2 = new System.IO.DirectoryInfo("..\\..\\..\\TestDir\\Dir");
            foreach (System.IO.FileInfo file in di2.GetFiles())
                lvs.Add(new ExtendedFileInfo(file));
        }*/

        [SetUp]
        [Test]
        public void Create()
        {
            lvs = new ListViewSave();
            System.IO.FileInfo file;
            file = new System.IO.FileInfo(testDir + "\\Chuck2.txt");
            lvs.Add(new ExtendedFileInfo(file));
            file = new System.IO.FileInfo(testDir + "\\Chuck3.txt");
            lvs.Add(new ExtendedFileInfo(file));
            file = new System.IO.FileInfo(testDir + "\\Chuck.txt");
            lvs.Add(new ExtendedFileInfo(file));
            file = new System.IO.FileInfo(testDir + "\\Dir\\Chuck.txt");
            lvs.Add(new ExtendedFileInfo(file));
            file = new System.IO.FileInfo(testDir + "\\Dir\\Chuck2.txt");
            lvs.Add(new ExtendedFileInfo(file));
            file = new System.IO.FileInfo(testDir + "\\Dir\\Chuck3.txt");
            lvs.Add(new ExtendedFileInfo(file));

            Assert.AreEqual(lvs.Items[0].Text, "Chuck2.txt");
            Assert.AreEqual(lvs.Items[0].SubItems[1].Text, testDir);
            Assert.AreEqual(lvs.Items[1].Text, "Chuck3.txt");
            Assert.AreEqual(lvs.Items[1].SubItems[1].Text, testDir);
            Assert.AreEqual(lvs.Items[2].Text, "Chuck.txt");
            Assert.AreEqual(lvs.Items[2].SubItems[1].Text, testDir);

            Assert.AreEqual(lvs.Items[3].Text, "Chuck.txt");
            Assert.AreEqual(lvs.Items[3].SubItems[1].Text, testDir + "\\Dir");
            Assert.AreEqual(lvs.Items[4].Text, "Chuck2.txt");
            Assert.AreEqual(lvs.Items[4].SubItems[1].Text, testDir + "\\Dir");
            Assert.AreEqual(lvs.Items[5].Text, "Chuck3.txt");
            Assert.AreEqual(lvs.Items[5].SubItems[1].Text, testDir + "\\Dir");
        }

        [Test]
        public void SortPathAscending()
        {
            ListViewSaveGroupSorter lvwGroupSorter = new ListViewSaveGroupSorter();
            lvwGroupSorter.Order = System.Windows.Forms.SortOrder.Ascending;
            // .SubItems[0 -путь
            // .SubItems[1 -размер
            lvwGroupSorter.SortColumn = 1;
            lvs.Sort(lvwGroupSorter);

            Assert.AreEqual(lvs.Items[0].Group, "7ce1ad068e422a6d1cd28b4543249483");
            Assert.AreEqual(lvs.Items[0].SubItems[1].Text, testDir);
            Assert.AreEqual(lvs.Items[1].SubItems[1].Text, testDir);
            Assert.AreEqual(lvs.Items[2].SubItems[1].Text, testDir);
            Assert.AreEqual(lvs.Items[3].SubItems[1].Text, testDir + "\\Dir");
            Assert.AreEqual(lvs.Items[4].SubItems[1].Text, testDir + "\\Dir");
            Assert.AreEqual(lvs.Items[5].SubItems[1].Text, testDir + "\\Dir");
            Assert.AreEqual(lvs.Items.Count, 6);
        }

        [Test]
        public void SortPathDescending()
        {
            ListViewSaveGroupSorter lvwGroupSorter = new ListViewSaveGroupSorter();
            lvwGroupSorter.Order = System.Windows.Forms.SortOrder.Descending;
            lvwGroupSorter.SortColumn = 1;
            lvs.Sort(lvwGroupSorter);

            Assert.AreEqual(lvs.Items[0].Group, "3163cba8042a222de418af9dc02038a0");
            Assert.AreEqual(lvs.Items[2].Group, "501f31368fee64d8ce99bf467b2f4269");
            Assert.AreEqual(lvs.Items[0].SubItems[1].Text, testDir + "\\Dir");
            Assert.AreEqual(lvs.Items[1].SubItems[1].Text, testDir + "\\Dir");
            Assert.AreEqual(lvs.Items[2].SubItems[1].Text, testDir + "\\Dir");
            Assert.AreEqual(lvs.Items[3].SubItems[1].Text, testDir);
            Assert.AreEqual(lvs.Items[4].SubItems[1].Text, testDir);
            Assert.AreEqual(lvs.Items[5].SubItems[1].Text, testDir);
            Assert.AreEqual(lvs.Items.Count, 6);
        }

        /*[Test]
        public void SortNameAscending()
        {
            ListViewSaveGroupSorter lvwGroupSorter = new ListViewSaveGroupSorter();
            lvwGroupSorter.Order = System.Windows.Forms.SortOrder.Ascending;
            lvwGroupSorter.SortColumn = 0;
            lvs.Sort(lvwGroupSorter);
        }*/
    }
#endif

}
