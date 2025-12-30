using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WindowsSpecific;
using DupTerminator.WPF.ViewModel;

namespace DupTerminator.WPF.Converters
{
    public class FileExtensionToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (value is ArchiveFileInfo afi)
            //{
            //    if (afi.ArchiveExtension is not null)
            //        return IconCache.Instance.GetIcon(afi.ArchiveExtension);
            //    else
            //        return IconCache.Instance.GetIcon(afi.Extension);
            //}
            //else
            //if (value is ExtendedFileInfo efi)
            if (value is ExtendedFileInfoViewModel efi)
            {
                return IconCache.Instance.GetIcon(efi.Extension);
            }


            //if (value is ExtendedFileInfo fileInfo)
            //    return IconCache.Instance.GetIcon(fileInfo.Extension);

            return IconCache.Instance.GetIcon(null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
