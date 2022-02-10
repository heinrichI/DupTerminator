using DupTerminator.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DupTerminator
{
    class FileUtils
    {
        public static bool IsDirectory(string filename)
        {
            char[] sep = new char[2];
            sep[0] = System.IO.Path.DirectorySeparatorChar;
            sep[1] = System.IO.Path.AltDirectorySeparatorChar;
            if (filename.IndexOfAny(sep) == -1)
            {
                return false;
            }
            return true;
        }

        public static bool MoveToRecycleBin(string file)
        {
            try
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(file,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                return true;
            }
            catch (OperationCanceledException ex)
            {
                return false;
            }
            catch (Exception ex)
            {
                new CrashReport(ex).ShowDialog();
            }
            return false;
        }

        public static bool IsDotNet35Installed
        {
            get
            {
                try
                {
                    return (Convert.ToInt32(Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5").GetValue("Install")) == 1);
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
