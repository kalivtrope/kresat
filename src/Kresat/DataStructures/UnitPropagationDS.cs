namespace Kresat.Representations {  
    interface IUnitPropagationDS {
        public bool HasContradiction {get;}
        public int currDecisionLevel {get;}
        public int unitPropSteps {get;}

        protected HashSet<int> UndecidedVars {get;}
        public void Backtrack(int decisionLevel);
        public void DecideLiteral(int literal);
        internal int ChooseDecisionLiteral(){
            return UndecidedVars.First();
        }
        public void UndoLastLiteral(){
            Backtrack(currDecisionLevel-1);
        }
        public bool AllVariablesAssigned();
        public void UnitPropagation();
        public List<int> ConstructModel();
    }
    abstract class UnitPropagationDS<TLiteral, TClause> : IUnitPropagationDS where TLiteral : ILiteral<TLiteral>, new()
                                                        where TClause : class, IClause<TLiteral>, ICreateFromLiterals<TClause, TLiteral> {
        protected List<TLiteral> literalData;
        protected List<TClause> clauseData;
        protected Stack<TClause> unitClauses = new();
        protected List<Decision<TLiteral>> decisions = new();
        public bool HasContradiction {get;protected set;} = false;
        public int currDecisionLevel {get;protected set;} = 0;
        public int unitPropSteps {get;private set;} = 0;
        public HashSet<int> UndecidedVars {get; private set;} = new();

        public bool AllVariablesAssigned()
        {
            if(decisions.Count > literalData.Count / 2){
                throw new Exception("corrupted stack");
            }
            return decisions.Count == literalData.Count / 2;
        }
        public void Backtrack(int decisionLevel){
            HasContradiction = false;
            unitClauses.Clear();
            while(decisions.Count > 0 && decisions[^1].DecisionLevel > decisionLevel){
                Decision<TLiteral> decision = decisions[^1];
                decisions.RemoveAt(decisions.Count-1);
                UndoLiteral(decision.Literal);
            }
            currDecisionLevel = decisionLevel;
        }

        protected void UndoLiteral(TLiteral literal){
            UndecidedVars.Add(Math.Abs(literal.LitNum));
            literal.Unsatisfy();
            literal.Opposite.Unsatisfy();
        }
        public void DecideLiteral(int literal){
            currDecisionLevel++;
            AssignLiteral(literalData.At(literal), null);
        }

        public List<int> ConstructModel(){
            List<int> res = new List<int>();
            for(int _var = 1; _var <= literalData.Count / 2; _var++){
                if(literalData.At(_var).Value == Valuation.UNSATISFIED){
                    throw new ArgumentException();
                }
                res.Add(literalData.At(_var).Value == Valuation.SATISFIED ? _var : -_var);
            }
            return res;
        }
        protected TClause AddClause(List<TLiteral> literals, bool addToClauseData=true){
            TClause clause = TClause.Create(literals);
            if(addToClauseData){
                clauseData.Add(clause);
            }
            if(clause.IsUnit()){
                unitClauses.Push(clause);
            }
            return clause;
        }
        private void AddClause(List<int> literals){
            List<TLiteral> _literals = literals.Select(x => literalData.At(x)).ToList();
            AddClause(_literals);
        }
        protected virtual void RegisterDecision(TLiteral literal, TClause? antecedent){
            decisions.Add(new Decision<TLiteral> { DecisionLevel = currDecisionLevel, Literal = literal });
        }
        protected virtual void RegisterContradiction(TClause clause){
            HasContradiction = true;
        }
        private void AssignLiteral(TLiteral literal, TClause? antecedent){
            RegisterDecision(literal, antecedent);
            UndecidedVars.Remove(Math.Abs(literal.LitNum));
            literal.Satisfy();
            literal.Opposite.Falsify();
            var clauses = literal.Opposite.GetClauses();
            for(int i = 0; i < clauses.Count; i++){
                if(clauses[i].IsSatisfied()){
                    continue;
                }
                if(clauses[i].IsUnit()){
                    unitClauses.Push((TClause)clauses[i]);
                }
                if(clauses[i].IsFalsified()){
                    RegisterContradiction((TClause)clauses[i]);
                    break;
                }
            }
        }

        public UnitPropagationDS(CommonRepresentation cr){
            literalData = new List<TLiteral>( new TLiteral[2*cr.LiteralCount]);
            for(int i = 0; i < literalData.Count; i++){
                literalData[i] = new();
            }
            for(int i = 1; i <= cr.LiteralCount; i++){
                literalData.At(i).AssignLitNum(i);
                literalData.At(i).SetOther(literalData.At(-i));
                literalData.At(-i).AssignLitNum(-i);
                literalData.At(-i).SetOther(literalData.At(i));
            }
            clauseData = new();
            UndecidedVars = new HashSet<int>(Enumerable.Range(1, cr.LiteralCount));
            foreach(var literals in cr.Clauses){
                AddClause(literals);
            }
        }
        public void UnitPropagation(){
            while(unitClauses.Count > 0 && !HasContradiction){
                TClause currClause = unitClauses.Pop();
                if(!currClause.IsUnit()){
                    if(currClause.IsFalsified()){
                        RegisterContradiction(currClause);
                    }
                    continue;
                }
                unitPropSteps++;
                TLiteral lit = currClause.GetUnitLiteral();
                AssignLiteral(lit, currClause);
            }
        }
    }
    internal static class ListExtensions {
        public static void Swap<T>(this IList<T> list, int idx1, int idx2){
            if(idx1 == idx2) return;
            T tmp = list[idx1];
            list[idx1] = list[idx2];
            list[idx2] = tmp;
        }
        public static void RemoveInPlace<T>(this IList<T> list, int idx){
            list.Swap(idx, list.Count-1);
            list.RemoveAt(list.Count-1);
        }
        public static TLiteral At<TLiteral>(this IList<TLiteral> list, int lit){
            if (lit < 0) {
                return list[-2*(lit + 1)];
            }
            return list[2*(lit - 1) + 1];
        }
    }
}
