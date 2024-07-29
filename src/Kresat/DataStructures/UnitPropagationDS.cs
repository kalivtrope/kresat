namespace Kresat.Representations {  
    enum Valuation {
        FALSIFIED,
        UNSATISFIED,
        SATISFIED
    }
    internal record struct Decision<TLiteral> {
        public int DecisionLevel { get; internal set; }
        public TLiteral Literal { get; internal set; }
    }
    interface ICreateFromLiterals<TClause, TLiteral> {
        static abstract TClause Create(List<TLiteral> _literals);
    }
    interface IClause<TLiteral> where TLiteral : ILiteral<TLiteral> {
        public List<TLiteral> Literals {get;}
        bool IsUnit();
        bool IsFalsified();
        bool IsSatisfied();
        TLiteral GetUnitLiteral();
    }
    interface ILiteral<TLiteral> where TLiteral : ILiteral<TLiteral> {
        public Valuation Value {get;}
        public TLiteral Opposite {get;set;}
        public int LitNum {get;set;}
        public void AssignLitNum(int LitNum){
            this.LitNum = LitNum;
        }
        public void SetOther(TLiteral Opposite){
            this.Opposite = Opposite;
        }
        void Satisfy();
        void Falsify();
        void Unsatisfy();
        IReadOnlyList<IClause<TLiteral>> GetClauses();
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
    abstract class UnitPropagationDS{
        public bool HasContradiction = false;
        public int currDecisionLevel = 0;
        public int unitPropSteps = 0;

        protected HashSet<int> UndecidedVars = new();
        public abstract void Backtrack(int decisionLevel);
        public abstract void DecideLiteral(int literal);
        internal int ChooseDecisionLiteral(){
            return UndecidedVars.First();
        }
        public void UndoLastLiteral(){
            Backtrack(currDecisionLevel-1);
        }
        public abstract bool AllVariablesAssigned();
        public abstract void UnitPropagation();
        public abstract List<int> ConstructModel();
    }
    abstract class UnitPropagationDS<TLiteral, TClause> : UnitPropagationDS where TLiteral : ILiteral<TLiteral>, new()
                                                        where TClause : IClause<TLiteral>, ICreateFromLiterals<TClause, TLiteral> {
        List<TLiteral> literalData;
        List<TClause> clauseData;
        Stack<TClause> unitClauses = new();
    
        Stack<Decision<TLiteral>> decisions = new();

        public sealed override bool AllVariablesAssigned()
        {
            if(decisions.Count > literalData.Count / 2){
                throw new Exception("corrupted stack");
            }
            return decisions.Count == literalData.Count / 2;
        }
        public sealed override void Backtrack(int decisionLevel){
            HasContradiction = false;
            unitClauses.Clear();
            while(decisions.Count > 0 && decisions.Peek().DecisionLevel > decisionLevel){
                Decision<TLiteral> decision = decisions.Pop();
                UndoLiteral(decision.Literal);
            }
            currDecisionLevel = decisionLevel;
        }

        private void UndoLiteral(TLiteral literal){
            UndecidedVars.Add(Math.Abs(literal.LitNum));
            literal.Unsatisfy();
            literal.Opposite.Unsatisfy();
        }
        public sealed override void DecideLiteral(int literal){
            currDecisionLevel++;
            AssignLiteral(literalData.At(literal));
        }

        public sealed override List<int> ConstructModel(){
            List<int> res = new List<int>();
            for(int _var = 1; _var <= literalData.Count / 2; _var++){
                if(literalData.At(_var).Value == Valuation.UNSATISFIED){
                    throw new ArgumentException();
                }
                res.Add(literalData.At(_var).Value == Valuation.SATISFIED ? _var : -_var);
            }
            return res;
        }

        private void AddClause(List<int> literals){
            List<TLiteral> _literals = literals.Select(x => literalData.At(x)).ToList();
            TClause clause = TClause.Create(_literals);
            clauseData.Add(clause);
            if(clause.IsUnit()){
                unitClauses.Push(clause);
            }
        }

        private void AssignLiteral(TLiteral literal){
            decisions.Push(new Decision<TLiteral> { DecisionLevel = currDecisionLevel, Literal = literal });
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
                    HasContradiction = true;
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
        public sealed override void UnitPropagation(){
            while(unitClauses.Count > 0 && !HasContradiction){
                //Console.WriteLine(string.Join(' ', unitClauses.ToList().Select(c=>clauseData.IndexOf(c))));
                TClause currClause = unitClauses.Pop();
                if(!currClause.IsUnit()){
                    if(currClause.IsFalsified()){
                        HasContradiction = true;
                    }
                    continue;
                }
                unitPropSteps++;
                TLiteral lit = currClause.GetUnitLiteral();
                AssignLiteral(lit);
            }      
        }
    }   
}
