using Kresat.Solvers;

namespace Kresat.Representations {
    interface ILearning<TClause> where TClause : class {
        TClause? Antecedent {get;set;}
        int DecisionLevel {get;set;}
    }
    interface IPurgeable<TLiteral> {
        bool IsDeleted {get;}
        IEnumerable<TLiteral> DeleteSelf();
    }
    interface IUnitPropagationDSWithLearning : IUnitPropagationDS {
        public int LearnAssertiveClause();
        public void AddLearnedClause();
        public void Restart();
    }
    abstract class UnitPropagationDSWithLearning<TLiteral, TClause> : UnitPropagationDS<TLiteral, TClause>, IUnitPropagationDSWithLearning
                                                        where TLiteral : ILiteral<TLiteral>, ILearning<TClause>, new()
                                                        where TClause : class, IClause<TLiteral>, IPurgeable<TLiteral>, ICreateFromLiterals<TClause, TLiteral>
    {
        protected UnitPropagationDSWithLearning(CommonRepresentation cr, ResetDeletionConfiguration config) : base(cr){
            used = new bool[cr.LiteralCount+1];
            cache = new Cache(multiplier: config.Multiplier, initialCacheSize: config.CacheSize, this);
        }
        Cache cache;
        bool[] used;
        TClause? conflict;
        List<TLiteral>? clauseToBeLearned;
        public abstract void PurgeDeletedClausesOfLiterals(HashSet<TLiteral> literals);
        class Cache {
            record class TLearnedClause {
                public TClause Clause {get;init;}
                public int LBD {get;init;}
            }
            double multiplier;
            long currMaxSize;
            UnitPropagationDSWithLearning<TLiteral, TClause> parent;
            public Cache(double multiplier, long initialCacheSize, UnitPropagationDSWithLearning<TLiteral, TClause> parent){
                this.multiplier = multiplier;
                currMaxSize = initialCacheSize;
                this.parent = parent;
            }
            List<TLearnedClause> learnedClauses = new();
            public void Add(TClause clause){
                if(learnedClauses.Count >= currMaxSize){
                    learnedClauses.Sort((a,b) => a.LBD.CompareTo(b.LBD));
                    HashSet<TLiteral> affectedLiterals = new();
                    while(learnedClauses.Count * 2 > currMaxSize){
                        TClause clauseToBeRemoved = learnedClauses[^1].Clause;
                        IEnumerable<TLiteral> currAffectedLiterals = clauseToBeRemoved.DeleteSelf();
                        affectedLiterals.UnionWith(currAffectedLiterals);
                        learnedClauses.RemoveAt(learnedClauses.Count-1);
                    }
                    currMaxSize = (int)(currMaxSize * multiplier);
                    parent.PurgeDeletedClausesOfLiterals(affectedLiterals);
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
        public void Restart(){
            Backtrack(0);
        }
        public int LearnAssertiveClause(){
            if(currDecisionLevel == 0) return -1;

            int numLiteralsAtCurrDecisionLevel = 0;
            List<TLiteral> result = new();
            int assertionLevel = 0;
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
}