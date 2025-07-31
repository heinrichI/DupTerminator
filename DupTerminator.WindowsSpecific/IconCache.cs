using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DupTerminator.WindowsSpecific
{
    public class IconCache
    {
        private static readonly Lazy<IconCache> _instance = new(() => new IconCache());
        private readonly ConcurrentDictionary<string, ImageSource> _cache;
        private readonly object _lock = new();

        private IconCache()
        {
            _cache = new ConcurrentDictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        }

        public static IconCache Instance => _instance.Value;

        public ImageSource GetIcon(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return GetDefaultIcon();

            if (_cache.TryGetValue(extension, out var cachedIcon))
                return cachedIcon;

            lock (_lock)
            {
                // Double-check lock pattern
                if (_cache.TryGetValue(extension, out cachedIcon))
                    return cachedIcon;

                ImageSource icon = CreateIcon(extension);
                _cache[extension] = icon;
                return icon;
            }
        }

        private ImageSource CreateIcon(string extension)
        {
            Win32.SHFILEINFO shinfo = new();
            string fileName = "dummy" + (extension.StartsWith('.') ? extension : "." + extension);
            uint flags = Win32.SHGFI_ICON | Win32.SHGFI_SMALLICON | Win32.SHGFI_USEFILEATTRIBUTES;

            IntPtr hIcon = Win32.SHGetFileInfo(
                fileName,
                Win32.FILE_ATTRIBUTE_NORMAL,
                ref shinfo,
                (uint)Marshal.SizeOf(shinfo),
                flags);

            if (shinfo.hIcon == IntPtr.Zero)
                return GetDefaultIcon();

            try
            {
                //using var icon = System.Drawing.Icon.FromHandle(shinfo.hIcon);

                BitmapSource bmp = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                bmp.Freeze(); // Для безопасного использования в нескольких потоках
                return bmp;
            }
            finally
            {
                Win32.DestroyIcon(shinfo.hIcon);
            }
        }

        private ImageSource GetDefaultIcon()
        {
            // Вернуть иконку по умолчанию
            if (_cache.TryGetValue("DEFAULT", out var icon))
                return icon;

            // Создание простой заглушки
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.LightGray, null, new Rect(0, 0, 16, 16));
            }

            var bmp = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(visual);
            bmp.Freeze();

            _cache["DEFAULT"] = bmp;
            return bmp;
        }
    }
}
