using Kresat.Representations;

namespace Kresat.Solvers {
    class DPLLSolver {
        public int numDecisions {get;private set;} = 0;
        public int unitPropSteps {get => upds.unitPropSteps;}
        IUnitPropagationDS upds;
        CommonRepresentation cr;
        public DPLLSolver(CommonRepresentation cr, UnitPropType unitProp){
            this.cr = cr;
            if(unitProp == UnitPropType.adjacency){
                upds = new AdjacencyLists(cr);
            }
            else{
                upds = new WatchedLiterals(cr);
            }
        }
        public Verdict Solve(){
            upds.UnitPropagation();
            if(upds.HasContradiction){
                return new Verdict {Satisfiable = false};
            }
            if(upds.AllVariablesAssigned()){
                return new Verdict {Satisfiable = true, Model = upds.ConstructModel()};
            }
            numDecisions++;
            int lit = upds.ChooseDecisionLiteral();
            upds.DecideLiteral(lit);
            Verdict subresult = Solve();
            if(subresult.Satisfiable){
                return subresult;
            } 
            upds.UndoLastLiteral();
            upds.DecideLiteral(-lit);
            subresult = Solve();
            if(subresult.Satisfiable){
                return subresult;
            }
            upds.UndoLastLiteral();
            return new Verdict {Satisfiable = false};
        }
    }
}