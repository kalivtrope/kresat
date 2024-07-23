using Kresat.Representations;

namespace Kresat.Solvers {
    class DPLLSolver {
        int currDL = 0;
        AdjacencyLists AL;
        void UnitPropagation(){

        }
        CommonRepresentation cr;
        public DPLLSolver(CommonRepresentation cr){
            this.cr = cr;
            AL = new(cr);
        }
        public Verdict Solve(){
            AL.UnitResolution();
            if(AL.numConflictingClauses > 0){
                return new Verdict {Satisfiable = false};
            }
            if(AL.decisions.Count == cr.LiteralCount){
                return new Verdict {Satisfiable = true, Model = AL.ConstructModel()};
            }
            int lit = AL.ChooseDecisionLiteral();
            AL.DecideLiteral(lit);
            Verdict subresult = Solve();
            if(subresult.Satisfiable){
                return subresult;
            } 
            AL.UndoLastLiteral();
            AL.DecideLiteral(-lit);
            subresult = Solve();
            if(subresult.Satisfiable){
                return subresult;
            }
            AL.UndoLastLiteral();
            return new Verdict {Satisfiable = false};
        }
}