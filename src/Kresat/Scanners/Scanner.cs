namespace Kresat.Scanners{
    abstract class Scanner<TToken, TTokenType, TPayload>
    where TToken : IToken<TToken, TTokenType, TPayload> 
    {
        private List<TToken> tokens = [];
        protected ScannerState state;

        protected void AddToken(TTokenType type){
            tokens.Add(TToken.Create(type));
        }
        protected void AddToken(TTokenType type, TPayload identifier){
            tokens.Add(TToken.Create(type, identifier));
        }
        protected abstract void AddEOF();
        protected abstract void ScanToken();
        public virtual IEnumerable<TToken> ScanTokens(){
            while(!state.IsAtEnd()){
                state.BeginNewToken();
                ScanToken();
            }
            AddEOF();
            return tokens;
        }
        protected void IgnoreWhiteSpace(){
            while(IsSpace(state.Peek()))
                state.Advance();
            state.BeginNewToken();
        }
        protected void IgnoreUntilNewline()
        {
            while(state.Peek() != '\n' && !state.IsAtEnd()){
                state.Advance();
            }
        }
        protected bool IsSpace(char c){
            return c == ' ' || c == '\t' || c == '\r';
        }
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
            return source[StartIdx..CurrIdx];
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