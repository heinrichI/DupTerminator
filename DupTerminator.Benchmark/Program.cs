using BenchmarkDotNet.Running;

namespace DupTerminator.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<Md5VsSha256>();
            //var summary = BenchmarkRunner.Run<IntroNativeMemory>();
            var summary = BenchmarkRunner.Run<ExtractArchives>();
        }
    }
}
