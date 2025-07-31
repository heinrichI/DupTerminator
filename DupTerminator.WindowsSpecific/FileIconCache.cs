using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace DupTerminator.WindowsSpecific
{
    public static class FileIconCache
    {
        private static readonly Dictionary<string, BitmapSource> _iconCache = new Dictionary<string, BitmapSource>();

        //public static BitmapSource GetIconForExtension(string extension)
        //{
        //    if (_iconCache.TryGetValue(extension, out BitmapSource cachedIcon))
        //        return cachedIcon;

        //    BitmapSource icon = GetSystemIconForExtension(extension);
        //    _iconCache[extension] = icon;
        //    return icon;
        //}

        //private static BitmapSource GetSystemIconForExtension(string extension)
        //{
        //    try
        //    {
        //        var dummyPath = Path.GetTempPath() + "dummy" + extension;
        //        if (!File.Exists(dummyPath))
        //            File.Create(dummyPath).Dispose();

        //        using var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(dummyPath);
        //        var bitmap = sysicon.ToBitmap();
        //        var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
        //            bitmap.GetHbitmap(),
        //            IntPtr.Zero,
        //            System.Windows.Int32Rect.Empty,
        //            BitmapSizeOptions.FromEmptyOptions());

        //        if (File.Exists(dummyPath))
        //            File.Delete(dummyPath);

        //        return bitmapSource;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
    }
}
