using Kresat.Loggers;
using Kresat.Representations;
using Kresat.Scanners;
using static Kresat.Scanners.SmtLibTokenType;
namespace Kresat.Parsers {
    class SmtLibParser : IParser {
        bool debug = false;
        List<SmtLibToken> tokens;
        Dictionary<string, int> idMap = [];
        List<string?> intToId = [null];
        Dictionary<int,string> gateExpr = [];
        int ID = 1;
        int origVarNum;
        bool useEquivalences;
        public CommonRepresentationBuilder cr {get; private set;} = new();
        public SmtLibParser(IEnumerable<SmtLibToken> tokens, bool useEquivalences)
        {
            this.useEquivalences = useEquivalences;
            SortedSet<string> hs = new();
            this.tokens = tokens.ToList();
            foreach(var token in tokens){
                if(token.Type == IDENTIFIER){
                    hs.Add(token.Payload!);
                }
            }
            foreach(var identifier in hs){
                intToId.Add(identifier);
                idMap[identifier] = ID++;
                //Console.WriteLine(identifier);
            }
            origVarNum = ID;
        }
        int currIdx = -1;
        SmtLibToken Advance(){
            currIdx++;
            return Peek();
        }
        SmtLibToken Peek(){
            if(currIdx >= tokens.Count){
                throw new IndexOutOfRangeException();
            }
            return tokens[currIdx];
        }
        int NewFormulaId(){
            int fid = ID++;
            string humanReadableId = $"_{fid-origVarNum}"; 
            idMap[humanReadableId] = fid;
            intToId.Add(humanReadableId);
            if(intToId.Count != ID){
                throw new ArgumentException($"actual {intToId.Count} vs expected {ID}");
            }
            return fid;
        }
        void CheckTokenType(SmtLibToken token, SmtLibTokenType expected){
            if(token.Type != expected){
                Console.WriteLine($"Unexpected token: {tokens} (expected {expected})");
            }
        }
        void Debug(int levelOfIndent, SmtLibToken token, int id = 0){
            if(debug){
                Console.WriteLine($"{new string(' ', levelOfIndent)}{token} {intToId[id]}");    
            }
        }
        void Debug(int levelOfIndent, SmtLibToken token1, SmtLibToken token2){
            if(debug){
                Console.WriteLine($"{new string(' ', levelOfIndent)}{token1} {token2}");
            }
        }
        public int ParseFormula(int levelOfIndent = 0){
            SmtLibToken curr = Advance();
            switch(curr.Type){
                case LEFT_PAREN:
                    SmtLibToken op = Advance();
                    switch(op.Type){
                        case AND:
                        case OR:
                            int fid = NewFormulaId();
                            Debug(levelOfIndent, op, fid);
                            int leftRoot = ParseFormula(levelOfIndent+1);
                            int rightRoot = ParseFormula(levelOfIndent+1);
                            gateExpr[GateId(fid)] = $"{(Math.Sign(leftRoot) == -1 ? '-' : null)}{intToId[Math.Abs(leftRoot)]} {op.Type} {(Math.Sign(rightRoot) == -1 ? '-' : null)}{intToId[Math.Abs(rightRoot)]}";
                            AddClause(op.Type, fid, leftRoot, rightRoot);
                            SmtLibToken rpar = Advance();
                            CheckTokenType(rpar, RIGHT_PAREN);
                            return fid;
                        case NOT:
                            SmtLibToken operand = Advance();
                            CheckTokenType(operand, IDENTIFIER);
                            Debug(levelOfIndent, op, operand);
                            int id = idMap[operand.Payload!];
                            rpar = Advance();
                            CheckTokenType(rpar, RIGHT_PAREN);
                            return -id;
                    }
                    break;
                case IDENTIFIER:
                    Debug(levelOfIndent, curr);
                    return idMap[curr.Payload!];
                default:
                    ErrorLogger.Report($"Unexpected token: {curr} at idx {currIdx} (expected either IDENTIFIER or LEFT_PAREN)");
                    break;
            }
            return 0;
        }

        private void AddClause(SmtLibTokenType type, int a, int b, int c)
        {
            if(type == OR){
                // a ≡ b v c
                // => (-a) v b v c
                // <= (a v (-b)) ^ (a v (-c))
                cr.AddClause([-a, b, c]);
                if(useEquivalences){
                    cr.AddClause([a,-b]);
                    cr.AddClause([a,-c]);
                }
            }
            else if(type == AND){
                // a ≡ b ^ c
                // => (-a v b) ^ (-a v c)
                // <= a v -b v -c
                cr.AddClause([-a,b]);
                cr.AddClause([-a,c]);
                if(useEquivalences){
                    cr.AddClause([a,-b,-c]);
                }
            }
            else{
                throw new ArgumentException(nameof(type));
            }                            
        }

        bool IsGate(int id){
            return Math.Abs(id) >= origVarNum;
        }
        int GateId(int id){
            return Math.Abs(id) - origVarNum;
        }

        public CommonRepresentation ToCommonRepresentation(){
            int rootId = ParseFormula();
            cr.AddClause([rootId]);
            for(int i = 1; i < intToId.Count; i++){
                cr.AddComment($"{i} = {intToId[i]}{(IsGate(i) ? " ≡ " + gateExpr[GateId(i)] : null)}{(i == Math.Abs(rootId) ? " (root)" : null)}");
            }
            List<string?> Mapping = new();
            for(int i = 0; i < ID; i++){
                Mapping.Add(IsGate(i) ? null : intToId[i]);
            }
            cr.OriginalMapping = Mapping;
            return cr.Build();
        }
    }
}