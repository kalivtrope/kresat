using Kresat.Loggers;
using Kresat.Parsers;
using Kresat.Scanners;
using Kresat.Solvers;
using Kresat.Representations;
using System.CommandLine;
using System.Diagnostics;
using Kresat.Benchmarks;
using Kresat.Verifiers;

namespace Kresat {
    internal enum UnitPropType {
        adjacency,
        watched
    }
    enum Format {
        dimacs,
        smtlib
    }
    enum Strategy {
        cdcl,
        dpll
    }
    static class Configuration {
        public static FileInfo? inputPath;
        public static FileInfo? outputPath;
        public static bool useEquivalences;
        public static Format? format;
        public static UnitPropType unitPropType;
        public static Strategy strategy;
        public static long cacheSize;
        public static double multiplier;
        public static int unitRun;
        public static bool? useDimacs;
        public static bool? useSmtlib;
        public static FileInfo? datasetLocation;
    }
    public class Program {
        public static string? ReadFileContents(string path){
            try {
                StreamReader sr = new StreamReader(path);
                return sr.ReadToEnd();
            }
            catch (Exception ex){
                ErrorLogger.Report($"{ex.Message}");
            }
            return null;
        }
        public static void WriteFileContents(string path, string contents){
            try {
                StreamWriter sw = new StreamWriter(path);
                sw.Write(contents);
            }
            catch (Exception ex){
                ErrorLogger.Report($"{ex.Message}");
            }
        }
        public static int Main(string[] args){
            var rootCommand = new RootCommand("KreSAT solver");
            var inputArgument = new Argument<FileInfo?>(
                    name: "input",
                    description: "The input file")
                    {
                        Arity = ArgumentArity.ZeroOrOne
                    };
            var outputArgument = new Argument<FileInfo?>(
                    name: "output",
                    "The output file"
                    ) {
                        Arity = ArgumentArity.ZeroOrOne
            };
            var useEquivalencesOption = new Option<bool>(
                    ["--use-equivalences", "-e"],
                    description: "Use equivalences instead of => implications",
                    getDefaultValue: () => false
            ){
                Arity = ArgumentArity.Zero
            };

            var tseitinCommand = new Command("tseitin", "Convert formula to CNF using Tseitin transformation")
            {
                inputArgument,
                outputArgument,
                useEquivalencesOption
            };
            tseitinCommand.SetHandler((context) => {
                Configuration.inputPath = context.ParseResult.GetValueForArgument(inputArgument);
                Configuration.outputPath = context.ParseResult.GetValueForArgument(outputArgument);
                Configuration.useEquivalences = context.ParseResult.GetValueForOption(useEquivalencesOption);
                TseitinHandler();
            });


            var formatOption = new Option<Format?>(
                    ["--format", "-f"],
                    description: $"Specify input file format\nAlternatively use -c (for smtlib) or -s (for dimacs)"){};

            formatOption.AddValidator( v => {
                try {
                    var _ = v.GetValueOrDefault<Format?>();
                }
                catch {
                    v.ErrorMessage = $"Invalid format: got '{string.Join(' ', v.Tokens)}', expected one of '{Format.smtlib}' or '{Format.dimacs}'.";
                }
            });
            var unitPropagationDSOption = new Option<UnitPropType>(
                ["-u", "--unit-propagation"],
                description: $"Select unit propagation data structure",
                getDefaultValue: () => UnitPropType.watched
            ){};

            unitPropagationDSOption.AddValidator( v => {
                try {
                    var _ = v.GetValueOrDefault<UnitPropType>();
                }
                catch {
                    v.ErrorMessage = $"Invalid unit propagation data structure: got '{string.Join(' ', v.Tokens)}', expected one of '{UnitPropType.adjacency} or '{UnitPropType.watched}'";
                }
            });

            var strategyOption = new Option<Strategy>(
                    ["-a", "--strategy"],
                    description: "Select solving strategy",
                    getDefaultValue: () => Strategy.cdcl
            ){
                Arity = ArgumentArity.ZeroOrOne
            };
            strategyOption.AddValidator( v => {
                try {
                    var _ = v.GetValueOrDefault<Strategy>();
                }
                catch {
                    v.ErrorMessage = $"Invalid strategy: got '{string.Join(' ', v.Tokens)}', expected one of '{Strategy.cdcl} or '{Strategy.dpll}'";
                }
            });

            var cacheSizeOption = new Option<long>(
                ["-z", "--cache-size"],
                description: "Set initial cache size\nOnly used in CDCL",
                getDefaultValue: () => 10000
            ){
                Arity = ArgumentArity.ZeroOrOne
            };
            var multiplierOption = new Option<double>(
                ["-m", "--multiplier"],
                description: "Set cache size multiplier\nOnly used in CDCL",
                getDefaultValue: () => 1.1  
            ){
                Arity = ArgumentArity.ZeroOrOne
            };

            var unitRunOption = new Option<int>(
                ["-r", "--unit-run"],
                description: "Set Luby unit run constant\nOnly used in CDCL",
                getDefaultValue: () => 100
            ){
                Arity = ArgumentArity.ZeroOrOne
            };

            var useSmtlibOption = new Option<bool?>(
                ["-c"],
                description: $"Use {Format.smtlib} format"
            ){
                Arity = ArgumentArity.Zero
            };
            var useDimacsOption = new Option<bool?>(
                ["-s"],
                description: $"Use {Format.dimacs} format"
            ){
                Arity = ArgumentArity.Zero
            };
            var solveCommand = new Command("solve", "Solve a given formula"){
                formatOption,
                inputArgument,
                outputArgument,
                useSmtlibOption,
                useDimacsOption,
                unitPropagationDSOption,
                strategyOption,
                cacheSizeOption,
                multiplierOption,
                unitRunOption
            };
            solveCommand.AddValidator( (result) => {
                var res = result.FindResultFor(inputArgument);
                // if either --format, -c or -s is present, we needn't infer it from the input file
                if(result.GetValueForOption(formatOption) is not null
                   || result.GetValueForOption(useSmtlibOption) is not null
                   || result.GetValueForOption(useDimacsOption) is not null){
                    return;
                }
                // otherwise, try to infer the format from the file extensions
                if(res is not null){
                    var file = res.GetValueOrDefault<FileInfo?>();
                    if(file is not null){
                        var path = file.FullName;
                        if(path.EndsWith(".cnf") || path.EndsWith(".sat")){
                            return;
                        }
                        result.ErrorMessage = "Failed to infer input format. You can specify it using -f, -c or -s.";
                    }
                }
                else {
                    result.ErrorMessage = "The input format must be specified when reading from stdin.";
                }
            } );
            solveCommand.SetHandler((context) => {
                Configuration.format = context.ParseResult.GetValueForOption(formatOption);
                Configuration.inputPath = context.ParseResult.GetValueForArgument(inputArgument);
                Configuration.outputPath = context.ParseResult.GetValueForArgument(outputArgument);
                Configuration.useSmtlib = context.ParseResult.GetValueForOption(useSmtlibOption);
                Configuration.useDimacs = context.ParseResult.GetValueForOption(useDimacsOption);
                Configuration.unitPropType = context.ParseResult.GetValueForOption(unitPropagationDSOption);
                Configuration.strategy = context.ParseResult.GetValueForOption(strategyOption);
                Configuration.cacheSize = context.ParseResult.GetValueForOption(cacheSizeOption);
                Configuration.multiplier = context.ParseResult.GetValueForOption(multiplierOption);
                Configuration.unitRun = context.ParseResult.GetValueForOption(unitRunOption);
                SolveHandler();
            });

            var datasetLocationArgument = new Argument<string?>(
                name: "path to datasets",
                description: "path to dataset folder"
            ){
                Arity = ArgumentArity.ZeroOrOne
            };
            var benchmarkCommand = new Command("benchmark", "Run benchmarks"){
                datasetLocationArgument,
                cacheSizeOption,
                multiplierOption,
                unitRunOption
            };
            benchmarkCommand.SetHandler(BenchmarkHandler, datasetLocationArgument);
            rootCommand.AddCommand(tseitinCommand);
            rootCommand.AddCommand(solveCommand);
            rootCommand.AddCommand(benchmarkCommand);

            rootCommand.Invoke(args);
            return ErrorLogger.HadError ? 1 : 0;
        }

