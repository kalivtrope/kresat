using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Kresat.Parsers;
using Kresat.Representations;
using Kresat.Scanners;
using Kresat.Solvers;

namespace Kresat.Tests {
    [SimpleJob(RunStrategy.Monitoring, launchCount: 0, warmupCount: 0, iterationCount: 1)]
    public class UnitPropagationBenchmarks {
        private IEnumerable<string> _datasets;
        public UnitPropagationBenchmarks(){
            string rootFolder = Path.Combine(Directory.GetCurrentDirectory(), "datasets");
            Console.WriteLine(rootFolder);
            _datasets = Directory.GetDirectories(rootFolder);
        }
        [ParamsSource(nameof(DatasetFiles))]
        public string DatasetFile {get; set;}
        public IEnumerable<string> DatasetFiles => GetAllDatasetFiles();
        private IEnumerable<string> GetAllDatasetFiles(){
            foreach(var dataset in _datasets){
                foreach(var file in Directory.GetFiles(dataset)){
                    if(file.EndsWith(".cnf") || file.EndsWith(".sat")){
                        yield return file;
                    }
                }
            }
        }
        [Benchmark]
        public void BenchmarkAdjacencyListWithDPLL(){
            var lines = Program.ReadFileContents(DatasetFile);
            IParser parser;
            if(DatasetFile.EndsWith(".cnf")){
                parser = new DimacsParser(new DimacsScanner(lines!).ScanTokens());
            }
            else{
                parser = new SmtLibParser(new SmtLibScanner(lines!).ScanTokens(), false);
            }
            CommonRepresentation cr = parser.ToCommonRepresentation();
            DPLLSolver solver = new DPLLSolver(cr, UnitPropType.adjacency);
            Verdict verdict = solver.Solve();
            Program.WriteFileContents("/dev/null", verdict.ToString());
        }


        [Benchmark]
        public void BenchmarkWatchedLiteralsWithDPLL(){
            var lines = Program.ReadFileContents(DatasetFile);
            IParser parser;
            if(DatasetFile.EndsWith(".cnf")){
                parser = new DimacsParser(new DimacsScanner(lines!).ScanTokens());
            }
            else{
                parser = new SmtLibParser(new SmtLibScanner(lines!).ScanTokens(), false);
            }
            CommonRepresentation cr = parser.ToCommonRepresentation();
            DPLLSolver solver = new DPLLSolver(cr, UnitPropType.watched);
            Verdict verdict = solver.Solve();
            Program.WriteFileContents("/dev/null", verdict.ToString());
        }
    }
}