using Kresat.Parsers;
using Kresat.Scanners;

namespace Kresat {
    public class Program {
        public static void ReadFile(string path){
            StreamReader sr = new StreamReader(path);
            var output = new DimacsScanner(sr.ReadToEnd()).ScanTokens();
            foreach(var token in output){
                Console.Write($"{token} ");
            }
        }
        public static void Main(string[] args){
            if(args.Length != 1){
                Console.WriteLine("Usage: kresat [input]");
                Environment.Exit(64);
            }
            ReadFile(args[0]);
        }
    }
}