using Kresat.Loggers;
using Kresat.Parsers;
using Kresat.Scanners;
using Kresat.Solvers;
using Kresat.Representations;
using System.CommandLine;
using System.Diagnostics;
using Kresat.Tests;

namespace Kresat {
    internal enum UnitPropType {
        adjacency,
        watched
    }
    public class Program {
        enum Format {
            dimacs,
            smtlib
        }

        public static string? ReadFileContents(string path){
            try {
                StreamReader sr = new StreamReader(path);
                return sr.ReadToEnd();
            }
            catch (Exception ex){
                ErrorLogger.Report(0, $"{ex.Message}");
            }
            return null;
        }
        public static void WriteFileContents(string path, string contents){
            try {
                StreamWriter sw = new StreamWriter(path);
                sw.Write(contents);
            }
            catch (Exception ex){
                ErrorLogger.Report(0, $"{ex.Message}");
            }
        }
        public static async Task<int> Main(string[] args){
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
            var useEquivalencesOption = new Option<bool?>(
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
            tseitinCommand.SetHandler(TseitinHandler, inputArgument, outputArgument, useEquivalencesOption);

            var formatOption = new Option<Format?>(
                    ["--format", "-f"],
                    description: $"Specify input file format\nAlternatively use -c (for smtlib) or -s (for dimacs)"){};

            formatOption.AddValidator( v => {
                try {
                    var x = v.GetValueOrDefault<Format?>();
                }
                catch {
                    v.ErrorMessage = $"Invalid format: got '{v.GetValueOrDefault<string>()}', expected one of '{Format.smtlib}' or '{Format.dimacs}'.";
                }
            }
            );

            var unitPropagationDSOption = new Option<UnitPropType?>(
                ["--unit-prop", "-u"],
                description: $"Specify unit propagation data structure",
                getDefaultValue: () => UnitPropType.watched
            ){};

            unitPropagationDSOption.AddValidator( v => {
                try {
                    var x = v.GetValueOrDefault<UnitPropType?>();
                }
                catch {
                    v.ErrorMessage = $"Invalid unit propagation data structure: got '{v.GetValueOrDefault<string>()}', expected one of '{UnitPropType.adjacency} or '{UnitPropType.watched}'";
                }
            });

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
                unitPropagationDSOption
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
            solveCommand.SetHandler(SolveHandler, formatOption, inputArgument, outputArgument,
                                    useSmtlibOption, useDimacsOption, unitPropagationDSOption);

            var datasetLocationArgument = new Argument<string?>(
                name: "path to datasets",
                description: "path to the dataset folder"
            ){
                Arity = ArgumentArity.ZeroOrOne
            };
            var benchmarkCommand = new Command("benchmark", "Run benchmarks"){
                datasetLocationArgument
            };
            benchmarkCommand.SetHandler(BenchmarkHandler, datasetLocationArgument);
            rootCommand.AddCommand(tseitinCommand);
            rootCommand.AddCommand(solveCommand);
            rootCommand.AddCommand(benchmarkCommand);

            await rootCommand.InvokeAsync(args);
            return ErrorLogger.HadError ? 1 : 0;
        }

        private static void BenchmarkHandler(string? datasetLocation){
            UnitPropagationBenchmarks.Run(datasetLocation);
        }

        private static void SolveHandler(Format? format, FileInfo? inputPath, FileInfo? outputPath,
                                         bool? useSmtlib, bool? useDimacs, UnitPropType? unitProp)
        {
            if(format is null){
                if(useDimacs.HasValue && useDimacs.Value){
                    format = Format.dimacs;
                }
                else if(useSmtlib.HasValue && useSmtlib.Value){
                    format = Format.smtlib;
                }
                else if(inputPath is not null){
                    if(inputPath.FullName.EndsWith(".cnf")){
                        format = Format.dimacs;
                    }
                    else if (inputPath.FullName.EndsWith(".sat")) {
                        format = Format.smtlib;
                    }
                }
            }
            if(unitProp is null){
                unitProp = UnitPropType.watched;
            }
            string? inputData = ReadFile(inputPath);
            if(ErrorLogger.HadError){
                return;    
            }
            IParser parser;
            if(format == Format.smtlib){
                parser = new SmtLibParser(new SmtLibScanner(inputData!).ScanTokens(), false);
            }
            else {
                parser = new DimacsParser(new DimacsScanner(inputData!).ScanTokens());
            }
            CommonRepresentation cr = parser.ToCommonRepresentation();
            if(ErrorLogger.HadError){
                return;
            }
            Process currentProcess = Process.GetCurrentProcess();
            DPLLSolver solver = new DPLLSolver(cr, unitProp.Value);
            var currTime = currentProcess.TotalProcessorTime;
            Verdict verdict = solver.Solve();
            var finalTime = currentProcess.TotalProcessorTime;
            if(format == Format.smtlib){
                WriteFile(outputPath, verdict.ToString(cr.OriginalMapping!) + "\n");
            }
            else{
                WriteFile(outputPath, verdict.ToString() + "\n");
            }
            WriteFile(outputPath, $"# of decisions: {solver.numDecisions}, # of propagated vars: {solver.unitPropSteps}\n");
            WriteFile(outputPath, $"Elapsed time: {(finalTime-currTime).TotalSeconds}\n\n");
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
                    ErrorLogger.Report(0, $"{ex.Message}");
                }
            }
            return output;
        }

        private static void WriteFile(FileInfo? outputPath, string data){
           if(outputPath != null){
                WriteFileContents(outputPath.FullName, data);
            }
            else{
                Console.Write(data);
            }
        }

        private static void TseitinHandler(FileInfo? inputPath, FileInfo? outputPath, bool? useEquivalences)
        {
            string? inputData = ReadFile(inputPath);
            if(!useEquivalences.HasValue){
                useEquivalences = false;
            }
            if(!ErrorLogger.HadError){
                SmtLibScanner scanner = new(inputData!);
                /*foreach(var token in scanner.ScanTokens()){
                    outputData += $"{token} ";
                }*/
                SmtLibParser parser = new(scanner.ScanTokens(), useEquivalences.Value);
                WriteFile(outputPath, parser.ToCommonRepresentation().ToString());
            }
        }
    }
}