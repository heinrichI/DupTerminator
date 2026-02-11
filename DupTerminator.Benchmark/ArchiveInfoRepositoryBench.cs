//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using BenchmarkDotNet.Attributes;
//using DupTerminator.BusinessLogic.Model;
//using DupTerminator.DataBase;
//using Microsoft.CodeAnalysis.Diagnostics;
//using SevenZipExtractor;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace DupTerminator.Benchmark
//{
//    [SimpleJob(launchCount: 1, warmupCount: 0, iterationCount: 1, invocationCount: 10)]
//    public class ArchiveInfoRepositoryBench
//    {
//        private ArchiveInfoRepository _archiveInfoRepository;
//        private ArchiveInfoRepositoryMemP _archiveInfoRepositoryMemP;
//        private ArchiveInfoRepositoryMesP _archiveInfoRepositoryMesP;
//        private List<(ExtendedFileInfo container, ArchiveFileInfo[] files)> _generated;

//        [GlobalSetup]
//        public void GlobalSetup()
//        {
//            _archiveInfoRepository = new ArchiveInfoRepository();
//            _archiveInfoRepositoryMemP = new ArchiveInfoRepositoryMemP();
//            _archiveInfoRepositoryMesP = new ArchiveInfoRepositoryMesP();
//            _generated = ArchiveFileInfoGenerator.GenerateDataset(10000);
//            foreach (var item in _generated)
//            {
//                _archiveInfoRepository.Add(item.container, item.files);
//            }
//            foreach (var item in _generated)
//            {
//                _archiveInfoRepositoryMemP.Add(item.container, item.files);
//            }
//            foreach (var item in _generated)
//            {
//                _archiveInfoRepositoryMesP.Add(item.container, item.files);
//            }
//        }

//        //| Method      | Mean    | Error |
//        //|------------ |--------:|------:|
//        //| GzipJson    | 2.985 s |    NA |
//        //| MemoryPack  | 1.892 s |    NA |
//        //| MessagePack | 1.639 s |    NA |
//        //в реальных кейсах
//        //MemoryPack ElapsedTime: 00:00:03.0179314
//        //MessagePack ElapsedTime: 00:00:05.41


//        [Benchmark]
//        public void GzipJson()
//        {
//            foreach (var item in _generated)
//            {
//                var fromDb = _archiveInfoRepository.Get(item.container.Path, item.container.LastWriteTime, item.container.Size);
//                if (fromDb == null)
//                    throw new NullReferenceException(nameof(fromDb));
//                if (fromDb.Length != item.files.Length)
//                    throw new Exception("Длины не сходятся!");
//                for (int i = 0; i < fromDb.Length; i++)
//                {
//                    if (fromDb[i].Name != item.files[i].Name)
//                        throw new Exception("Name не сходятся!");
//                    if (fromDb[i].Path != item.files[i].Path)
//                        throw new Exception("Path не сходятся!");
//                    if (fromDb[i].Size != item.files[i].Size)
//                        throw new Exception("Size не сходятся!");
//                    if (fromDb[i].Extension != item.files[i].Extension)
//                        throw new Exception("Extension не сходятся!");
//                    if (fromDb[i].LastWriteTime != item.files[i].LastWriteTime)
//                        throw new Exception("LastWriteTime не сходятся!");
//                    if (fromDb[i].ArchiveExtension != item.files[i].ArchiveExtension)
//                        throw new Exception("ArchiveExtension не сходятся!");
//                    if (fromDb[i].ArchiveCRC != item.files[i].ArchiveCRC)
//                        throw new Exception("ArchiveCRC не сходятся!");
//                    if (fromDb[i].ArchiveFileName != item.files[i].ArchiveFileName)
//                        throw new Exception("ArchiveFileName не сходятся!");
//                    if (fromDb[i].ArchiveInArchive != item.files[i].ArchiveInArchive)
//                        throw new Exception("ArchiveFileName не сходятся!");
//                    if (fromDb[i].ArchivePath != item.files[i].ArchivePath)
//                        throw new Exception("ArchiveFileName не сходятся!");
//                    if (fromDb[i].Container.Extension != item.files[i].Container.Extension)
//                        throw new Exception("Container.Extension не сходятся!");
//                    if (fromDb[i].Container.LastWriteTime != item.files[i].Container.LastWriteTime)
//                        throw new Exception("Container.LastWriteTime не сходятся!");
//                    if (fromDb[i].Container.Name != item.files[i].Container.Name)
//                        throw new Exception("Container.LastWriteTime не сходятся!");
//                    if (fromDb[i].Container.DirectoryName != item.files[i].Container.DirectoryName)
//                        throw new Exception("Container.DirectoryName не сходятся!");
//                    if (fromDb[i].Container.Path != item.files[i].Container.Path)
//                        throw new Exception("Container.Path не сходятся!");
//                    if (fromDb[i].Container.Size != item.files[i].Container.Size)
//                        throw new Exception("Container.Size не сходятся!");
//                    if (fromDb[i].ContainerFiles is not null && item.files[i].ContainerFiles is not null && fromDb[i].ContainerFiles.Length != item.files[i].ContainerFiles.Length)
//                        throw new Exception("ContainerFiles.Length не сходятся!");
//                    if (fromDb[i].ContainerFilesCount != item.files[i].ContainerFilesCount)
//                        throw new Exception("ContainerFilesCount не сходятся!");
//                }
//            }
//        }

