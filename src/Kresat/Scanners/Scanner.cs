namespace Kresat.Scanners{
    abstract class Scanner
    {
        protected ScannerState state;

        protected bool IsAlphaNumeric(char c)
        {
            return IsAlpha(c) || IsDigit(c);
        }
        protected bool IsDigit(char c){
            return c >= '0' && c <= '9';
        }
        protected bool IsAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z');
        }
        public Scanner(string source){
            state = new(source);
        }
    }

    class ScannerState {
        public int StartIdx {get; private set;} = 0;
        public int CurrIdx {get; private set;} = 0;
        public int Line {get;private set;} = 1;
        private string source;
        public ScannerState(string source){
            this.source = source;
        }
        public void BeginNewToken(){
            StartIdx = CurrIdx;
        }
        public void BeginNewLine(){
            Line++;
        }
        public string ReadLexem(){
            return source[CurrIdx..StartIdx];
        }
        public char Peek(){
            if(IsAtEnd()){
                return '\0';
            }
            return source[CurrIdx];
        }
        public char Advance(){
            return source[CurrIdx++];
        }
        public bool IsAtEnd(){
            return CurrIdx >= source.Length;
        }
    }
}