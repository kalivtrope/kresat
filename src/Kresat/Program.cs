using Kresat.Parsers;
using Kresat.Scanners;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Data;

namespace Kresat {
    public class Program {
        public static void ReadFile(string path){
            StreamReader sr = new StreamReader(path);
            var output = new DimacsScanner(sr.ReadToEnd()).ScanTokens();
            foreach(var token in output){
                Console.Write($"{token} ");
            }
        }
        public static async Task<int> Main(string[] args){
            var rootCommand = new RootCommand("KreSAT solver");
            var tseitinCommand = new Command("tseitin", "Convert formula to CNF using Tseitin transformation"){
                new Argument<FileInfo?>("input", "the input file") {
                    Arity = ArgumentArity.ZeroOrOne
                },
                new Argument<FileInfo?>("output", "the output file") {
                    Arity = ArgumentArity.ZeroOrOne
                },
                new Option<bool>(
                    ["--use-equivalences", "-e"],
                    description: "Use equivalences instead of => implications",
                    getDefaultValue: () => false
                    )
                    {}
            };
            tseitinCommand.Handler = CommandHandler.Create<FileInfo?, FileInfo?, bool>(TseitinHandler);

            var solveCommand = new Command("solve", "Solve a given formula"){
                new Option<string>(
                    ["--format", "-f"],
                    description: "The format of the input file (smtlib or dimacs)")
                    {
                        IsRequired = true,
                    },
                new Argument<FileInfo?>("input", "the input file"){
                    Arity = ArgumentArity.ZeroOrOne
                },
                new Argument<FileInfo?>("output", "the output file"){
                    Arity = ArgumentArity.ZeroOrOne
                }
            };
            solveCommand.Handler = CommandHandler.Create<string, FileInfo?, FileInfo?>(SolveHandler);

            rootCommand.AddCommand(tseitinCommand);
            rootCommand.AddCommand(solveCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static void SolveHandler(string format, FileInfo? inputPath, FileInfo? outputPath)
        {
            Console.WriteLine($"Input path: {inputPath}");
            if(format != "smtlib" && format != "dimacs"){
                Console.Error.WriteLine("Invalid format. Valid values are 'smtlib' or 'dimacs'");
                return;
            }
            string inputData;
            if(inputPath != null){
                inputData = File.ReadAllText(inputPath.FullName);
            }
            else {
                using var sr = new StreamReader(Console.OpenStandardInput());
                inputData = sr.ReadToEnd();
            }
            string outputData = "TODODODO";
            if(outputPath != null){
                File.WriteAllText(outputPath.FullName, outputData);
            }
            else{
                Console.WriteLine(outputData);
            }
        }

        private static void TseitinHandler(FileInfo? inputPath, FileInfo? outputPath, bool use_equivalences)
        {
            string inputData;
            if(inputPath != null){
                inputData = File.ReadAllText(inputPath.FullName);
            }
            else{
                using var sr = new StreamReader(Console.OpenStandardInput());
                inputData = sr.ReadToEnd();
            }
            string outputData = "TODODODODODO";
            if(outputPath != null){
                File.WriteAllText(outputPath.FullName, outputData);
            }
            else{
                Console.WriteLine(outputData);
            }
        }
    }
}