//        [Benchmark]
//        public void MemoryPack()
//        {
//            foreach (var item in _generated)
//            {
//                var fromDb = _archiveInfoRepositoryMemP.Get(item.container.Path, item.container.LastWriteTime, item.container.Size);
//                if (fromDb == null)
//                    throw new NullReferenceException(nameof(fromDb));
//                if (fromDb.Length != item.files.Length)
//                    throw new Exception("Длины не сходятся!");
//                for (int i = 0; i < fromDb.Length; i++)
//                {
//                    if (fromDb[i].Name != item.files[i].Name)
//                        throw new Exception("Name не сходятся!");
//                    if (fromDb[i].Path != item.files[i].Path)
//                        throw new Exception("Path не сходятся!");
//                    if (fromDb[i].Size != item.files[i].Size)
//                        throw new Exception("Size не сходятся!");
//                    if (fromDb[i].Extension != item.files[i].Extension)
//                        throw new Exception("Extension не сходятся!");
//                    if (fromDb[i].LastWriteTime != item.files[i].LastWriteTime)
//                        throw new Exception("LastWriteTime не сходятся!");
//                    if (fromDb[i].ArchiveExtension != item.files[i].ArchiveExtension)
//                        throw new Exception("ArchiveExtension не сходятся!");
//                    if (fromDb[i].ArchiveCRC != item.files[i].ArchiveCRC)
//                        throw new Exception("ArchiveCRC не сходятся!");
//                    if (fromDb[i].ArchiveFileName != item.files[i].ArchiveFileName)
//                        throw new Exception("ArchiveFileName не сходятся!");
//                    if (fromDb[i].ArchiveInArchive != item.files[i].ArchiveInArchive)
//                        throw new Exception("ArchiveFileName не сходятся!");
//                    if (fromDb[i].ArchivePath != item.files[i].ArchivePath)
//                        throw new Exception("ArchiveFileName не сходятся!");
//                    if (fromDb[i].Container.Extension != item.files[i].Container.Extension)
//                        throw new Exception("Container.Extension не сходятся!");
//                    if (fromDb[i].Container.LastWriteTime != item.files[i].Container.LastWriteTime)
//                        throw new Exception("Container.LastWriteTime не сходятся!");
//                    if (fromDb[i].Container.Name != item.files[i].Container.Name)
//                        throw new Exception("Container.LastWriteTime не сходятся!");
//                    if (fromDb[i].Container.DirectoryName != item.files[i].Container.DirectoryName)
//                        throw new Exception("Container.DirectoryName не сходятся!");
//                    if (fromDb[i].Container.Path != item.files[i].Container.Path)
//                        throw new Exception("Container.Path не сходятся!");
//                    if (fromDb[i].Container.Size != item.files[i].Container.Size)
//                        throw new Exception("Container.Size не сходятся!");
//                    if (fromDb[i].ContainerFiles is not null && item.files[i].ContainerFiles is not null && fromDb[i].ContainerFiles.Length != item.files[i].ContainerFiles.Length)
//                        throw new Exception("ContainerFiles.Length не сходятся!");
//                    if (fromDb[i].ContainerFilesCount != item.files[i].ContainerFilesCount)
//                        throw new Exception("ContainerFilesCount не сходятся!");
//                }
//            }
//        }

