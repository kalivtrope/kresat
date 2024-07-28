using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kresat.Parsers;
using Kresat.Representations;
using Kresat.Scanners;
using Kresat.Solvers;

namespace Kresat.Tests {
    public class UnitPropagationBenchmarks {
        static string WhereAmI([CallerFilePath] string callerFilePath = "") => callerFilePath;

        private static IEnumerable<string> GetAllDatasetFiles(string datasetFolder){
            foreach(var file in Directory.GetFiles(datasetFolder)){
                if(file.EndsWith(".cnf") || file.EndsWith(".sat")){
                    yield return file;
                }
            }
        }
        internal static TimeSpan BenchmarkDPLL(string DatasetFile, UnitPropType upt){
            var lines = Program.ReadFileContents(DatasetFile);
            IParser parser;
            if(DatasetFile.EndsWith(".cnf")){
                parser = new DimacsParser(new DimacsScanner(lines!).ScanTokens());
            }
            else{
                parser = new SmtLibParser(new SmtLibScanner(lines!).ScanTokens(), false);
            }
            CommonRepresentation cr = parser.ToCommonRepresentation();
            DPLLSolver solver = new DPLLSolver(cr, upt);
            Stopwatch stopwatch = new();
            stopwatch.Start();
            Verdict verdict = solver.Solve();
            stopwatch.Stop();
            Program.WriteFileContents("/dev/null", verdict.ToString());
            //Console.WriteLine($"processed {DatasetFile} {upt} in {stopwatch.Elapsed}");
            return stopwatch.Elapsed;
        }
        public static TimeSpan BenchmarkAdjacencyListWithDPLL(string DatasetFile){
            return BenchmarkDPLL(DatasetFile, UnitPropType.adjacency);
        }

        public static TimeSpan BenchmarkWatchedLiteralsWithDPLL(string DatasetFile){
            return BenchmarkDPLL(DatasetFile, UnitPropType.watched);
        }
        public static void Run(string? rootFolder){
            if(rootFolder is null){
                rootFolder = Path.Combine(Path.GetDirectoryName(WhereAmI()), "../../../datasets");
            }
            var DatasetFiles = Directory.GetDirectories(rootFolder).SelectMany(GetAllDatasetFiles);

            var AdjacencyListResults = DatasetFiles
                                        .Select(
                path => (Path.GetDirectoryName(path), BenchmarkAdjacencyListWithDPLL(path))).
                GroupBy(pair => pair.Item1).Select(
                    group => {
                        string datasetName = Path.GetFileName(group.Key);
                        var averageTime = group.Select(p => p.Item2.TotalMilliseconds).Average();
                        return new {DatasetName = datasetName, AverageTime = averageTime};
                    }
                );
 
            var WatchLiteralsResults = DatasetFiles
            .Select(
                path => (Path.GetDirectoryName(path), BenchmarkWatchedLiteralsWithDPLL(path)))
            .GroupBy(
                pair => pair.Item1
            ).Select(
                group => {
                    string datasetName = Path.GetFileName(group.Key);
                    var averageTime = group.Select(p => p.Item2.TotalMilliseconds).Average();
                    return new { DatasetName = datasetName, AverageTime = averageTime};
                }
            );
            var results = AdjacencyListResults.Zip(WatchLiteralsResults);
            Console.WriteLine("Dataset \t Adjacency avg (sec) \t Watched avg (sec)");
            foreach(var result in results){
                Console.WriteLine($"{result.First.DatasetName} \t {string.Format("{0:0.####}", result.First.AverageTime/1000)} \t {string.Format("{0:0.####}", result.Second.AverageTime/1000)}");
            }
        }
    }
}
