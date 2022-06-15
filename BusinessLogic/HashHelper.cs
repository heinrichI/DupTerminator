using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace DupTerminator.BusinessLogic
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
    }
}
