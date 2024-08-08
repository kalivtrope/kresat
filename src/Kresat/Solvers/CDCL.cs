using Kresat.Representations;

namespace Kresat.Solvers {

    internal class ResetDeletionConfiguration
    {
        public double Multiplier { get; set; }
        public int UnitRun { get; set; }
        public long CacheSize { get; set; }
    }
    class CDCLSolver : ISolver {
        public int numDecisions {get;private set;} = 0;
        public int unitPropSteps {get => upds.unitPropSteps;}
        public int numRestarts {get; private set;} = 0;
        public int totalNumConflicts {get; private set;} = 0;
        int numConflicts = 0;
        IUnitPropagationDSWithLearning upds;
        LubyGenerator lubyGenerator;
        public CDCLSolver(CommonRepresentation cr, UnitPropType unitProp, ResetDeletionConfiguration config){
            lubyGenerator = new LubyGenerator(unitRun: config.UnitRun);
            if(unitProp == UnitPropType.adjacency){
                upds = new AdjacencyListsWithLearning(cr, config);
            }
            else{
                upds = new WatchedLiteralsWithLearning(cr, config);
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
            long unitRun;
            public LubyGenerator(long unitRun){
                this.unitRun = unitRun;
            }
            public bool TimeToRestart(int numConflicts){
                if(numConflicts >= unitRun * v){
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