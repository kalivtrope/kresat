using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kresat.Loggers;
using Kresat.Parsers;
using Kresat.Representations;
using Kresat.Scanners;
using Kresat.Solvers;

namespace Kresat.Benchmarks {
    public class UnitPropagationBenchmarks {
        static string WhereAmI([CallerFilePath] string callerFilePath = "") => callerFilePath;

        private static IEnumerable<string> GetAllDatasetFiles(string datasetFolder){
            foreach(var file in Directory.GetFiles(datasetFolder)){
                if(file.EndsWith(".cnf") || file.EndsWith(".sat")){
                    yield return file;
                }
            }
        }
        internal static TimeSpan Benchmark(string DatasetFile, UnitPropType upt){
            var lines = Program.ReadFileContents(DatasetFile);
            IParser parser;
            if(DatasetFile.EndsWith(".cnf")){
                parser = new DimacsParser(new DimacsScanner(lines!).ScanTokens());
            }
            else{
                parser = new SmtLibParser(new SmtLibScanner(lines!).ScanTokens(), false);
            }
            CommonRepresentation cr = parser.ToCommonRepresentation();
            ISolver solver;
            if(Configuration.strategy == Strategy.dpll){
                solver = new DPLLSolver(cr, upt);
            }
            else{
                solver = new CDCLSolver(cr, upt, new ResetDeletionConfiguration{
                        Multiplier = Configuration.multiplier, UnitRun = Configuration.unitRun, CacheSize = Configuration.cacheSize});
            }
            Stopwatch stopwatch = new();
            stopwatch.Start();
            Verdict verdict = solver.Solve();
            stopwatch.Stop();
            Program.WriteFileContents("/dev/null", verdict.ToString());
            return stopwatch.Elapsed;
        }
        public static TimeSpan BenchmarkAdjacencyList(string DatasetFile){
            return Benchmark(DatasetFile, UnitPropType.adjacency);
        }

        public static TimeSpan BenchmarkWatchedLiterals(string DatasetFile){
            return Benchmark(DatasetFile, UnitPropType.watched);
        }
        public static void Run(){
            string rootFolder;
            if(Configuration.datasetLocation is null){
                rootFolder = Path.Combine(Path.GetDirectoryName(WhereAmI()), "../../../datasets");
            }
            else {
                rootFolder = Configuration.datasetLocation.FullName;
            }
            IEnumerable<string>? DatasetFiles;
            try {    
                DatasetFiles = Directory.GetDirectories(rootFolder).SelectMany(GetAllDatasetFiles);
            } catch(Exception ex){
                ErrorLogger.Report(ex.Message);
                return;
            }

            var AdjacencyListResults = DatasetFiles
                                        .Select(
                path => (Path.GetDirectoryName(path), BenchmarkAdjacencyList(path))).
                GroupBy(pair => pair.Item1).Select(
                    group => {
                        string datasetName = Path.GetFileName(group.Key);
                        var averageTime = group.Select(p => p.Item2.TotalMilliseconds).Average();
                        return new {DatasetName = datasetName, AverageTime = averageTime};
                    }
                );
 
            var WatchLiteralsResults = DatasetFiles
            .Select(
                path => (Path.GetDirectoryName(path), BenchmarkWatchedLiterals(path)))
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
            try {    
                foreach(var result in results){
                    Console.WriteLine($"{result.First.DatasetName} \t {string.Format("{0:0.####}", result.First.AverageTime/1000)} \t {string.Format("{0:0.####}", result.Second.AverageTime/1000)}");
                }
            } catch(Exception ex){
                ErrorLogger.Report(ex.Message);
            }
        }
    }
}
