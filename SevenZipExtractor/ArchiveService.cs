using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Helper;
using DupTerminator.BusinessLogic.Model;
using Microsoft.Extensions.Logging;

namespace SevenZipExtractor
{
    internal class ArchiveService : IArchiveService
    {
        private readonly ILogger<ArchiveService> _logger;

        public ArchiveService(ILogger<ArchiveService> logger)
        {
            _logger = logger;
        }

        public IEnumerable<ArchiveFileInfo> GetInfoFromArchive(Stream stream, ExtendedFileInfo container, bool archiveInArchive = false)
        {
            List<ArchiveFileInfo> containerInfos = new List<ArchiveFileInfo>();
            List<ArchiveFileInfo> archiveInArchiveInfos = new List<ArchiveFileInfo>();

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

                        var fileInfo = Map(entry, container, archiveFile.Entries.Count(e => !e.IsFolder), archiveInArchive);
                        containerInfos.Add(fileInfo);

                        entryStream.Position = 0;
                        if (ArchiveFile.IsArchiveByStream(entryStream))
                        {
                            archiveInArchiveInfos.AddRange(GetInfoFromArchive(entryStream, fileInfo, archiveInArchive: true));
                        }
                    }
                }
                var freeze = containerInfos.Select(c => new ArchiveSimpleFileInfo(c)).ToArray();
                foreach (var fileInfo in containerInfos)
                {
                    fileInfo.ContainerFiles = freeze;
                }
            }
            containerInfos.AddRange(archiveInArchiveInfos);
            return containerInfos.ToArray();
        }

        private static ArchiveFileInfo Map(Entry entry, ExtendedFileInfo archive, int containerFilesCount, bool archiveInArchive)
        {
            ArchiveFileInfo efi = new ArchiveFileInfo()
            {
                //InArchive = true,
                ArchiveCRC = entry.CRC,
                ArchiveFileName = archive.Name,
                ArchivePath = archive.Path,
                ArchiveExtension = archive.Extension,
                //LastAccessTime = entry.LastAccessTime,
                LastWriteTime = entry.LastWriteTime,
                Name = Path.GetFileName(entry.FileName),
                Extension = Path.GetExtension(entry.FileName),
                Size = entry.Size,
                Path = $"{archive.Path}\\{entry.FileName}",
                Container = archive,
                ContainerFilesCount = containerFilesCount,
                ArchiveInArchive = archiveInArchive
            };
            Debug.Assert(!string.IsNullOrEmpty(efi.ArchiveExtension));
            Debug.Assert(!string.IsNullOrEmpty(efi.Path));
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

        public ArchiveFileInfo[] GetInfoFromArchive(ExtendedFileInfo archive, CancellationToken token, bool archiveInArchive = false)
        {
            List<ArchiveFileInfo> containerInfos = new List<ArchiveFileInfo>();
            List<ArchiveFileInfo> archiveInArchiveInfos = new List<ArchiveFileInfo>();

            try
            {
                using (ArchiveFile archiveFile = new ArchiveFile(archive.Path))
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

                            var fileInfo = Map(entry, archive, archiveFile.Entries.Count(e => !e.IsFolder), archiveInArchive);
                            containerInfos.Add(fileInfo);

                            entryStream.Position = 0;
                            if (ArchiveFile.IsArchiveByStream(entryStream))
                            {
                                archiveInArchiveInfos.AddRange(GetInfoFromArchive(entryStream, fileInfo, archiveInArchive: true));
                            }
                        }
                    }
                    var freeze = containerInfos.Select(c => new ArchiveSimpleFileInfo(c)).ToArray();
                    foreach (var fileInfo in containerInfos)
                    {
                        fileInfo.ContainerFiles = freeze;
                    }
                }
                containerInfos.AddRange(archiveInArchiveInfos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{archive.Path}: {ex.Message}", ex);
            }
            return containerInfos.ToArray();
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

            foreach (var item in streams)
            {
                item.Item1.ContainerFilesCount = streams.Count;
            }
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
                    var archInfo = Map(entry, container, archive.Entries.Count(e => !e.IsFolder), isNested);
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
                        var nestedContainer = Map(entry, container, archive.Entries.Count(e => !e.IsFolder), true);
                        CollectImageStreams(nestedArchive, nestedContainer, true, streams, isSupportedExtension, cancelToken);
                        // Intermediate stream auto-disposes here → buffer returned
                    }
                    entryStream.Dispose();
                }
            }
        }

        public Stream GetStream(ArchiveSimpleFileInfo archiveSimpleFileInfo)
        {
            using (ArchiveFile archiveFile = new ArchiveFile(archiveSimpleFileInfo.ArchivePath))
            {
                foreach (var entry in archiveFile.Entries)
                {
                    if (entry.IsFolder)
                    {
                        continue;
                    }

                    if (Path.GetFileName(entry.FileName) == archiveSimpleFileInfo.Name)
                    {
                        var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size));
                        entry.Extract(entryStream);

                        entryStream.Position = 0;
                        return entryStream;
                    }
                }
            }
            return null;
        }

        public (ArchiveFileInfo, T)[] CalculateHashesInArchive<T>(ArchiveFileInfo[] data, Func<Stream, T> calculator)
        {
            List<(ArchiveFileInfo, T)> result = new List<(ArchiveFileInfo, T)>(data.Length);

            var first = data.First();
            Debug.Assert(first.Container != null);
            var firsetLevelArchive = first.ArchiveInArchive ? first.Container.Container : first.Container;
            var byContainer = data.GroupBy(d => d.Container.Name);
            using (ArchiveFile archiveFile = new ArchiveFile(firsetLevelArchive.Path))
            {
                foreach (var group in byContainer)
                {
                    bool groupFinded = false;

                    if (group.Key == firsetLevelArchive.Name)
                    {
                        groupFinded = true;
                        foreach (var item in group)
                        {
                            bool fileFinded = false;
                            foreach (var entry2 in archiveFile.Entries)
                            {
                                if (entry2.IsFolder)
                                {
                                    continue;
                                }

                                if (Path.GetFileName(entry2.FileName) == item.Name)
                                {
                                    fileFinded = true;
                                    using (var entryStream2 = new ChunkedMemoryStream(Convert.ToInt32(entry2.Size)))
                                    {
                                        entry2.Extract(entryStream2);

                                        entryStream2.Position = 0;
                                        result.Add((item, calculator(entryStream2)));
                                    }
                                    break;
                                }
                            }
                            Debug.Assert(fileFinded);
                        }
                    }
                    else
                    {
                        foreach (var entry in archiveFile.Entries)
                        {
                            if (entry.IsFolder)
                            {
                                continue;
                            }

                            if (Path.GetFileName(entry.FileName) == group.Key)
                            {
                                groupFinded = true;
                                using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
                                {
                                    entry.Extract(entryStream);
                                    entryStream.Position = 0;
                                    using (ArchiveFile archiveFile2 = new ArchiveFile(entryStream))
                                    {
                                        foreach (var item in group)
                                        {
                                            bool fileFinded = false;
                                            foreach (var entry2 in archiveFile2.Entries)
                                            {
                                                if (entry2.IsFolder)
                                                {
                                                    continue;
                                                }

                                                if (Path.GetFileName(entry2.FileName) == item.Name)
                                                {
                                                    fileFinded = true;
                                                    using (var entryStream2 = new ChunkedMemoryStream(Convert.ToInt32(entry2.Size)))
                                                    {
                                                        entry2.Extract(entryStream2);

                                                        entryStream2.Position = 0;
                                                        result.Add((item, calculator(entryStream2)));
                                                    }
                                                    break;
                                                }
                                            }
                                            Debug.Assert(fileFinded);
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }                      
                    Debug.Assert(groupFinded);
                }
                //foreach (var target in data)
                //{
                //    if (target.ArchiveInArchive)
                //    {
                //        foreach (var entry in archiveFile.Entries)
                //        {
                //            if (entry.IsFolder)
                //            {
                //                continue;
                //            }

                //            if (Path.GetFileName(entry.FileName) == group.Key)
                //            {
                //                groupFinded = true;
                //                using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
                //                {
                //                    entry.Extract(entryStream);

                //                    entryStream.Position = 0;
                //                    using (ArchiveFile archiveFile2 = new ArchiveFile(entryStream))
                //                    {
                //                        foreach (var item in group)
                //                        {
                //                            bool fileFinded = false;
                //                            foreach (var entry2 in archiveFile2.Entries)
                //                            {
                //                                if (entry2.IsFolder)
                //                                {
                //                                    continue;
                //                                }

                //                                if (Path.GetFileName(entry2.FileName) == item.Name)
                //                                {
                //                                    fileFinded = true;
                //                                    using (var entryStream2 = new ChunkedMemoryStream(Convert.ToInt32(entry2.Size)))
                //                                    {
                //                                        entry2.Extract(entryStream2);

                //                                        entryStream2.Position = 0;
                //                                        result.Add((item, calculator(entryStream2)));
                //                                    }
                //                                    break;
                //                                }
                //                            }
                //                            Debug.Assert(fileFinded);
                //                        }
                //                    }
                //                }
                //                break;
                //            }
                //        }
                //    }
                //    else
                //    {
                //        foreach (var entry in archiveFile.Entries)
                //        {
                //            if (entry.IsFolder)
                //            {
                //                continue;
                //            }

                //            if (Path.GetFileName(entry.FileName) == target.Name)
                //            {
                //                using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
                //                {
                //                    entry.Extract(entryStream);

                //                    entryStream.Position = 0;
                //                    //result.Add((target, calculator(entryStream)));
                //                }
                //                break;
                //            }
                //        }
                //    }
                //}
            }

            //var fileInfo = data.First();
            //Debug.Assert(fileInfo.Container != null);
            //if (fileInfo.ArchiveInArchive)
            //{
            //    using (ArchiveFile archiveFile = new ArchiveFile(fileInfo.Container.Container.Path))
            //    {
            //        var byContainer2 = data.GroupBy(d => d.Container.Name);
            //        var c2 = byContainer.SelectMany(f => f);
            //        foreach (var group in byContainer)
            //        {
            //            bool groupFinded = false;
            //            if (group.Key == fileInfo.Container.Container.Name)
            //            {
            //                groupFinded = true;
            //                foreach (var item in group)
            //                {
            //                    foreach (var entry in archiveFile.Entries)
            //                    {
            //                        if (entry.IsFolder)
            //                        {
            //                            continue;
            //                        }

            //                        if (Path.GetFileName(entry.FileName) == item.Name)
            //                        {
            //                            using (var entryStream2 = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
            //                            {
            //                                entry.Extract(entryStream2);

            //                                entryStream2.Position = 0;
            //                                result.Add((item, calculator(entryStream2)));
            //                            }
            //                            break;
            //                        }
            //                    }
            //                }
            //            }
            //            else
            //            {
            //                foreach (var entry in archiveFile.Entries)
            //                {
            //                    if (entry.IsFolder)
            //                    {
            //                        continue;
            //                    }

            //                    if (Path.GetFileName(entry.FileName) == group.Key)
            //                    {
            //                        groupFinded = true;
            //                        using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
            //                        {
            //                            entry.Extract(entryStream);

            //                            entryStream.Position = 0;
            //                            using (ArchiveFile archiveFile2 = new ArchiveFile(entryStream))
            //                            {
            //                                foreach (var item in group)
            //                                {
            //                                    bool fileFinded = false;
            //                                    foreach (var entry2 in archiveFile2.Entries)
            //                                    {
            //                                        if (entry2.IsFolder)
            //                                        {
            //                                            continue;
            //                                        }

            //                                        if (Path.GetFileName(entry2.FileName) == item.Name)
            //                                        {
            //                                            fileFinded = true;
            //                                            using (var entryStream2 = new ChunkedMemoryStream(Convert.ToInt32(entry2.Size)))
            //                                            {
            //                                                entry2.Extract(entryStream2);

            //                                                entryStream2.Position = 0;
            //                                                result.Add((item, calculator(entryStream2)));
            //                                            }
            //                                            break;
            //                                        }
            //                                    }
            //                                    Debug.Assert(fileFinded);
            //                                }
            //                            }
            //                        }
            //                        break;
            //                    }
            //                }
            //            }
            //            Debug.Assert(groupFinded);
            //        }
            //    }
            //}
            //else
            //{
            //    using (ArchiveFile archiveFile = new ArchiveFile(fileInfo.ArchivePath))
            //    {
            //        foreach (var item in data)
            //        {
            //            foreach (var entry in archiveFile.Entries)
            //            {
            //                if (entry.IsFolder)
            //                {
            //                    continue;
            //                }

            //                if (Path.GetFileName(entry.FileName) == item.Name)
            //                {
            //                    using (var entryStream = new ChunkedMemoryStream(Convert.ToInt32(entry.Size)))
            //                    {
            //                        entry.Extract(entryStream);

            //                        entryStream.Position = 0;
            //                        result.Add((item, calculator(entryStream)));
            //                    }
            //                    break;
            //                }
            //            }
            //        }                  
            //    }
            //}
            Debug.Assert(result.Count == data.Length);
            return result.ToArray();
        }
    }
}
