namespace Kresat.Scanners {
    class DimacsScanner : Scanner
    {
        public DimacsScanner(string source) : base(source){}

        internal enum TokenType
        {
            HEAD, LITERAL, CLAUSE_END, EOF
        }

        internal record struct Token(TokenType type, int identifier) {}
        public List<Token> ScanTokens(){
            throw new NotImplementedException();
        }
    }
}