using Kresat.Representations;

namespace Kresat.Solvers {
    interface ISolver { 
        public int numDecisions {get;}
        public int unitPropSteps {get;}
        public Verdict Solve();
    }
}