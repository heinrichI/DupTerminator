using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Helper;
using DupTerminator.BusinessLogic.Model;

namespace SevenZipExtractor
{
    internal class ArchiveService : IArchiveService
    {
        public IEnumerable<ArchiveFileInfo> GetInfoFromArchive(Stream stream, ExtendedFileInfo container, bool archiveInArchive = false)
        {
            List<ArchiveFileInfo> infos = new List<ArchiveFileInfo>();
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

                        var fileInfo = Map(entry, container, archiveInArchive, checksumInArchive);
                        infos.Add(fileInfo);

                        entryMemoryStream.Position = 0;
                        if (ArchiveFile.IsArchiveByStream(entryMemoryStream))
                        {
                            infos.AddRange(GetInfoFromArchive(entryMemoryStream, fileInfo, archiveInArchive: true));
                        }
                    }
                }
            }
            return infos;
        }

        private ArchiveFileInfo Map(Entry entry, ExtendedFileInfo container, bool archiveInArchive, string? checksumInArchive = null)
        {
            ArchiveFileInfo efi = new ArchiveFileInfo()
            {
                //InArchive = true,
                ArchiveCRC = entry.CRC,
                ArchiveFileName = container.Name,
                ArchivePath = container.Path,
                ArchiveExtension = container.Extension,
                LastAccessTime = entry.LastAccessTime,
                Name = Path.GetFileName(entry.FileName),
                Extension = Path.GetExtension(entry.FileName),
                Size = entry.Size,
                Path = $"{container.Path}\\{entry.FileName}",
                Container = container,
                ArchiveInArchive = archiveInArchive
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

        public IEnumerable<ArchiveFileInfo> GetInfoFromArchive(string fullName, ExtendedFileInfo container, CancellationToken token, bool archiveInArchive = false)
        {
            List<ArchiveFileInfo> infos = new List<ArchiveFileInfo>();

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

                        var fileInfo = Map(entry, container, archiveInArchive);
                        infos.Add(fileInfo);

                        entryMemoryStream.Position = 0;
                        if (ArchiveFile.IsArchiveByStream(entryMemoryStream))
                        {
                            infos.AddRange(GetInfoFromArchive(entryMemoryStream, fileInfo, archiveInArchive: true));
                        }
                    }
                }
            }
            return infos.ToArray();
        }

        public T CalculateHashInArchive<T>(ArchiveFileInfo fileInfo, Func<Stream, T> calculator)
        {
            Debug.Assert(fileInfo.Container != null);
            if (fileInfo.ArchiveInArchive)
            {
                using (ArchiveFile archiveFile = new ArchiveFile(fileInfo.Container.Container.Path))
                {
                    foreach (var entry in archiveFile.Entries)
                    {
                        //Entry entry = archiveFile.Entries.FirstOrDefault(e => e.FileName == testEntry.Name && e.IsFolder == testEntry.IsFolder);
                        if (entry.IsFolder)
                        {
                            continue;
                        }

                        if (Path.GetFileName(entry.FileName) == fileInfo.Container.Name)
                        {
                            using (MemoryStream entryMemoryStream = new MemoryStream(Convert.ToInt32(entry.Size)))
                            {
                                entry.Extract(entryMemoryStream);

                                entryMemoryStream.Position = 0;
                                using (ArchiveFile archiveFile2 = new ArchiveFile(entryMemoryStream))
                                {
                                    foreach (var entry2 in archiveFile2.Entries)
                                    {
                                        if (entry2.IsFolder)
                                        {
                                            continue;
                                        }

                                        if (Path.GetFileName(entry2.FileName) == fileInfo.Name)
                                        {
                                            using (MemoryStream entryMemoryStream2 = new MemoryStream(Convert.ToInt32(entry2.Size)))
                                            {
                                                entry2.Extract(entryMemoryStream2);

                                                entryMemoryStream2.Position = 0;
                                                return calculator(entryMemoryStream2);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                using (ArchiveFile archiveFile = new ArchiveFile(fileInfo.ArchivePath))
                {
                    foreach (var entry in archiveFile.Entries)
                    {
                        //Entry entry = archiveFile.Entries.FirstOrDefault(e => e.FileName == testEntry.Name && e.IsFolder == testEntry.IsFolder);
                        if (entry.IsFolder)
                        {
                            continue;
                        }

                        if (Path.GetFileName(entry.FileName) == fileInfo.Name)
                        {
                            using (MemoryStream entryMemoryStream = new MemoryStream(Convert.ToInt32(entry.Size)))
                            {
                                entry.Extract(entryMemoryStream);

                                entryMemoryStream.Position = 0;
                                return calculator(entryMemoryStream);
                            }
                        }
                    }
                }
            }
            return default;
        }

        public Stream GetStream(ArchiveFileInfo archiveFileInfo)
        {
            if (archiveFileInfo.ArchiveInArchive)
            {
                using (ArchiveFile archiveFile = new ArchiveFile(archiveFileInfo.Container.Container.Path))
                {
                    foreach (var entry in archiveFile.Entries)
                    {
                        if (entry.IsFolder)
                        {
                            continue;
                        }

                        if (Path.GetFileName(entry.FileName) == archiveFileInfo.Container.Name)
                        {
                            using (MemoryStream entryMemoryStream = new MemoryStream(Convert.ToInt32(entry.Size)))
                            {
                                entry.Extract(entryMemoryStream);

                                entryMemoryStream.Position = 0;
                                using (ArchiveFile archiveFile2 = new ArchiveFile(entryMemoryStream))
                                {
                                    foreach (var entry2 in archiveFile2.Entries)
                                    {
                                        if (entry2.IsFolder)
                                        {
                                            continue;
                                        }

                                        if (Path.GetFileName(entry2.FileName) == archiveFileInfo.Name)
                                        {
                                            MemoryStream entryMemoryStream2 = new MemoryStream(Convert.ToInt32(entry2.Size));
                                            entry2.Extract(entryMemoryStream2);

                                            entryMemoryStream2.Position = 0;
                                            return entryMemoryStream2;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                using (ArchiveFile archiveFile = new ArchiveFile(archiveFileInfo.ArchivePath))
                {
                    foreach (var entry in archiveFile.Entries)
                    {
                        if (entry.IsFolder)
                        {
                            continue;
                        }

                        if (Path.GetFileName(entry.FileName) == archiveFileInfo.Name)
                        {
                            MemoryStream entryMemoryStream = new MemoryStream(Convert.ToInt32(entry.Size));
                            entry.Extract(entryMemoryStream);

                            entryMemoryStream.Position = 0;
                            return entryMemoryStream;
                        }
                    }
                }
            }
            return null;
        }
    }
}
