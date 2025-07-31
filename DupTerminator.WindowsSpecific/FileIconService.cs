using System;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DupTerminator.WindowsSpecific
{
    // FileIconService class to cache and retrieve icons
    public static class FileIconService
    {
        private static readonly Dictionary<string, ImageSource> _cache = new Dictionary<string, ImageSource>();

        public static ImageSource GetIconForExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return null;

            if (_cache.TryGetValue(extension, out var img))
                return img;

            string dummyFilename = "dummy" + extension;
            var shinfo = new SHFILEINFO();
            var result = SHGetFileInfo(
                dummyFilename,
                0,
                ref shinfo,
                (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILENAME
            );

            if (result == IntPtr.Zero)
                return null;

            //ImageSource imgSrc = Imaging.CreateBitmapSourceFromHIcon(
            //    shinfo.hIcon,
            //    Int32Rect.Empty,
            //    BitmapSizeOptions.FromEmptyOptions()
            //);
            ImageSource imgSrc = Imaging.CreateBitmapSourceFromHIcon(
                shinfo.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );

            DestroyIcon(shinfo.hIcon);
            _cache[extension] = imgSrc;
            return imgSrc;
        }

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbfileinfo, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_USEFILENAME = 0x000000020;
    }

    // Modify the ExtendedFileInfo to include the Image property
    //    public class ExtendedFileInfo
    //    {
    //        // ... existing properties ...

    //        public ImageSource Image
    //        {
    //            get
    //            {
    //                return FileIconService.GetIconForExtension(this.Extension);
    //            }
    //        }
    //    }
}
