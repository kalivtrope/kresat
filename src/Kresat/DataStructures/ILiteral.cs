namespace Kresat.Representations {
    enum Valuation {
        FALSIFIED,
        UNSATISFIED,
        SATISFIED
    }
    internal record struct Decision<TLiteral> {
        public int DecisionLevel { get; internal set; }
        public TLiteral Literal { get; internal set; }
    }
    interface ILiteral<TLiteral> where TLiteral : ILiteral<TLiteral> {
        public Valuation Value {get;}
        public TLiteral Opposite {get;set;}
        public int LitNum {get;set;}
        public void AssignLitNum(int LitNum){
            this.LitNum = LitNum;
        }
        public void SetOther(TLiteral Opposite){
            this.Opposite = Opposite;
        }
        void Satisfy();
        void Falsify();
        void Unsatisfy();
        IReadOnlyList<IClause<TLiteral>> GetClauses();
    }
}