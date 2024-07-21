using Kresat.Loggers;
using Kresat.Parsers;
using Kresat.Scanners;
using System.CommandLine;

namespace Kresat {
    public class Program {
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
            var useEquivalencesOption = new Option<bool>(
                    ["--use-equivalences", "-e"],
                    description: "Use equivalences instead of => implications",
                    getDefaultValue: () => false
            );
            var tseitinCommand = new Command("tseitin", "Convert formula to CNF using Tseitin transformation")
            {
                inputArgument,
                outputArgument,
                useEquivalencesOption
            };
            tseitinCommand.SetHandler(TseitinHandler, inputArgument, outputArgument, useEquivalencesOption);

            var formatOption = new Option<string>(
                    ["--format", "-f"],
                    description: "The format of the input file (smtlib or dimacs)")
                    {
                        IsRequired = true,
                    };
            var solveCommand = new Command("solve", "Solve a given formula"){
                formatOption,
                inputArgument,
                outputArgument
            };
            solveCommand.SetHandler(SolveHandler, formatOption, inputArgument, outputArgument);

            rootCommand.AddCommand(tseitinCommand);
            rootCommand.AddCommand(solveCommand);

            return await rootCommand.InvokeAsync(args);
        }

        private static void SolveHandler(string format, FileInfo? inputPath, FileInfo? outputPath)
        {
            if(format != "smtlib" && format != "dimacs"){
                Console.Error.WriteLine("Invalid format. Valid values are 'smtlib' or 'dimacs'");
                return;
            }
            string? inputData;
            if(inputPath != null){
                inputData = ReadFileContents(inputPath.FullName);
            }
            else {
                try {
                    using var sr = new StreamReader(Console.OpenStandardInput());
                    inputData = sr.ReadToEnd();
                }
                catch (Exception ex){
                    ErrorLogger.Report(0, $"{ex.Message}");
                }
            }
            string outputData = "TODODODO";
            if(outputPath != null){
                
                try {
                    File.WriteAllText(outputPath.FullName, outputData);
                }
                catch (Exception ex) {
                    ErrorLogger.Report(0, $"{ex.Message}");
                }
            }
            else{
                Console.WriteLine(outputData);
            }
        }

        private static void TseitinHandler(FileInfo? inputPath, FileInfo? outputPath, bool useEquivalences)
        {
            string? inputData;
            if(inputPath != null){
                inputData = ReadFileContents(inputPath.FullName);
            }
            else{
                try {
                    using var sr = new StreamReader(Console.OpenStandardInput());
                    inputData = sr.ReadToEnd();
                }
                catch (Exception ex){
                    ErrorLogger.Report(0, $"{ex.Message}");
                }
            }
            string outputData = "TODODODODODO";
            if(outputPath != null){
                try {
                    File.WriteAllText(outputPath.FullName, outputData);
                }
                catch (Exception ex){
                    ErrorLogger.Report(0, $"{ex.Message}");
                }
            }
            else{
                Console.WriteLine(outputData);
            }
        }
    }
}