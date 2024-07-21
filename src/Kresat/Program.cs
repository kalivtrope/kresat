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

            await rootCommand.InvokeAsync(args);
            return ErrorLogger.HadError ? 1 : 0;
        }

        private static void SolveHandler(string format, FileInfo? inputPath, FileInfo? outputPath)
        {
            if(format != "smtlib" && format != "dimacs"){
                Console.Error.WriteLine("Invalid format. Valid values are 'smtlib' or 'dimacs'");
                return;
            }
            string? inputData = ReadFile(inputPath);
            if(!ErrorLogger.HadError){

            }
            string outputData = "TODODODO";
            WriteFile(outputPath, outputData);
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
                Console.WriteLine(data);
            }
        }

        private static void TseitinHandler(FileInfo? inputPath, FileInfo? outputPath, bool useEquivalences)
        {
            string? inputData = ReadFile(inputPath);
            string outputData = "";
            if(!ErrorLogger.HadError){
                SmtLibScanner scanner = new(inputData!);
                foreach(var token in scanner.ScanTokens()){
                    outputData += $"{token} ";
                }
            WriteFile(outputPath, outputData);
            }
        }
    }
}