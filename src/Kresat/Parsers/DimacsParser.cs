using Kresat.Representations;
using Kresat.Scanners;
using static Kresat.Scanners.DimacsTokenType;
namespace Kresat.Parsers {
    class DimacsParser : IParser {
        public CommonRepresentationBuilder cr {get; private set;} = new();
        IEnumerable<DimacsToken> tokens;
        public DimacsParser(IEnumerable<DimacsToken> tokens){
            this.tokens = tokens;
        }
        public CommonRepresentation ToCommonRepresentation()
        {
            foreach(var token in tokens){
                switch(token.Type){
                    case NUM_CLAUSES:
                        cr.SetExpectedClauseNum(token.Payload);
                        break;
                    case NUM_VARS:
                        cr.SetExpectedVarNum(token.Payload);
                        break;
                    case LITERAL:
                        cr.AddLiteral(token.Payload);
                        break;
                    case CLAUSE_END:
                    case EOF:
                        cr.EndClause();
                        break;
                }
            }
            return cr.Build();
        }
    }
}