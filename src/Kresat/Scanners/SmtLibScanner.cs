using Kresat.Loggers;
namespace Kresat.Scanners {
    internal class SmtLibScanner(string source) : Scanner(source) {
        List<Token> tokens = [];
        internal enum TokenType
        {
            LEFT_PAREN, RIGHT_PAREN,
            OR, AND, NOT,
            IDENTIFIER,
            EOF
        }
        internal struct Token(TokenType Type, string? Identifier) {
            public override string ToString()
            {
                return $"{Type} {Identifier}";
            }
        }

        void AddToken(TokenType type){
            AddToken(type, null);
        }
        void AddToken(TokenType type, string? identifier){
            tokens.Add(new Token(type, identifier));
        }
        public List<Token> ScanTokens(){
            while(!state.IsAtEnd()){
                state.BeginNewToken();
                ScanToken();
            }
            AddToken(TokenType.EOF);
            return tokens;
        }

        private void ScanToken()
        {
            char c = state.Advance();
            switch(c){
                case ')' : AddToken(TokenType.LEFT_PAREN); break;
                case '(' : AddToken(TokenType.RIGHT_PAREN); break;
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
            bool keywordFound = keywords.TryGetValue(text, out TokenType type);
            if(keywordFound){
                AddToken(type);
            }
            else{
                AddToken(TokenType.IDENTIFIER, text);
            }
        }

        private static readonly Dictionary<string, TokenType> keywords =
                new Dictionary<string, TokenType>(){
                {"and", TokenType.AND},
                {"or", TokenType.OR},
                {"not", TokenType.NOT}
        };
    }
}