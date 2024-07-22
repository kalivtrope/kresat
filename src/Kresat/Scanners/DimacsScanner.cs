using Kresat.Loggers;
using static Kresat.Scanners.DimacsTokenType;
namespace Kresat.Scanners {
    class DimacsScanner : Scanner<DimacsToken, DimacsTokenType, int>
    {
        public DimacsScanner(string source) : base(source){}
        protected override void AddEOF()
        {
            AddToken(EOF);
        }

        protected override void ScanToken()
        {
            char c = state.Advance();
            switch(c){
                case 'c': // comment
                    IgnoreUntilNewline();
                    break;
                case 'p': // header
                    ScanHeader();
                    break;
                case '-':
                    ScanLiteral();
                    break;
                case '\n':
                    state.BeginNewLine();
                    break;
                default:
                    if (IsSpace(c) || c == '%'){}
                    else if(IsDigit(c)){
                        ScanLiteral();
                    }
                    else {
                        ErrorLogger.Report(state.Line, $"Unexpected character: {c}");
                    }
                    break;
            }
        }
        private int ReadNumber(){
            while(IsDigit(state.Peek())){
                state.Advance();
            }
            string text = state.ReadLexem();
            bool success = int.TryParse(text, out int result);
            if(!success){
                ErrorLogger.Report(state.Line, $"Failed to parse literal {text}");
            }
            return result;
        }
        private string ReadString(){
            IgnoreWhiteSpace();
            while(IsAlpha(state.Peek())){
                state.Advance();
            }
            string result = state.ReadLexem();
            state.BeginNewToken();
            return result;
        }
        private void ScanLiteral()
        {
            int num = ReadNumber();
            if(num == 0){
                AddToken(CLAUSE_END);
            }
            else{
                AddToken(LITERAL, num);
            }
        }
        private void ScanHeader(){
            string format = ReadString();
            if(format != "cnf"){
                ErrorLogger.Report(state.Line, $"Invalid format: {format}");
            }
            IgnoreWhiteSpace();
            int num_vars = ReadNumber();
            AddToken(NUM_VARS, num_vars);
            IgnoreWhiteSpace();
            int num_clauses = ReadNumber();
            AddToken(NUM_CLAUSES, num_clauses);
        }
    }
}