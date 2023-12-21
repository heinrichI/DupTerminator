using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Helper
{
    public static class HashHelper
    {
        public static string MD5String(this byte[] value)
        {
            byte[] hashBytes = (MD5.Create()).ComputeHash(value);
            StringBuilder builder = new StringBuilder();

            foreach (byte hashByte in hashBytes)
            {
                builder.Append(hashByte.ToString("x2"));
            }

            return builder.ToString();
        }
        public static string CRC32String(this byte[] value)
        {
            Crc32 crc32 = new Crc32();
            string hash = string.Empty;

            foreach (byte b in crc32.ComputeHash(value))
            {
                hash += b.ToString("x2").ToUpper();
            }

            return hash;
        }

        public static string? CreateMD5Checksum(Stream stream)
        {
            using System.Security.Cryptography.MD5 oMD5 = System.Security.Cryptography.MD5.Create();
            StringBuilder sb = new StringBuilder();

            try
            {
                foreach (byte b in oMD5.ComputeHash(stream))
                    sb.Append(b.ToString("x2").ToLower());
            }

            catch (System.UnauthorizedAccessException ex)
            {
                //MessageBox.Show(ex.Message);
                return null;
            }
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                //MessageBox.Show(ex.Message);
                return null;
            }
            catch (System.IO.IOException)
            {
                return null;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Calculate MD5 Checksum for file.
        /// </summary>
        /// <param name="fn">Filename with full path</param>
        /// <returns>Checksum of file.</returns>
        public static string? CreateMD5Checksum(ExtendedFileInfo fileInfo)
        {
            using System.Security.Cryptography.MD5 oMD5 = System.Security.Cryptography.MD5.Create();
            StringBuilder sb = new StringBuilder();

            try
            {
                using (System.IO.FileStream fs = System.IO.File.OpenRead(fileInfo.Path))
                {
                    foreach (byte b in oMD5.ComputeHash(fs))
                        sb.Append(b.ToString("x2").ToLower());
                }
            }

            catch (System.UnauthorizedAccessException ex)
            {
                //MessageBox.Show(ex.Message);
                return null;
            }
            catch (System.IO.FileNotFoundException)
            {
                return null;
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                //MessageBox.Show(ex.Message);
                return null;
            }
            catch (System.IO.IOException)
            {
                return null;
            }

            return sb.ToString();
        }
    }
}
