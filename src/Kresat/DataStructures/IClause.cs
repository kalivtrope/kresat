namespace Kresat.Representations {
    interface ICreateFromLiterals<TClause, TLiteral> {
        static abstract TClause Create(List<TLiteral> _literals);
    }
    interface IClause<TLiteral> where TLiteral : ILiteral<TLiteral> {
        public List<TLiteral> Literals {get;}
        bool IsUnit();
        bool IsFalsified();
        bool IsSatisfied();
        TLiteral GetUnitLiteral();
    }    
}