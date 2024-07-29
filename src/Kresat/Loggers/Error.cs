namespace Kresat.Loggers {
    public static class ErrorLogger {
        public static bool HadError = false;
        public static bool HadWarning = false;
        public static void Report(int line, string message){
            HadError = true;
            Console.Error.WriteLine($"[line {line}] Error: {message}");
        }
        public static void ReportWarning(string message){
            HadWarning = true;
            Console.Error.WriteLine($"Warning: {message}");
        }
    }
}