using Kresat.Representations;
using Kresat;

namespace Kresat.Solvers {
    class CDCLSolver {
        public int numDecisions {get;private set;} = 0;
        public int unitPropSteps {get => upds.unitPropSteps;}
        IUnitPropagationDSWithLearning upds;
        CommonRepresentation cr;
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
                    int level = upds.LearnAssertiveClause(); 
                    if(level < 0){
                        return new Verdict {Satisfiable = false};
                    }
                    upds.Backtrack(level);
                    upds.AddLearnedClause();
                    upds.UnitPropagation();
                }
                if(upds.AllVariablesAssigned()){
                    return new Verdict {Satisfiable = true, Model = upds.ConstructModel()};
                }
                numDecisions++;
                int lit = upds.ChooseDecisionLiteral();
                upds.DecideLiteral(lit);
            }
        }
    }
}