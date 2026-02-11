//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using DupTerminator.BusinessLogic.Model;

//namespace DupTerminator.Benchmark
//{
//    public class ArchiveFileInfoGenerator
//    {
//        private readonly Random _random;
//        private readonly List<ArchiveFileInfo> _generatedFiles;
//        private readonly List<string> _usedNames;
//        private readonly List<string> _usedPaths;
//        private readonly List<string> _usedArchiveNames;
//        private readonly List<string> _usedArchivePaths;
//        private readonly List<string> _usedArchiveExtensions;

//        public ArchiveFileInfoGenerator(int seed = 42)
//        {
//            _random = new Random(seed);
//            _generatedFiles = new List<ArchiveFileInfo>();
//            _usedNames = new List<string>();
//            _usedPaths = new List<string>();
//            _usedArchiveNames = new List<string>();
//            _usedArchivePaths = new List<string>();
//            _usedArchiveExtensions = new List<string>();
//        }

//        public List<ArchiveFileInfo> GenerateArchiveFiles(int count)
//        {
//            for (int i = 0; i < count; i++)
//            {
//                var file = GenerateArchiveFileInfo(i);
//                _generatedFiles.Add(file);
//            }

//            // Update containers after all files are generated
//            foreach (var file in _generatedFiles)
//            {
//                if (file.Container != null)
//                {
//                    file.Container.ContainerFilesCount++;
//                }

//                //if (file.ContainerFiles != null)
//                //{
//                //    foreach (var containerFile in file.ContainerFiles)
//                //    {
//                //        containerFile.Container = file;
//                //    }
//                //}
//            }

//            return _generatedFiles;
//        }

//        private ArchiveFileInfo GenerateArchiveFileInfo(int index)
//        {
//            var size = (ulong)_random.Next(1024, 1024 * 1024 * 100); // Random size between 1KB and 100MB
//            var name = GenerateUniqueName($"file_{index}");
//            var path = GenerateUniquePath($"path_{index}");
//            var lastWriteTime = DateTime.Now.AddDays(-_random.Next(0, 365)); // Random date within last year
//            var directoryName = path.Substring(0, path.LastIndexOf('\\'));
//            var extension = GenerateExtension();

//            var archiveCrc = (uint)_random.Next();
//            var archivePath = GenerateUniqueArchivePath($"archive_path_{index}");
//            var archiveExtension = GenerateArchiveExtension();
//            var archiveFileName = GenerateUniqueArchiveName($"archive_file_{index}");
//            var archiveInArchive = _random.Next(0, 2) == 1;

//            // Create ContainerFiles array if needed
//            ArchiveSimpleFileInfo[] containerFiles = null;
//            if (_random.Next(0, 2) == 1) // 50% chance to have container files
//            {
//                var containerFilesCount = _random.Next(1, 5);
//                containerFiles = new ArchiveSimpleFileInfo[containerFilesCount];
//                for (int i = 0; i < containerFilesCount; i++)
//                {
//                    containerFiles[i] = new ArchiveSimpleFileInfo
//                    {
//                        Size = (ulong)_random.Next(1024, 1024 * 1024),
//                        Name = GenerateUniqueName($"container_file_{index}_{i}"),
//                        Path = GenerateUniquePath($"container_path_{index}_{i}"),
//                        ArchivePath = GenerateUniqueArchivePath($"container_archive_path_{index}_{i}")
//                    };
//                }
//            }

//            // Select a random file as container (but not itself)
//            ExtendedFileInfo container = null;
//            if (_random.Next(0, 2) == 1 && _generatedFiles.Count > 0) // 50% chance to have a container
//            {
//                var randomIndex = _random.Next(0, _generatedFiles.Count);
//                container = _generatedFiles[randomIndex];
//            }

//            var fileInfo = new ArchiveFileInfo
//            {
//                Size = size,
//                Name = name,
//                Path = path,
//                LastWriteTime = lastWriteTime,
//                DirectoryName = directoryName,
//                Extension = extension,
//                ArchiveCRC = archiveCrc,
//                ArchivePath = archivePath,
//                ArchiveExtension = archiveExtension,
//                ArchiveFileName = archiveFileName,
//                ArchiveInArchive = archiveInArchive,
//                ContainerFiles = containerFiles,
//                Container = container
//            };

//            return fileInfo;
//        }

//        private string GenerateUniqueName(string baseName)
//        {
//            var name = $"{baseName}_{_random.Next(1000, 9999)}";
//            _usedNames.Add(name);
//            return name;
//        }

//        private string GenerateUniquePath(string basePath)
//        {
//            var path = $"{basePath}\\dir{_random.Next(1, 10)}\\subdir{_random.Next(1, 5)}";
//            _usedPaths.Add(path);
//            return path;
//        }

//        private string GenerateExtension()
//        {
//            var extensions = new[] { ".txt", ".doc", ".pdf", ".jpg", ".png", ".zip", ".rar", ".exe", ".dll", ".config" };
//            var extension = extensions[_random.Next(0, extensions.Length)];
//            return extension;
//        }

//        private string GenerateUniqueArchiveName(string baseName)
//        {
//            var name = $"{baseName}_archive{_random.Next(1000, 9999)}";
//            _usedArchiveNames.Add(name);
//            return name;
//        }

//        private string GenerateUniqueArchivePath(string basePath)
//        {
//            var path = $"{basePath}\\archive{_random.Next(1, 10)}";
//            _usedArchivePaths.Add(path);
//            return path;
//        }

//        private string GenerateArchiveExtension()
//        {
//            var extensions = new[] { ".zip", ".rar", ".7z", ".tar", ".gz" };
//            var extension = extensions[_random.Next(0, extensions.Length)];
//            return extension;
//        }
//    }
//}
