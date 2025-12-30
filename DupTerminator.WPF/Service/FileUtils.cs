using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.WPF.Service
{
    internal class FileUtils
    {
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
            return false;
        }
    }
}
