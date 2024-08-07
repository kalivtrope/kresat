using Kresat.Representations;
using Kresat;

namespace Kresat.Solvers {
    class CDCLSolver {
        public int numDecisions {get;private set;} = 0;
        public int unitPropSteps {get => upds.unitPropSteps;}
        public int numRestarts {get; private set;} = 0;
        public int totalNumConflicts {get; private set;} = 0;
        int numConflicts = 0;
        IUnitPropagationDSWithLearning upds;
        CommonRepresentation cr;
        LubyGenerator lubyGenerator = new();
        public CDCLSolver(CommonRepresentation cr, UnitPropType unitProp){
            this.cr = cr;
            if(unitProp == UnitPropType.adjacency){
                upds = new AdjacencyListsWithLearning(cr);
            }
            else{
                upds = new WatchedLiteralsWithLearning(cr);
            }
        }
        public Verdict Solve(){
            while(true){
                upds.UnitPropagation();
                while(upds.HasContradiction){
                    totalNumConflicts++;
                    int level = upds.LearnAssertiveClause(); 
                    if(level < 0){
                        return new Verdict {Satisfiable = false};
                    }
                    if(lubyGenerator.TimeToRestart(numConflicts++)){
                        numConflicts = 0;
                        numRestarts++;
                        upds.Restart();
                    }
                    else {
                        upds.Backtrack(level);
                        upds.AddLearnedClause();
                        upds.UnitPropagation();
                    }
                }
                if(upds.AllVariablesAssigned()){
                    return new Verdict {Satisfiable = true, Model = upds.ConstructModel()};
                }
                numDecisions++;
                int lit = upds.ChooseDecisionLiteral();
                upds.DecideLiteral(lit);
            }
        }
        class LubyGenerator {
            // see https://oeis.org/A182105 on details about why this works
            long u = 1, v = 1;
            public bool TimeToRestart(int numConflicts){
                if(numConflicts >= v){
                    Advance();
                    return true;
                }
                return false;
            }
            void Advance(){
                if((u & (-u)) == v){
                    u++;
                    v = 1;
                }
                else{
                    v <<= 1;
                }
            }
        }
    }
}