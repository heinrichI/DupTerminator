using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Helper;

namespace SevenZipExtractor
{
    internal class ArchiveService : IArchiveService
    {
        public IEnumerable<ExtendedFileInfo> GetInfoFromArchive(Stream stream, ExtendedFileInfo container)
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

                    using (MemoryStream entryMemoryStream = new MemoryStream(Convert.ToInt32(entry.Size)))
                    {
                        entry.Extract(entryMemoryStream);

                        string checksumInArchive = entryMemoryStream.ToArray().MD5String();

                        var fileInfo = Map(entry, container, checksumInArchive);
                        infos.Add(fileInfo);

                        entryMemoryStream.Position = 0;
                        if (ArchiveFile.IsArchiveByStream(entryMemoryStream))
                        {
                            infos.AddRange(GetInfoFromArchive(entryMemoryStream, container));
                        }
                    }
                }
            }
            return infos;
        }

        private ExtendedFileInfo Map(Entry entry, ExtendedFileInfo container, string? checksumInArchive = null)
        {
            ExtendedFileInfo efi = new ExtendedFileInfo()
            {
                InArchive = true,
                ArchiveCRC = entry.CRC,
                ArchivePath = entry.FileName,
                LastAccessTime = entry.LastAccessTime,
                Name = Path.GetFileName(entry.FileName),
                Extension = Path.GetExtension(entry.FileName),
                Size = entry.Size,
                Container = container
            };
            if (checksumInArchive != null)
            {
                efi.CheckSum = checksumInArchive;
            }
            return efi;
        }

        //public IEnumerable<ExtendedFileInfo> GetHashesFromArchive(ExtendedFileInfo container)
        //{
        //    List<ExtendedFileInfo> infos = new List<ExtendedFileInfo>();
        //    using (ArchiveFile archiveFile = new ArchiveFile(container.Path))
        //    {
        //        foreach (var entry in archiveFile.Entries)
        //        {
        //            //Entry entry = archiveFile.Entries.FirstOrDefault(e => e.FileName == testEntry.Name && e.IsFolder == testEntry.IsFolder);
        //            if (entry.IsFolder)
        //            {
        //                continue;
        //            }

        //            using (MemoryStream entryMemoryStream = new MemoryStream(Convert.ToInt32(entry.Size)))
        //            {
        //                entry.Extract(entryMemoryStream);

        //                string checksumInArchive = entryMemoryStream.ToArray().MD5String();

        //                var fileInfo = Map(entry, container, checksumInArchive);
        //                infos.Add(fileInfo);

        //                entryMemoryStream.Position = 0;
        //                if (ArchiveFile.IsArchiveByStream(entryMemoryStream))
        //                {
        //                    infos.AddRange(GetInfoFromArchive(entryMemoryStream, container));
        //                }
        //            }
        //        }
        //    }
        //    return infos;
        //}

        public bool IsArchiveFile(string fullName)
        {
            return ArchiveFile.IsArchive(fullName);
        }

        public IEnumerable<ExtendedFileInfo> GetInfoFromArchive(string fullName, ExtendedFileInfo container, CancellationToken token)
        {
            List<ExtendedFileInfo> infos = new List<ExtendedFileInfo>();

            using (ArchiveFile archiveFile = new ArchiveFile(fullName))
            {
                foreach (var entry in archiveFile.Entries)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (entry.IsFolder)
                    {
                        continue;
                    }

                    using (MemoryStream entryMemoryStream = new MemoryStream(Convert.ToInt32(entry.Size)))
                    {
                        entry.Extract(entryMemoryStream);

                        var fileInfo = Map(entry, container);
                        infos.Add(fileInfo);

                        entryMemoryStream.Position = 0;
                        if (ArchiveFile.IsArchiveByStream(entryMemoryStream))
                        {
                            infos.AddRange(GetInfoFromArchive(entryMemoryStream, container));
                        }
                    }
                }
            }
            return infos;
        }

        public string CalculateHashInArchive(ExtendedFileInfo fileInfo)
        {
            using (ArchiveFile archiveFile = new ArchiveFile(fileInfo.Container.Path))
            {
                foreach (var entry in archiveFile.Entries)
                {
                    //Entry entry = archiveFile.Entries.FirstOrDefault(e => e.FileName == testEntry.Name && e.IsFolder == testEntry.IsFolder);
                    if (entry.IsFolder)
                    {
                        continue;
                    }

                    if (entry.FileName == fileInfo.ArchivePath)
                    {
                        using (MemoryStream entryMemoryStream = new MemoryStream(Convert.ToInt32(entry.Size)))
                        {
                            entry.Extract(entryMemoryStream);

                            entryMemoryStream.Position = 0;
                            return HashHelper.CreateMD5Checksum(entryMemoryStream);
                        }
                    }
                }
            }
            return null;
        }
    }
}
