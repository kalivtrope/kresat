using Kresat.Representations;
using Kresat.Solvers;

namespace Kresat.Verifiers {
    internal class Verifier {
        public static bool Verify(CommonRepresentation cr, Verdict verdict){
            if(!verdict.Satisfiable){
                return true;
            }
            List<int> valuation = verdict.Model!;
            foreach(var clause in cr.Clauses){
                if(!valuation.Intersect(clause).Any()){
                    return false;
                }
            }
            return true;
        }
    }
}