using BenchmarkDotNet.Running;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.DataBase;

namespace DupTerminator.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var archiveInfoRepository = new ArchiveInfoRepository();
            //List<(ExtendedFileInfo container, ArchiveFileInfo[] files)>? generated = ArchiveFileInfoGenerator.GenerateDataset(10);
            //foreach (var item in generated)
            //{
            //    archiveInfoRepository.Add(item.container, item.files);
            //}
        

            //var summary = BenchmarkRunner.Run<Md5VsSha256>();
            //var summary = BenchmarkRunner.Run<IntroNativeMemory>();
            //var summary = BenchmarkRunner.Run<ExtractArchives>();
            var summary = BenchmarkRunner.Run<ArchiveInfoRepositoryBench>();
        }
    }
}
