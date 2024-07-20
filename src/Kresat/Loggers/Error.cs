namespace Kresat.Loggers {
    public static class ErrorLogger {
        public static bool HadError = false;
        public static void Report(int line, string message){
            HadError = true;
            Console.Error.WriteLine($"[line {line}] Error: {message}");
        }
    }

}