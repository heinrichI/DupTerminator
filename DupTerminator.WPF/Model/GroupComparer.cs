using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using DupTerminator.WPF.Controls;

namespace DupTerminator.WPF.Model
{
    public class GroupComparer : IComparer<CollectionViewGroup>
    {
        private readonly string _mode;          // "Size" или "Path"

        public GroupComparer(string mode) => _mode = mode;

        public int Compare(CollectionViewGroup x, CollectionViewGroup y)
        {
            var gx = x as CollectionViewGroup;
            var gy = y as CollectionViewGroup;
            if (gx == null || gy == null) return 0;

            // Берём любой элемент группы, а из него – саму DuplicateGroup
            var g1 = gx.Items.Cast<FileRow>().First().Group;
            var g2 = gy.Items.Cast<FileRow>().First().Group;

            return _mode switch
            {
                //"Size" => g1.TotalSize.CompareTo(g2.TotalSize),
                //"Path" => string.Compare(g1.FirstFilePath, g2.FirstFilePath,
                //                        StringComparison.OrdinalIgnoreCase),
                _ => string.Compare(g1.Name, g2.Name,
                                        StringComparison.OrdinalIgnoreCase)
            };
        }
    }
}
