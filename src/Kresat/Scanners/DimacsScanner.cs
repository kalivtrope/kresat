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
            throw new NotImplementedException();
        }
    }
}