//        [Benchmark]
//        public void MessagePack()
//        {
//            foreach (var item in _generated)
//            {
//                var fromDb = _archiveInfoRepositoryMesP.Get(item.container.Path, item.container.LastWriteTime, item.container.Size);
//                if (fromDb == null)
//                    throw new NullReferenceException(nameof(fromDb));
//                if (fromDb.Length != item.files.Length)
//                    throw new Exception("Длины не сходятся!");
//                for (int i = 0; i < fromDb.Length; i++)
//                {
//                    if (fromDb[i].Name != item.files[i].Name)
//                        throw new Exception("Name не сходятся!");
//                    if (fromDb[i].Path != item.files[i].Path)
//                        throw new Exception("Path не сходятся!");
//                    if (fromDb[i].Size != item.files[i].Size)
//                        throw new Exception("Size не сходятся!");
//                    if (fromDb[i].Extension != item.files[i].Extension)
//                        throw new Exception("Extension не сходятся!");
//                    if (fromDb[i].LastWriteTime != item.files[i].LastWriteTime)
//                        throw new Exception("LastWriteTime не сходятся!");
//                    if (fromDb[i].ArchiveExtension != item.files[i].ArchiveExtension)
//                        throw new Exception("ArchiveExtension не сходятся!");
//                    if (fromDb[i].ArchiveCRC != item.files[i].ArchiveCRC)
//                        throw new Exception("ArchiveCRC не сходятся!");
//                    if (fromDb[i].ArchiveFileName != item.files[i].ArchiveFileName)
//                        throw new Exception("ArchiveFileName не сходятся!");
//                    if (fromDb[i].ArchiveInArchive != item.files[i].ArchiveInArchive)
//                        throw new Exception("ArchiveFileName не сходятся!");
//                    if (fromDb[i].ArchivePath != item.files[i].ArchivePath)
//                        throw new Exception("ArchiveFileName не сходятся!");
//                    if (fromDb[i].Container.Extension != item.files[i].Container.Extension)
//                        throw new Exception("Container.Extension не сходятся!");
//                    if (fromDb[i].Container.LastWriteTime != item.files[i].Container.LastWriteTime)
//                        throw new Exception("Container.LastWriteTime не сходятся!");
//                    if (fromDb[i].Container.Name != item.files[i].Container.Name)
//                        throw new Exception("Container.LastWriteTime не сходятся!");
//                    if (fromDb[i].Container.DirectoryName != item.files[i].Container.DirectoryName)
//                        throw new Exception("Container.DirectoryName не сходятся!");
//                    if (fromDb[i].Container.Path != item.files[i].Container.Path)
//                        throw new Exception("Container.Path не сходятся!");
//                    if (fromDb[i].Container.Size != item.files[i].Container.Size)
//                        throw new Exception("Container.Size не сходятся!");
//                    if (fromDb[i].ContainerFiles is not null && item.files[i].ContainerFiles is not null && fromDb[i].ContainerFiles.Length != item.files[i].ContainerFiles.Length)
//                        throw new Exception("ContainerFiles.Length не сходятся!");
//                    if (fromDb[i].ContainerFilesCount != item.files[i].ContainerFilesCount)
//                        throw new Exception("ContainerFilesCount не сходятся!");
//                }
//            }
//        }
//    }
//}
