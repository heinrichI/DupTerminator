using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DupTerminator
{
    //#region "Sorting Functions"
    /// <summary>
    /// Helper class to sort listForCompare of files by size
    /// </summary>
    /*public class sortBySize2 : System.Collections.IComparer
    {
        /// <summary>
        /// Determine the larger of two files
        /// </summary>
        /// <param name="fi1"></param>
        /// <param name="fi2"></param>
        /// <returns></returns>
        int System.Collections.IComparer.Compare(object object1, object object2)
        {
            System.IO.FileInfo fi1 = (System.IO.FileInfo)object1;
            System.IO.FileInfo fi2 = (System.IO.FileInfo)object2;
            return (int)(fi1.Length - fi2.Length);
        }
    }*/

    public class SortBySize : System.Collections.IComparer
    {
        /// <summary>
        /// Determine the larger of two files
        /// </summary>
        /// <param name="fi1"></param>
        /// <param name="fi2"></param>
        /// <returns></returns>
        int System.Collections.IComparer.Compare(object object1, object object2)
        {
            ExtendedFileInfo fi1 = (ExtendedFileInfo)object1;
            ExtendedFileInfo fi2 = (ExtendedFileInfo)object2;
            return (int)(fi1.fileInfo.Length - fi2.fileInfo.Length);
        }
    }

    public class SortByName : System.Collections.IComparer
    {
        int System.Collections.IComparer.Compare(object object1, object object2)
        {
            ExtendedFileInfo efi1 = (ExtendedFileInfo)object1;
            ExtendedFileInfo efi2 = (ExtendedFileInfo)object2;
            return (int)string.Compare(efi1.fileInfo.Name, efi2.fileInfo.Name);
        }
    }


    /// <summary>
    /// Helper class to sort listForCompare of files by checksum.
    /// </summary>
    public class SortByChecksum : System.Collections.IComparer
    {
        public bool FastCheck;
        public uint FastCheckFileSize;
        public uint chunkSize;
        /// <summary>
        /// Determine the larger of two files
        /// </summary>
        /// <param name="fi1"></param>
        /// <param name="fi2"></param>
        /// <returns></returns>
        int System.Collections.IComparer.Compare(object object1, object object2)
        {
            ExtendedFileInfo efi1 = (ExtendedFileInfo)object1;
            ExtendedFileInfo efi2 = (ExtendedFileInfo)object2;

            if (FastCheck && (efi1.fileInfo.Length >= FastCheckFileSize))
                if (fastCheсk(efi1, efi2))
                    return (int)string.Compare(efi1.CheckSum, efi2.CheckSum);
                else
                    return 0;
            else
                return (int)string.Compare(efi1.CheckSum, efi2.CheckSum);
            //return (int)string.Compare(efi1.CheckSum, efi2.CheckSum);
        }

        /// <summary>
        /// Retuern true if first 1024 bytes are equal.
        /// </summary>
        private bool fastCheсk(ExtendedFileInfo efi1, ExtendedFileInfo efi2)
        {
            /*byte[] b1 = new byte[chunkSize];
            byte[] b2 = new byte[chunkSize];*/
            if (efi1.Chunk == null)
            {
                efi1.Chunk = new byte[chunkSize];
                int b1Read;
                using (FileStream file1 = File.OpenRead(efi1.fileInfo.FullName))
                {
                    b1Read = file1.Read(efi1.Chunk, 0, efi1.Chunk.Length);
                }
                if (b1Read < chunkSize)
                    new CrashReport("SortByChecksum.fastCheсk() b1Read < chunkSize!");
            }
            if (efi2.Chunk == null)
            {
                efi2.Chunk = new byte[chunkSize];
                int b2Read;
                using (FileStream file2 = File.OpenRead(efi2.fileInfo.FullName))
                {
                    b2Read = file2.Read(efi2.Chunk, 0, efi2.Chunk.Length);
                }
                if (b2Read < chunkSize)
                    new CrashReport("SortByChecksum.fastCheсk() b2Read < chunkSize!");
            }
            return BlockCompare(efi1.Chunk, efi2.Chunk, 0, chunkSize);

            /*int b1Read;
            int b2Read;
            using (FileStream file1 = File.OpenRead(efi1.fileInfo.FullName), 
                file2 = File.OpenRead(efi2.fileInfo.FullName))
            {
                b1Read = file1.Read(efi1.Chunk, 0, efi1.Chunk.Length);
                b2Read = file2.Read(efi2.Chunk, 0, efi2.Chunk.Length);
            }
            bool result = BlockCompare(efi1.Chunk, efi2.Chunk, 0, Math.Min(b1Read, b2Read));
            return result;*/

        }

        unsafe bool BlockCompare(byte[] buffer1, byte[] buffer2, int offset, uint length)
        {
            if (buffer1 == null || buffer2 == null || buffer1.Length < offset + length || buffer2.Length < offset + length) return false;
            if (buffer1 == buffer2) return true;

            uint blockCount = length / sizeof(uint);
            fixed (byte* pBase1 = buffer1, pBase2 = buffer2)
            {
                int* ptr1 = (int*)(pBase1 + offset);
                int* ptr2 = (int*)(pBase2 + offset);
                for (int i = 0; i < blockCount; i++)
                    if (*ptr1++ != *ptr2++) return false;
            }
            for (int i = 0; i < length % sizeof(int); i++)
                if (buffer1[length - i - 1] != buffer2[length - i - 1]) return false;
            return true;
        }
    }

   

}
