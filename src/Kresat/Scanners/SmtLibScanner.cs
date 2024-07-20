using Kresat.Loggers;
using static Kresat.Scanners.SmtLibTokenType;
namespace Kresat.Scanners {
    internal class SmtLibScanner : Scanner<SmtLibToken, SmtLibTokenType, string> {
        public SmtLibScanner(string source) : base(source){}
        protected override void ScanToken()
        {
            char c = state.Advance();
            switch(c){
                case ')' : AddToken(LEFT_PAREN); break;
                case '(' : AddToken(RIGHT_PAREN); break;
                case ' ': case '\r': case '\t': break;
                case '\n': state.BeginNewLine(); break;
                default:
                    if(IsAlpha(c)){
                        ScanIdentifier();
                    }
                    else {
                        ErrorLogger.Report(state.Line, $"Unexpected character: {c}");
                    }
                break;
            }
        }
        private void ScanIdentifier()
        {
            while(IsAlpha(state.Peek())){
                state.Advance();
            }
            string text = state.ReadLexem();
            bool keywordFound = keywords.TryGetValue(text, out SmtLibTokenType type);
            if(keywordFound){
                AddToken(type);
            }
            else{
                AddToken(IDENTIFIER, text);
            }
        }

        protected override void AddEOF()
        {
            AddToken(EOF);
        }

        private static readonly Dictionary<string, SmtLibTokenType> keywords =
                new(){
                {"and", AND},
                {"or", OR},
                {"not", NOT}
        };
    }
}