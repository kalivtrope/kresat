using Kresat.Solvers;

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
    interface ILearning<TClause> where TClause : class {
        TClause? Antecedent {get;set;}
        int DecisionLevel {get;set;}
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
    interface IPurgeable {
        public bool IsDeleted {get;set;}
    }
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

    interface IUnitPropagationDSWithLearning : IUnitPropagationDS {
        public int LearnAssertiveClause();
        public void AddLearnedClause();
        public void Restart();
    }
    abstract class UnitPropagationDSWithLearning<TLiteral, TClause> : UnitPropagationDS<TLiteral, TClause>, IUnitPropagationDSWithLearning
                                                        where TLiteral : ILiteral<TLiteral>, ILearning<TClause>, new()
                                                        where TClause : class, IClause<TLiteral>, IPurgeable, ICreateFromLiterals<TClause, TLiteral>
    {
        protected UnitPropagationDSWithLearning(CommonRepresentation cr, ResetDeletionConfiguration config) : base(cr){
            used = new bool[cr.LiteralCount+1];
            cache = new Cache(multiplier: config.Multiplier, initialCacheSize: config.CacheSize);
        }
        Cache cache;
        bool[] used;
        TClause? conflict;
        List<TLiteral>? clauseToBeLearned;
        class Cache {
            record class TLearnedClause {
                public TClause Clause {get;init;}
                public int LBD {get;init;}
            }
            double multiplier = 1.5;
            long currMaxSize = 10000;
            List<TLearnedClause> learnedClauses = new();
            public void Add(TClause clause){
                if(learnedClauses.Count >= currMaxSize){
                    learnedClauses.Sort((a,b) => a.LBD.CompareTo(b.LBD));
                    while(learnedClauses.Count * 2 > currMaxSize){
                        TClause clauseToBeRemoved = learnedClauses[^1].Clause;
                        clauseToBeRemoved.IsDeleted = true;
                        learnedClauses.RemoveAt(learnedClauses.Count-1);
                    }
                    currMaxSize = (int)(currMaxSize * multiplier);
                }
                int LBD = CalculateLBD(clause);
                learnedClauses.Add(new TLearnedClause{ Clause = clause, LBD = LBD });
            }
            int CalculateLBD(TClause clause){
                HashSet<int> levels = new();
                for(int i = 0; i < clause.Literals.Count; i++){
                    levels.Add(clause.Literals[i].DecisionLevel);
                }
                return levels.Count;
            }
        }
        Cache cache = new();
        public void Restart(){
            Backtrack(0);
        }
        public int LearnAssertiveClause(){
            if(currDecisionLevel == 0) return -1;

            int numLiteralsAtCurrDecisionLevel = 0;
            List<TLiteral> result = new();
            int assertionLevel = 0;
            //Console.WriteLine($"starting with {used.Contains(true)}");
            for(int j = 0; j < conflict!.Literals!.Count; j++){
                used[Math.Abs(conflict!.Literals[j].LitNum)] = true;
                // In a conflict, all of the literals are falsified.
                // Since we only record the satisfied literals, we need
                // to actually observe the decision levels of the opposite
                // literals in the falsified clause.
                if(conflict!.Literals[j].Opposite.DecisionLevel == currDecisionLevel){
                    // Once there's only a single literal at the current decision
                    // level, we stop (this is a 1-UIP)
                    numLiteralsAtCurrDecisionLevel++;
                }
                else{
                  result.Add(conflict!.Literals[j]);
                  assertionLevel = Math.Max(assertionLevel, conflict!.Literals[j].Opposite.DecisionLevel);
                }
            }
            for(int i = decisions.Count-1; i >= 0; i--){
                TLiteral currLit = decisions[i].Literal;
                if(!used[Math.Abs(currLit.LitNum)]){
                    continue;
                }
                if(currLit.DecisionLevel < currDecisionLevel){
                    throw new Exception("shouldn't have gone beyond current decision level");
                }
                if(currLit.DecisionLevel == currDecisionLevel){
                    if(numLiteralsAtCurrDecisionLevel == 1){
                        result.Add(currLit.Opposite);
                        break;
                    }
                    numLiteralsAtCurrDecisionLevel--;
                    for(int j = 0; j < currLit.Antecedent!.Literals.Count; j++){
                        // simulate resolution by simply marking the literals
                        // in the antecedent as present in our newly built clause
                        // (represented by the used HashSet)
                        TLiteral nextLit = currLit.Antecedent.Literals[j];
                        int litNum = Math.Abs(nextLit.LitNum);
                        if(!used[litNum]){
                            used[litNum] = true;
                            if(nextLit.Opposite.DecisionLevel == currDecisionLevel){
                                numLiteralsAtCurrDecisionLevel++;
                            }
                            else{
                              result.Add(nextLit);
                              assertionLevel = Math.Max(assertionLevel, nextLit.Opposite.DecisionLevel);
                            }
                        }
                    }
                }
            }
            clauseToBeLearned = result;
            Array.Clear(used);
            return assertionLevel;
        }
        public void AddLearnedClause(){
            TClause clause = AddClause(clauseToBeLearned!, addToClauseData: false);
            cache.Add(clause);
        }

        protected sealed override void RegisterDecision(TLiteral literal, TClause? antecedent){
            decisions.Add(new Decision<TLiteral> { DecisionLevel = currDecisionLevel, Literal = literal });
            literal.DecisionLevel = currDecisionLevel;
            literal.Antecedent = antecedent;
        }
        protected sealed override void RegisterContradiction(TClause clause){
            HasContradiction = true;
            conflict = clause;
        }
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
}
