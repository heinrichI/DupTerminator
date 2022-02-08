using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DupTerminator
{
    public class GroupOfDupl
    {
        public string Name;
        public List<ListViewItemSave> Items;

        public GroupOfDupl()
        {
            Items = new List<ListViewItemSave>();
        }

        public GroupOfDupl(string name)
        {
            this.Name = name;
            Items = new List<ListViewItemSave>();
        }

        public void Clear()
        {
            Name = String.Empty;
            Items.Clear();
        }
    }
}
