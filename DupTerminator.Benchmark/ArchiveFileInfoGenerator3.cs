//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using DupTerminator.BusinessLogic.Model;

//namespace DupTerminator.Benchmark
//{
//    public static class ArchiveFileInfoGenerator3
//    {
//        /// <summary>
//        /// Генерирует <paramref name="count"/> объектов <see cref="ArchiveFileInfo"/>
//        /// с детерминированным набором данных.
//        /// </summary>
//        /// <param name="count">Сколько объектов создать.</param>
//        /// <param name="seed">Seed для <see cref="Random"/> – одинаковый seed → одинаковый набор.</param>
//        /// <returns>Список готовых к использованию объектов.</returns>
//        public static List<ArchiveFileInfo> Generate(int count, int seed = 42)
//        {
//            var rnd = new Random(seed);
//            var list = new List<ArchiveFileInfo>(count);

//            for (int i = 0; i < count; i++)
//            {
//                var af = new ArchiveFileInfo
//                {
//                    // ---------- SimpleFileInfo ----------
//                    Size = (ulong)rnd.Next(1_024, 10_000_000), // от 1 KB до ~10 MB
//                    Name = $"file_{i:D5}.dat",
//                    Path = $@"C:\Data\Archive\{i:D5}\file_{i:D5}.dat",

//                    // ---------- ExtendedFileInfo ----------
//                    LastWriteTime = RandomDate(rnd, new DateTime(2020, 1, 1), DateTime.UtcNow),
//                    DirectoryName = $@"C:\Data\Archive\{i:D5}",
//                    Extension = ".dat",

//                    // ---------- ArchiveFileInfo ----------
//                    ArchiveCRC = (uint)rnd.Next(),
//                    ArchivePath = $@"D:\Archives\archive_{i % 100:D2}.zip",
//                    ArchiveExtension = ".zip",
//                    ArchiveFileName = $"archive_{i % 100:D2}.zip",
//                    ArchiveInArchive = rnd.NextDouble() < 0.05, // 5 % вложенных архивов
//                    Container = GenerateContainer(rnd, depth: 0),

//                    // ---------- ContainerFiles ----------
//                    ContainerFiles = GenerateContainerFiles(rnd, maxCount: 5)
//                };

//                list.Add(af);
//            }

//            return list;
//        }

//        // -----------------------------------------------------------------
//        // Вспомогательные методы
//        // -----------------------------------------------------------------
//        private static DateTime RandomDate(Random rnd, DateTime from, DateTime to)
//        {
//            var range = to - from;
//            var randTicks = (long)(rnd.NextDouble() * range.Ticks);
//            return from.AddTicks(randTicks);
//        }

//        /// <summary>
//        /// Генерирует «контейнер»‑файл (поле <see cref="ExtendedFileInfo.Container"/>).
//        /// Чтобы не получить бесконечную рекурсию, ограничиваем глубину.
//        /// </summary>
//        private static ExtendedFileInfo GenerateContainer(Random rnd, int depth)
//        {
//            if (depth > 2) // ограничиваем вложенность
//                return null!; // поле может быть null – в вашем коде оно не помечено nullable

//            var hasContainer = rnd.NextDouble() < 0.2; // 20 % имеют контейнер
//            if (!hasContainer) return null!;

//            var container = new ExtendedFileInfo
//            {
//                Size = (ulong)rnd.Next(1_024, 5_000_000),
//                Name = $"container_{rnd.Next(0, 1000)}.bin",
//                Path = $@"C:\Containers\{Guid.NewGuid():N}.bin",
//                LastWriteTime = RandomDate(rnd, new DateTime(2019, 1, 1), DateTime.UtcNow),
//                DirectoryName = @"C:\Containers",
//                Extension = ".bin",
//                Container = GenerateContainer(rnd, depth + 1) // рекурсия
//            };

//            return container;
//        }

//        /// <summary>
//        /// Генерирует массив простых файлов, находящихся внутри архива.
//        /// </summary>
//        private static ArchiveSimpleFileInfo[] GenerateContainerFiles(Random rnd, int maxCount)
//        {
//            int count = rnd.Next(0, maxCount + 1);
//            var arr = new ArchiveSimpleFileInfo[count];
//            for (int i = 0; i < count; i++)
//            {
//                arr[i] = new ArchiveSimpleFileInfo
//                {
//                    Size = (ulong)rnd.Next(256, 2_000_000),
//                    Name = $"inner_{i:D3}.txt",
//                    Path = $@"inner_{i:D3}.txt",
//                    ArchivePath = $@"D:\Archives\inner_{i:D3}.zip"
//                };
//            }

//            return arr;
//        }
//    }
//}
