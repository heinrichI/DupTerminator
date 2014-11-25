using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using SQLite;

namespace DupTerminator
{
    /// <summary>
    /// Extension of system.IO.FileInfo to contain the file checksum as well. 
    /// FileInfo can not be inherited since it is sealed.
    /// </summary>
    public class ExtendedFileInfo
    {
        private string _checkSum;
        private System.IO.FileInfo _fi;
        private DBManager _dbManager;

        /*[AutoIncrement]
        [PrimaryKey]
        public long FileId
        {
            get;
            set;
        }*/

        public byte[] Chunk;

        public ExtendedFileInfo(System.IO.FileInfo fi)
        {
            _fi = fi;
        }

        public ExtendedFileInfo(System.IO.FileInfo fi, DBManager dbManager)
        {
            _fi = fi;
            _dbManager = dbManager;
        }

        /// <summary>
        /// Return check sum of file. If the checksum does not exist, create it.
        /// </summary>
        public string CheckSum
        {
            get
            {
                if (_checkSum == null)
                {
                    if (_dbManager != null)
                    {
                        if (_dbManager.Active)
                        {
                            //System.Diagnostics.Debug.WriteLine("CheckSum _dbManager.Active=" + _dbManager.Active);
                            string md5 = String.Empty;
                            md5 = _dbManager.ReadMD5(_fi.FullName, _fi.LastWriteTime, _fi.Length);
                            if (String.IsNullOrEmpty(md5))
                            {
                                //System.Diagnostics.Debug.WriteLine(String.Format("md5 not found in DB for file {0}, lastwrite: {1}, length: {2}", _fi.FullName, _fi.LastWriteTime, _fi.Length));
                                _checkSum = CreateMD5Checksum(_fi.FullName);
                                //_dbManager.Add(_fi.FullName, _fi.LastWriteTime, _fi.Length, _checkSum);
                                _dbManager.Update(_fi.FullName, _fi.LastWriteTime, _fi.Length, _checkSum);
                            }
                            else
                                _checkSum = md5;
                        }
                        else
                            _checkSum = CreateMD5Checksum(_fi.FullName);
                    }
                    else
                        _checkSum = CreateMD5Checksum(_fi.FullName);

                }
                return _checkSum;
            }
        }

        /// <summary>
        /// Return fileinfo object.
        /// </summary>
        public System.IO.FileInfo fileInfo
        {
            get { return _fi; }
        }

        /// <summary>
        /// Return MD5 Checksum for file.
        /// </summary>
        /// <param name="fn">Filename with full path</param>
        /// <returns>Checksum of file.</returns>
        private string CreateMD5Checksum(string fn)
        {
            System.Security.Cryptography.MD5 oMD5 = System.Security.Cryptography.MD5.Create();
            StringBuilder sb = new StringBuilder();

            try
            {
                using (System.IO.FileStream fs = System.IO.File.OpenRead(fn))
                {
                    foreach (byte b in oMD5.ComputeHash(fs))
                        sb.Append(b.ToString("x2").ToLower());
                }
            }

            catch (System.UnauthorizedAccessException ex)
            {
                //MessageBox.Show(ex.Message);
                return String.Empty;
            }
            catch (System.IO.FileNotFoundException)
            {
                return String.Empty;
            }
            catch (System.IO.DirectoryNotFoundException ex)
            {
                //MessageBox.Show(ex.Message);
                return String.Empty;
            }

            return sb.ToString();
        }
    }
}
