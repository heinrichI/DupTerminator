using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SevenZipExtractor
{
    internal class ArchiveService : IArchiveService
    {
        public IEnumerable<ExtendedFileInfo> GetInfoFromArchive(Stream stream)
        {
            List<ExtendedFileInfo> infos = new List<ExtendedFileInfo>();
            using (ArchiveFile archiveFile = new ArchiveFile(stream))
            {
                foreach (var entry in archiveFile.Entries)
                {
                    if (entry.IsFolder)
                    {
                        continue;
                    }

                    using (MemoryStream entryMemoryStream = new MemoryStream())
                    {
                        entry.Extract(entryMemoryStream);

                        string checksumInArchive = entryMemoryStream.ToArray().MD5String();

                        ExtendedFileInfo fileInfo = new ExtendedFileInfo()
                        {
                            InArchive = true,
                            ArchiveCRC = entry.CRC,
                            LastAccessTime = entry.LastAccessTime,
                            Name = entry.FileName,
                            Size = entry.Size,
                            CheckSum = checksumInArchive
                        };
                        infos.Add(fileInfo);

                        if (ArchiveFile.IsArchiveByStream(entryMemoryStream))
                        {
                            infos.AddRange(GetInfoFromArchive(entryMemoryStream));
                        }
                    }
                }
            }
            return infos;
        }

        public IEnumerable<ExtendedFileInfo> GetInfoFromArchive(string path)
        {
            List<ExtendedFileInfo> infos = new List<ExtendedFileInfo>();
            using (ArchiveFile archiveFile = new ArchiveFile(path))
            {
                foreach (var entry in archiveFile.Entries)
                {
                    //Entry entry = archiveFile.Entries.FirstOrDefault(e => e.FileName == testEntry.Name && e.IsFolder == testEntry.IsFolder);
                    if (entry.IsFolder)
                    {
                        continue;
                    }

                    using (MemoryStream entryMemoryStream = new MemoryStream())
                    {
                        entry.Extract(entryMemoryStream);

                        string checksumInArchive = entryMemoryStream.ToArray().MD5String();

                        ExtendedFileInfo fileInfo = new ExtendedFileInfo()
                        {
                            InArchive = true,
                            ArchiveCRC = entry.CRC,
                            LastAccessTime = entry.LastAccessTime,
                            Name = entry.FileName,
                            Size = entry.Size,
                            CheckSum = checksumInArchive
                        };
                        infos.Add(fileInfo);

                        if (ArchiveFile.IsArchiveByStream(entryMemoryStream))
                        {
                            infos.AddRange(GetInfoFromArchive(entryMemoryStream));
                        }
                    }
                }
            }
            return infos;
        }

        public bool IsArchiveFile(string fullName)
        {
            return ArchiveFile.IsArchive(fullName);
        }
    }
}
