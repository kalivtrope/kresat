namespace Kresat.Representations {  
    enum Valuation {
        FALSIFIED,
        UNSATISFIED,
        SATISFIED
    }
    internal record struct Decision {
        public int DecisionLevel { get; internal set; }
        public int Literal { get; internal set; }
    }
    interface ICreateFromLiterals<TClause, TLiteral> {
        static abstract TClause Create(List<int> literals, List<TLiteral> literalData);
    }
    interface ICreateFromLiteralData<TLiteral> {
        static abstract TLiteral Create(List<TLiteral> literalData);
    }
    interface IClause<TLiteral> where TLiteral : ILiteral<TLiteral> {
        public List<int> Literals {get;set;}
        public List<TLiteral> literalData {get;set;}
        bool IsUnit();
        bool IsFalsified();
        int GetUnitLiteral();
    }
    interface ILiteral<TLiteral> where TLiteral : ILiteral<TLiteral> {
        public Valuation Value {get;}
        public List<TLiteral> literalData {get;set;}
        void Satisfy();
        void Falsify();
        void Unsatisfy();
        IEnumerable<IClause<TLiteral>> GetClauses();
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
        internal record struct Decision {
            public int DecisionLevel { get; internal set; }
            public int Literal { get; internal set; }
        }
        internal Stack<Decision> decisions = new();
        protected HashSet<int> UndecidedVars = new();
        public abstract void Backtrack(int decisionLevel);
        public abstract void DecideLiteral(int literal);
        internal int ChooseDecisionLiteral(){
            return UndecidedVars.First();
        }
        public void UndoLastLiteral(){
            Backtrack(currDecisionLevel-1);
        }
        public abstract void UnitPropagation();
        public abstract List<int> ConstructModel();
    }
    abstract class UnitPropagationDS<TLiteral, TClause> : UnitPropagationDS where TLiteral : ILiteral<TLiteral>, ICreateFromLiteralData<TLiteral>
                                                        where TClause : IClause<TLiteral>, ICreateFromLiterals<TClause, TLiteral> {
        List<TLiteral> literalData;
        List<TClause> clauseData;
        Stack<TClause> unitClauses = new();

        public sealed override void Backtrack(int decisionLevel){
            HasContradiction = false;
            unitClauses.Clear();
            while(decisions.Count > 0 && decisions.Peek().DecisionLevel > decisionLevel){
                Decision decision = decisions.Pop();
                UndoLiteral(decision.Literal);
            }
            currDecisionLevel = decisionLevel;
        }

        private void UndoLiteral(int literal){
            UndecidedVars.Add(Math.Abs(literal));
            literalData.At(literal).Unsatisfy();;
            literalData.At(-literal).Unsatisfy();
        }
        public sealed override void DecideLiteral(int literal){
            currDecisionLevel++;
            AssignLiteral(literal);
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

        private void AddClause(List<int> literals, List<TLiteral> literalData){
            TClause clause = TClause.Create(literals, literalData);
            clauseData.Add(clause);
            if(clause.IsUnit()){
                unitClauses.Push(clause);
            }
        }

        private void AssignLiteral(int literal){
            decisions.Push(new Decision { DecisionLevel = currDecisionLevel, Literal = literal });
            UndecidedVars.Remove(Math.Abs(literal));
            literalData.At(literal).Satisfy();
            literalData.At(-literal).Falsify();
            foreach(TClause clause in literalData.At(-literal).GetClauses()){
                if(clause.IsUnit()){
                    unitClauses.Push(clause);
                }
                if(clause.IsFalsified()){
                    HasContradiction = true;
                }
            }
        }

        public UnitPropagationDS(CommonRepresentation cr){
            literalData = new List<TLiteral>( new TLiteral[2*cr.LiteralCount]);
            for(int i = 0; i < literalData.Count; i++){
                literalData[i] = TLiteral.Create(literalData);
            }
            clauseData = new();
            UndecidedVars = new HashSet<int>(Enumerable.Range(1, cr.LiteralCount));
            foreach(var literals in cr.Clauses){
                AddClause(literals, literalData);
            }
        }
        public sealed override void UnitPropagation(){
            while(unitClauses.Count > 0 && !HasContradiction){
                TClause currClause = unitClauses.Pop();
                if(!currClause.IsUnit()){
                    if(currClause.IsFalsified()){
                        HasContradiction = true;
                    }
                    continue;
                }
                unitPropSteps++;
                int lit = currClause.GetUnitLiteral();
                AssignLiteral(lit);
            }      
        }
    }   
}
