using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.Serialization;

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

    //для Load_Save ListViewDuplicate
    [Serializable]
    //[XmlInclude(typeof(ListViewItemSave))]
    public class ListViewSave
    {
        public List<String> Groups = new List<String>();
        [XmlArray("Items")]
        public List<ListViewItemSave> Items = new List<ListViewItemSave>();

        public ListViewSave()
        { }

        public ListViewSave(int col)
        { }
    }

    [Serializable]
    public class ListViewItemSave //: ISerializable
    {
        public bool Checked;
        public string Group;
        //public int Index;
        public string Name;
        public string Text;
        public ListViewItemSaveSubItem[] SubItems;

        public ListViewItemSave()
        { }
        //public List<ListViewItemSaveSubItemCollection> SubItems = new List<ListViewItemSaveSubItemCollection>();

        public ListViewItemSave(int col)
        {
            SubItems = new ListViewItemSaveSubItem[(col - 1)];
        }
    }

    [Serializable]
     public class ListViewItemSaveSubItem// : ISerializable
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

}
