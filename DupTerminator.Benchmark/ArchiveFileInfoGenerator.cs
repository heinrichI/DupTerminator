using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.Benchmark
{
    public class ArchiveFileInfoGenerator
    {
        private const string CHARS = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static List<ArchiveFileInfo> GenerateArchiveFileInfos(int count, int seed = 42)
        {
            var random = new Random(seed);
            var result = new List<ArchiveFileInfo>(count);

            // Предопределенные значения для воспроизводимости
            var extensions = new[] { ".txt", ".log", ".dll", ".exe", ".png", ".jpg", ".7z", ".zip", ".rar" };
            var archiveNames = new[] { "archive", "backup", "data", "storage", "package" };
            var directories = new[] { "C:\\Files", "D:\\Data", "E:\\Archives", "/var/lib", "/mnt/storage" };

            for (int i = 0; i < count; i++)
            {
                // Генерация базового объекта
                var fileInfo = new ArchiveFileInfo
                {
                    Size = (ulong)random.Next(1024, 1048576 * 10),
                    Name = GenerateString(random, 5, 25) + extensions[random.Next(extensions.Length)],
                    LastWriteTime = DateTime.Now.AddDays(-random.Next(1, 365 * 5)),
                    ArchiveCRC = (uint)random.Next(),
                    ArchiveInArchive = random.NextDouble() > 0.9,
                    ArchiveExtension = extensions[random.Next(extensions.Length)],
                    ArchiveFileName = archiveNames[random.Next(archiveNames.Length)] +
                                    extensions[random.Next(3, extensions.Length)],
                    ArchivePath = directories[random.Next(directories.Length)],
                    //Container = new ExtendedFileInfo
                    //{
                    //    Name = GenerateString(random, 5, 25),
                    //    DirectoryName = GenerateString(random, 5, 25),
                    //},
                    Container = GenerateContainer(random)
                };

                fileInfo.Path = $"{fileInfo.ArchivePath}\\{fileInfo.ArchiveFileName}";

                // Вычисление производных свойств
                fileInfo.DirectoryName = System.IO.Path.GetDirectoryName(fileInfo.Path);
                fileInfo.Extension = System.IO.Path.GetExtension(fileInfo.Name);



                // Генерация вложенных файлов
                var containerCount = random.Next(1, 50);
                fileInfo.ContainerFiles = new ArchiveSimpleFileInfo[containerCount];
                fileInfo.ContainerFilesCount = containerCount;
                for (int j = 0; j < containerCount; j++)
                {
                    fileInfo.ContainerFiles[j] = new ArchiveSimpleFileInfo(fileInfo)
                    {
                        Size = fileInfo.Size / (ulong)(containerCount + 1),
                        Name = $"{System.IO.Path.GetFileNameWithoutExtension(fileInfo.Name)}_{j}{fileInfo.Extension}",
                        Path = fileInfo.Path
                    };
                }

                result.Add(fileInfo);
            }

            return result;
        }

        //private const int CONTAINER_COUNT = 200; // Количество контейнеров
        private const int FILES_PER_CONTAINER = 50; // Среднее количество файлов на контейнер

        public static List<(ExtendedFileInfo container, ArchiveFileInfo[] files)> GenerateDataset(int containerCount = 10000, int seed = 42)
        {
            Random rand = new Random(seed);
            var baseDate = DateTime.Parse("2023-01-01");
            List<(ExtendedFileInfo container, ArchiveFileInfo[] files)> result = new List<(ExtendedFileInfo container, ArchiveFileInfo[] files)>();

            // 1. Генерация контейнеров (ExtendedFileInfo)
            var containers = new List<ExtendedFileInfo>(containerCount);
            for (int i = 0; i < containerCount; i++)
            {
                var container = new ExtendedFileInfo
                {
                    Name = $"container_{i:0000}.zip",
                    Path = Path.Combine("archive_root", $"container_{i:0000}"),
                    Size = (ulong)rand.Next(1000000, 500000000),
                    LastWriteTime = baseDate.AddDays(rand.Next(-365, 365)),
                    DirectoryName = "archive_root",
                    Extension = ".zip",
                    ContainerFilesCount = 0 // Временное значение
                };

                //int filesInContainer = Math.Min(
                //    FILES_PER_CONTAINER + rand.Next(-10, 10),
                //    totalFiles / CONTAINER_COUNT
                //);
                int filesInContainer = FILES_PER_CONTAINER + rand.Next(-10, 10);

                var containerFiles = new List<ArchiveFileInfo>(filesInContainer);
                for (int j = 0; j < filesInContainer; j++)
                {
                    var file = new ArchiveFileInfo
                    {
                        Name = $"{GenerateName(rand, 12)}.{GenerateExtension(rand)}",
                        Size = (ulong)rand.Next(1024, 10000000),
                        LastWriteTime = container.LastWriteTime.AddHours(rand.Next(168)),
                        DirectoryName = container.Path,
                        ArchiveCRC = (uint)rand.Next(),
                        ArchivePath = container.Path,
                        ArchiveFileName = container.Name,
                        ArchiveExtension = container.Extension,
                        ArchiveInArchive = rand.NextDouble() > 0.9,
                        Container = container
                    };

                    file.Path = $"{container.Path}\\{file.Name}";
                    containerFiles.Add(file);
                }

                // Обновляем счётчик файлов в контейнере
                container.ContainerFilesCount = filesInContainer;

                result.Add((container, containerFiles.ToArray()));
            }
          
            return result;
        }

        private static string GenerateName(Random rand, int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789_";
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[rand.Next(chars.Length)])
                .ToArray());
        }

        private static string GenerateExtension(Random rand)
        {
            string[] extensions = { "txt", "log", "jpg", "png", "doc", "xls", "dll", "exe", "pdf", "mp3" };
            return extensions[rand.Next(extensions.Length)];
        }

        /// <summary>
        /// Генерирует «контейнер»‑файл (поле <see cref="ExtendedFileInfo.Container"/>).
        /// Чтобы не получить бесконечную рекурсию, ограничиваем глубину.
        /// </summary>
        private static ExtendedFileInfo GenerateContainer(Random rnd)
        {
            var container = new ExtendedFileInfo
            {
                Size = (ulong)rnd.Next(1_024, 5_000_000),
                Name = $"container_{rnd.Next(0, 1000)}.cbz",
                Path = $@"C:\Containers\{Guid.NewGuid():N}.cbz",
                LastWriteTime = DateTime.Now.AddDays(-rnd.Next(1, 365 * 5)),
                DirectoryName = @"C:\Containers",
                Extension = ".cbz",
            };

            return container;
        }

        private static string GenerateString(Random random, int minLen, int maxLen)
        {
            var length = random.Next(minLen, maxLen);
            var buffer = new char[length];

            for (var i = 0; i < length; i++)
            {
                buffer[i] = CHARS[random.Next(CHARS.Length)];
            }
            return new string(buffer);
        }
    }
}
