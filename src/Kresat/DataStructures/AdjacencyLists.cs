// associate the list of adjacent clauses with each *literal*
/*
Unit clause detection:
  - counter of falsified literals in each clause
  - remember if a clause is satisfied
  - need to be updated during backtrack
Disadvantages:
  - long and slow to traverse
  - clause states are checked too often
    - only need to detect *unit* and *unsatisfied* clauses
  - *satisfied* clauses can be detected lazily

*/
using Kresat.Loggers;
namespace Kresat.Representations {
    internal class AdjacencyListClause : IClause<AdjacencyListLiteral>,
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
        public List<int> Literals { get; set; }
        public List<AdjacencyListLiteral> literalData { get; set; }

        void CheckNonnegative(int val, string name){
        if (val < 0){
          ErrorLogger.Report(0, $"{name} became negative which should be impossible");
        }
      }
      int numLiterals;
      int numFalsifiedLiterals;
      int numSatisfiedLiterals;

      public AdjacencyListClause(List<int> literals, List<AdjacencyListLiteral> literalData){
        Literals = literals;
        foreach(var literal in literals){
          literalData.At(literal).AddClause(this);
        }
        this.literalData = literalData;
      }

      public void AddLiteral(){
        numLiterals++;
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
        return numSatisfiedLiterals == 0
        && numFalsifiedLiterals + 1 == numLiterals;
      }
      public bool IsSatisfied(){
        return numSatisfiedLiterals >= 1;
      }
      public int GetUnitLiteral(){
        foreach(var lit in Literals){
          if(literalData.At(lit).Value == Valuation.UNSATISFIED){
            return lit;
          }
        }
        throw new ArgumentException();
      }
      public bool IsFalsified(){
        return numFalsifiedLiterals == numLiterals;
      }
        public static AdjacencyListClause Create(List<int> literals, List<AdjacencyListLiteral> literalData)
        {
            return new(literals, literalData);
        }
    }

    internal class AdjacencyListLiteral : ILiteral<AdjacencyListLiteral>,
                                          ICreateFromLiteralData<AdjacencyListLiteral>{
        public Valuation Value {get; private set;} = Valuation.UNSATISFIED;
        public List<AdjacencyListClause> Clauses {get; private set;} = new();
        public AdjacencyListLiteral(List<AdjacencyListLiteral> literalData){
            this.literalData = literalData;
        }
        public List<AdjacencyListLiteral> literalData { get; set; }
        public static AdjacencyListLiteral Create(List<AdjacencyListLiteral> literalData){
            return new(literalData);
        }
        public void Falsify(){
          Value = Valuation.FALSIFIED;
          foreach(var clause in Clauses){
            clause.FalsifyLiteral();
          }
        }

        public IEnumerable<IClause<AdjacencyListLiteral>> GetClauses(){
            return Clauses;
        }
        public void Satisfy(){
          Value = Valuation.SATISFIED;
            foreach(var clause in Clauses){
              clause.SatisfyLiteral();
            }
        }
        public void Unsatisfy(){
          foreach(var clause in Clauses){
            if(Value == Valuation.FALSIFIED){
              clause.UnfalsifyLiteral();
            }
            else{
              clause.UnsatisfyLiteral();
            }
          }
          Value = Valuation.UNSATISFIED;
        }

        internal void AddClause(AdjacencyListClause clause){
            Clauses.Add(clause);
        }
    }
    class AdjacencyLists : UnitPropagationDS<AdjacencyListLiteral, AdjacencyListClause> {

    public AdjacencyLists(CommonRepresentation cr) : base(cr){}
  }
}