namespace Kresat.Representations {
    enum Valuation {
        FALSIFIED,
        UNSATISFIED,
        SATISFIED
    }
    internal record struct Decision {
        public int DecisionLevel { get; internal set; }
        public int Literal { get; internal set; }
    }

}