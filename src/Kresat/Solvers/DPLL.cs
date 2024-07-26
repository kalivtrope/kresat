using Kresat.Representations;

namespace Kresat.Solvers {
    class DPLLSolver {
        public int numDecisions {get;private set;} = 0;
        //AdjacencyLists AL;
        WatchedLiterals WL;
        CommonRepresentation cr;
        public DPLLSolver(CommonRepresentation cr){
            this.cr = cr;
            //AL = new(cr);
            WL = new(cr);
        }
        public Verdict Solve(){
            //Console.WriteLine($"dl: {AL.currDecisionLevel}");
            WL.UnitPropagation();
            if(WL.HasContradiction){
                return new Verdict {Satisfiable = false};
            }
            if(WL.decisions.Count == cr.LiteralCount){
                return new Verdict {Satisfiable = true, Model = WL.ConstructModel()};
            }
            if(WL.decisions.Count > cr.LiteralCount){
                throw new ArgumentException("corrupted stack :/");
            }
            numDecisions++;
            int lit = WL.ChooseDecisionLiteral();
            //Console.WriteLine($"deciding {lit}");
            WL.DecideLiteral(lit);
            Verdict subresult = Solve();
            if(subresult.Satisfiable){
                return subresult;
            } 
            WL.UndoLastLiteral();
            //Console.WriteLine($"deciding {-lit}");
            WL.DecideLiteral(-lit);
            subresult = Solve();
            if(subresult.Satisfiable){
                return subresult;
            }
            WL.UndoLastLiteral();
            return new Verdict {Satisfiable = false};
            /*while(BCP()){
                int backtrackLevel = AnalyzeConflict();
                if(backtrackLevel < 0){
                    return new Verdict{ Satisfiable = false };
                }
                Backtrack(backtrackLevel);
            }
            if(!Decide()){
                return new Verdict{ Satisfiable = true }; // TODO construct model
            }*/
        }

        bool BCP(){
            // output "conflict" if and only if conflict is encountered
            // repeated application of the unit clause rule until either
            // a conflict is encountered or there are no more implications
            throw new NotImplementedException();
        }

        /*
        int AnalyzeConflict(){
            // input:
            // output: Backtracking decision level + a new conflict clause
            if(currDL == 0){
                return -1;
            }
            var cl = CurrentConflictingClause();
            while(!StopCriterionMet(cl)){
                var lit = LastAssignedLiteral(cl);
                var variable = VariableOfLiteral(lit);
                var ante = Antecedent(lit);
                cl = Resolve(cl, ante, variable);
            }
            AddClauseToDatabase(cl);
            return ClauseAssertingLevel(cl);
        }

        private int ClauseAssertingLevel(object cl)
        {
            throw new NotImplementedException();
        }

        private void AddClauseToDatabase(object cl)
        {
            throw new NotImplementedException();
        }

        private object Resolve(object cl, object ante, object variable)
        {
            throw new NotImplementedException();
        }

        private object Antecedent(object lit)
        {
            throw new NotImplementedException();
        }

        private object VariableOfLiteral(object lit)
        {
            throw new NotImplementedException();
        }

        private object LastAssignedLiteral(object cl)
        {
            throw new NotImplementedException();
        }

        private bool StopCriterionMet(object cl)
        {
            // e.g. return true iff cl contains the negation
            // of the first UIP as its single literal at the current
            // decision level
            throw new NotImplementedException();
        }

        private object CurrentConflictingClause()
        {
            throw new NotImplementedException();
        }

        void Backtrack(int decisionLevel){
            
        }

        bool Decide(){
            // Choose an unassigned variable and a truth value for it
            // False iff there are no more variables to assign
            // decision heuristics:
            //  Jeroslow-Weng: J(l) = sum_{c in B ^ l in c} 2^{-|c|}
            //  - choose the literal for which J(l) is maximal,
            //    and for which neither l or neg l is asserted
            //  Dynamic-Largest Individual Sum (DLIS)
            //  - at each decision level, choose the unassigned literal
            //    that satisfies the largest number of currently unsatisfied clauses.
            //  Variable State Independent Decaying Sum (VSIDS)
            //  - disregard the question whether a clause in which a literal
            //    appears is already satisfied or not (!! Conflict clauses included)
            //  - periodically divide all scores by 2
            throw new NotImplementedException();
        }
        */
    }
}