        private static void BenchmarkHandler(string? datasetLocation){
            UnitPropagationBenchmarks.Run(datasetLocation);
        }

        private static void SolveHandler(){
            if(Configuration.format is null){
                if(Configuration.useDimacs.HasValue && Configuration.useDimacs.Value){
                    Configuration.format = Format.dimacs;
                }
                else if(Configuration.useSmtlib.HasValue && Configuration.useSmtlib.Value){
                    Configuration.format = Format.smtlib;
                }
                else if(Configuration.inputPath is not null){
                    if(Configuration.inputPath.FullName.EndsWith(".cnf")){
                        Configuration.format = Format.dimacs;
                    }
                    else if (Configuration.inputPath.FullName.EndsWith(".sat")) {
                        Configuration.format = Format.smtlib;
                    }
                }
            }
            string? inputData = ReadFile(Configuration.inputPath);
            if(ErrorLogger.HadError){
                return;    
            }
            IParser parser;
            if(Configuration.format == Format.smtlib){
                parser = new SmtLibParser(new SmtLibScanner(inputData!).ScanTokens(), false);
            }
            else {
                parser = new DimacsParser(new DimacsScanner(inputData!).ScanTokens());
            }
            CommonRepresentation cr = parser.ToCommonRepresentation();
            if(ErrorLogger.HadError){
                return;
            }
            ISolver solver;
            if(Configuration.strategy == Strategy.dpll){
                solver = new DPLLSolver(cr, Configuration.unitPropType);
            }
            else{
                solver = new CDCLSolver(cr, Configuration.unitPropType, new ResetDeletionConfiguration{
                        Multiplier = Configuration.multiplier, UnitRun = Configuration.unitRun, CacheSize = Configuration.cacheSize});
            }
            Stopwatch stopwatch = new();
            stopwatch.Start();
            Verdict verdict = solver.Solve();
            stopwatch.Stop();
            if(!Verifier.Verify(cr, verdict)){
                throw new Exception($"invalid model {string.Join(' ', verdict.Model)}");
            }
            if(Configuration.format == Format.smtlib){
                WriteStringToFile(Configuration.outputPath, verdict.ToString(cr.OriginalMapping!) + "\n");
            }
            else{
                WriteStringToFile(Configuration.outputPath, verdict.ToString() + "\n");
            }
            WriteStringToFile(Configuration.outputPath, $"# of decisions: {solver.numDecisions}, # of propagated vars: {solver.unitPropSteps}");
            if(solver is CDCLSolver cdclSolver){
                WriteStringToFile(Configuration.outputPath, $", # of conflicts: {cdclSolver.totalNumConflicts}, # of restarts: {cdclSolver.numRestarts}");
            }
            WriteStringToFile(Configuration.outputPath, "\n");
            WriteStringToFile(Configuration.outputPath, $"Elapsed time: {stopwatch.Elapsed.TotalSeconds}\n\n");
        }

        private static string? ReadFile(FileInfo? inputPath){
            string? output = null;
            if(inputPath != null){
                output = ReadFileContents(inputPath.FullName);
            }
            else{
                try {
                    using var sr = new StreamReader(Console.OpenStandardInput());
                    output = sr.ReadToEnd();
                }
                catch (Exception ex){
                    ErrorLogger.Report($"{ex.Message}");
                }
            }
            return output;
        }

        private static void WriteStringToFile(FileInfo? outputPath, string data){
           if(outputPath != null){
                WriteFileContents(outputPath.FullName, data);
            }
            else{
                Console.Write(data);
            }
        }

        private static void TseitinHandler(){
            string? inputData = ReadFile(Configuration.inputPath);
            if(!ErrorLogger.HadError){
                SmtLibScanner scanner = new(inputData!);
                SmtLibParser parser = new(scanner.ScanTokens(), Configuration.useEquivalences);
                WriteStringToFile(Configuration.outputPath, parser.ToCommonRepresentation().ToString());
            }
        }
    }
}