using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using DupTerminator.BusinessLogic;
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

                    using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
                    {
                        entry.Extract(entryStream);

                        //string checksumInArchive = entryStream.ToArray().MD5String();
                        string checksumInArchive = string.Empty;

                        var fileInfo = Map(entry, container, archiveInArchive, checksumInArchive);
                        infos.Add(fileInfo);

                        entryStream.Position = 0;
                        if (ArchiveFile.IsArchiveByStream(entryStream))
                        {
                            infos.AddRange(GetInfoFromArchive(entryStream, fileInfo, archiveInArchive: true));
                        }
                    }
                }
            }
            return infos;
        }

        private static ArchiveFileInfo Map(Entry entry, ExtendedFileInfo container, bool archiveInArchive, string? checksumInArchive = null)
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

                    using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
                    {
                        entry.Extract(entryStream);

                        var fileInfo = Map(entry, container, archiveInArchive);
                        infos.Add(fileInfo);

                        entryStream.Position = 0;
                        if (ArchiveFile.IsArchiveByStream(entryStream))
                        {
                            infos.AddRange(GetInfoFromArchive(entryStream, fileInfo, archiveInArchive: true));
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
                            using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
                            {
                                entry.Extract(entryStream);

                                entryStream.Position = 0;
                                using (ArchiveFile archiveFile2 = new ArchiveFile(entryStream))
                                {
                                    foreach (var entry2 in archiveFile2.Entries)
                                    {
                                        if (entry2.IsFolder)
                                        {
                                            continue;
                                        }

                                        if (Path.GetFileName(entry2.FileName) == fileInfo.Name)
                                        {
                                            using (var entryStream2 = new ChunkedMemoryStream(Convert.ToInt32(entry2.Size)))
                                            {
                                                entry2.Extract(entryStream2);

                                                entryStream2.Position = 0;
                                                return calculator(entryStream2);
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
                            using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
                            {
                                entry.Extract(entryStream);

                                entryStream.Position = 0;
                                return calculator(entryStream);
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
                            using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
                            {
                                entry.Extract(entryStream);

                                entryStream.Position = 0;
                                using (ArchiveFile archiveFile2 = new ArchiveFile(entryStream))
                                {
                                    foreach (var entry2 in archiveFile2.Entries)
                                    {
                                        if (entry2.IsFolder)
                                        {
                                            continue;
                                        }

                                        if (Path.GetFileName(entry2.FileName) == archiveFileInfo.Name)
                                        {
                                            var entryStream2 = new ChunkedMemoryStream(Convert.ToInt32(entry2.Size));
                                            entry2.Extract(entryStream2);

                                            entryStream2.Position = 0;
                                            return entryStream2;
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
                            var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size));
                            entry.Extract(entryStream);

                            entryStream.Position = 0;
                            return entryStream;
                        }
                    }
                }
            }
            return null;
        }

        //public IList<(ArchiveFileInfo, Stream)> GetStreams(ExtendedFileInfo fileInfo, Func<string, bool> isSupportedExtension, CancellationToken cancelToken)
        //{
        //    List<(ArchiveFileInfo, Stream) > streams = new();
        //    using (ArchiveFile archiveFile = new ArchiveFile(fileInfo.Path))
        //    {
        //        foreach (var entry in archiveFile.Entries)
        //        {
        //            if (entry.IsFolder)
        //            {
        //                continue;
        //            }

        //            MemoryStream entryMemoryStream = new MemoryStream(Convert.ToInt32(entry.Size));
        //            entry.Extract(entryMemoryStream);
        //            entryMemoryStream.Position = 0;
        //            if (isSupportedExtension(Path.GetExtension(entry.FileName)))
        //            {
        //                var archInfo = Map(entry, fileInfo, false);
        //                streams.Add((archInfo, entryMemoryStream));
        //            }
        //            else
        //            {
        //                if (ArchiveFile.IsArchiveByStream(entryMemoryStream))
        //                {
        //                    using (ArchiveFile archiveFile2 = new ArchiveFile(entryMemoryStream))
        //                    {
        //                        var container = Map(entry, fileInfo, true);
        //                        foreach (var entry2 in archiveFile2.Entries)
        //                        {
        //                            if (entry2.IsFolder)
        //                            {
        //                                continue;
        //                            }

        //                            MemoryStream entryMemoryStream2 = new MemoryStream(Convert.ToInt32(entry2.Size));
        //                            entry2.Extract(entryMemoryStream2);
        //                            entryMemoryStream2.Position = 0;

        //                            if (isSupportedExtension(Path.GetExtension(entry2.FileName)))
        //                            {
        //                                var archInfo = Map(entry2, container, true);
        //                                streams.Add((archInfo, entryMemoryStream2));
        //                            }
        //                            else
        //                            {
        //                                if (ArchiveFile.IsArchiveByStream(entryMemoryStream2))
        //                                {
        //                                    using (ArchiveFile archiveFile3 = new ArchiveFile(entryMemoryStream2))
        //                                    {
        //                                        foreach (var entry3 in archiveFile3.Entries)
        //                                        {
        //                                            if (entry3.IsFolder)
        //                                            {
        //                                                continue;
        //                                            }

        //                                            if (isSupportedExtension(Path.GetExtension(entry3.FileName)))
        //                                            {
        //                                                MemoryStream entryMemoryStream3 = new MemoryStream(Convert.ToInt32(entry3.Size));
        //                                                entry3.Extract(entryMemoryStream3);
        //                                                entryMemoryStream3.Position = 0;
        //                                                var archInfo = Map(entry3, fileInfo, true);
        //                                                streams.Add((archInfo, entryMemoryStream3));
        //                                            }
        //                                        }
        //                                    }
        //                                }
        //                                entryMemoryStream2.Dispose();
        //                            }
        //                        }
        //                    }
        //                }
        //                entryMemoryStream.Dispose();
        //            }
        //        }
        //    }
        //    return streams;
        //}

        public IList<(ArchiveFileInfo, Stream)> GetStreams(ExtendedFileInfo fileInfo, Func<string, bool> isSupportedExtension, CancellationToken cancelToken)
        {
            var streams = new List<(ArchiveFileInfo, Stream)>();
            using var archiveFile = new ArchiveFile(fileInfo.Path);

            CollectImageStreams(archiveFile, fileInfo, isNested: false, streams, isSupportedExtension, cancelToken);
            return streams;
        }

        private static void CollectImageStreams(ArchiveFile archive, ExtendedFileInfo container, bool isNested,
            List<(ArchiveFileInfo, Stream)> streams, Func<string, bool> isSupportedExtension, CancellationToken cancelToken)
        {
            foreach (var entry in archive.Entries)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    System.Diagnostics.Debug.WriteLine("CollectImageStreams was canceled.");
                    break;
                }

                if (entry.IsFolder)
                {
                    continue;
                }

                var entryStream = new ChunkedMemoryStream((int)entry.Size);
                entry.Extract(entryStream);
                entryStream.Position = 0;

                if (isSupportedExtension(Path.GetExtension(entry.FileName)))
                {
                    var archInfo = Map(entry, container, isNested);
                    streams.Add((archInfo, entryStream));  // Transfer ownership
                    //entryStream = null;  // Skip dispose
                }
                else
                {
                    if (ArchiveFile.IsArchiveByStream(entryStream))
                    {
                        // Critical: Reset position after IsArchiveByStream (it may advance it)
                        entryStream.Position = 0;
                        using var nestedArchive = new ArchiveFile(entryStream);
                        var nestedContainer = Map(entry, container, true);
                        CollectImageStreams(nestedArchive, nestedContainer, true, streams, isSupportedExtension, cancelToken);
                        // Intermediate stream auto-disposes here → buffer returned
                    }
                    entryStream.Dispose();
                }
            }
        }
    }
}
