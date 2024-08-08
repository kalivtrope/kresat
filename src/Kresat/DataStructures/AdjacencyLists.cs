using Kresat.Loggers;
using Kresat.Solvers;
namespace Kresat.Representations {
    internal class AdjacencyListClause : IClause<AdjacencyListLiteral>, IPurgeable,
                   ICreateFromLiterals<AdjacencyListClause, AdjacencyListLiteral>{
    /*
    We use a counter-based approach to Adjacency lists
      as described in
      https://www.cs.cmu.edu/~15414/s23/s21/f18/lectures/20-sat-techniques.pdf

    We need to maintain the following invariant:
      Each time we assign l_i = true, we need to increase
      the satisfied counter of all clauses c_i that contain l_i.
      We also need to increase the satisfied counter of all clauses c_i
      that contain \not l_i.
    */
      public List<AdjacencyListLiteral> Literals { get; set; }
        void CheckNonnegative(int val, string name){
        if (val < 0){
          ErrorLogger.Report(0, $"{name} became negative which should be impossible");
        }
      }
      public bool IsDeleted {get; set;} = false;
      int numLiterals;
      int numFalsifiedLiterals;
      int numSatisfiedLiterals;

      public AdjacencyListClause(List<AdjacencyListLiteral> _literals){
        Literals = _literals;
        numLiterals = _literals.Count;
        foreach(var literal in _literals){
          if(literal.Value == Valuation.FALSIFIED){
            numFalsifiedLiterals++;
          }
          else if(literal.Value == Valuation.SATISFIED){
            numSatisfiedLiterals++;
          }
          literal.AddClause(this);
        }
      }
      public void FalsifyLiteral(){
        numFalsifiedLiterals++;
      }
      public void UnfalsifyLiteral(){
        numFalsifiedLiterals--;
        CheckNonnegative(numFalsifiedLiterals, nameof(numFalsifiedLiterals));
      }
      public void SatisfyLiteral(){
        numSatisfiedLiterals++;
      }
      public void UnsatisfyLiteral(){
        numSatisfiedLiterals--;
        CheckNonnegative(numSatisfiedLiterals, nameof(numSatisfiedLiterals));
      }
      public bool IsUnit(){
        if(IsDeleted) return false;
        return numSatisfiedLiterals == 0
        && numFalsifiedLiterals + 1 == numLiterals;
      }
      public bool IsSatisfied(){
        if(IsDeleted) return false;
        return numSatisfiedLiterals >= 1;
      }
      public AdjacencyListLiteral GetUnitLiteral(){
        foreach(var lit in Literals){
          if(lit.Value == Valuation.UNSATISFIED){
            return lit;
          }
        }
        throw new ArgumentException();
      }
      public bool IsFalsified(){
        if(IsDeleted) return false;
        return numFalsifiedLiterals == numLiterals;
      }
        public static AdjacencyListClause Create(List<AdjacencyListLiteral> _literals)
        {
            return new(_literals);
        }
    }

    internal sealed class AdjacencyListLiteral : ILiteral<AdjacencyListLiteral>, ILearning<AdjacencyListClause> {
        public Valuation Value {get; private set;} = Valuation.UNSATISFIED;
        public List<AdjacencyListClause> Clauses {get; private set;} = new();
        public AdjacencyListLiteral Opposite { get; set; }
        public int LitNum { get; set; }
        public AdjacencyListClause? Antecedent { get; set; }
        public int DecisionLevel { get; set; }

        public void Falsify(){
          Value = Valuation.FALSIFIED;
          for(int i = 0; i < Clauses.Count; i++){
            if(Clauses[i].IsDeleted){
              Clauses.RemoveInPlace(i);
              i--;
              continue;
            }
            Clauses[i].FalsifyLiteral();
          }
        }
        public IReadOnlyList<IClause<AdjacencyListLiteral>> GetClauses(){
            return Clauses;
        }
        public void Satisfy(){
          Value = Valuation.SATISFIED;
            for(int i = 0; i < Clauses.Count; i++){
              Clauses[i].SatisfyLiteral();
            }
        }
        public void Unsatisfy(){
          if(Value == Valuation.UNSATISFIED){
            return;
          }
          for(int i = 0; i < Clauses.Count; i++){
            if(Clauses[i].IsDeleted){
              Clauses.RemoveInPlace(i);
              i--;
              continue;
            }
            if(Value == Valuation.FALSIFIED){
              Clauses[i].UnfalsifyLiteral();
            }
            else{
              Clauses[i].UnsatisfyLiteral();
            }
          }
          Value = Valuation.UNSATISFIED;
        }
        internal void AddClause(AdjacencyListClause clause){
            Clauses.Add(clause);
        }
    }
    internal class AdjacencyLists : UnitPropagationDS<AdjacencyListLiteral, AdjacencyListClause> {
      public AdjacencyLists(CommonRepresentation cr) : base(cr){}
    }
    internal class AdjacencyListsWithLearning : UnitPropagationDSWithLearning<AdjacencyListLiteral, AdjacencyListClause> {
      public AdjacencyListsWithLearning(CommonRepresentation cr) : base(cr){}
    }
}