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
  enum Valuation {
    FALSIFIED,
    UNSATISFIED,
    SATISFIED
  }
  class AdjacencyLists {
    public int currDecisionLevel {get; private set;} = 0;
    public int numConflictingClauses = 0;
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

        internal record struct Decision {
            public int DecisionLevel { get; internal set; }
            public int Literal { get; internal set; }
        }
        internal Stack<Decision> decisions = new();
        SortedSet<int> UndecidedVars = new();
    public void Backtrack(int decisionLevel){
      bool refilled = false;
      while (decisions.Count > 0 && decisions.Peek().DecisionLevel > decisionLevel){
        Decision decision = decisions.Pop();
        bool shouldRefill = decisions.Count <= 0 || decisions.Peek().DecisionLevel <= decisionLevel; 
        UndoLiteral(decision.Literal,
                    refillUnits: shouldRefill);
        refilled |= shouldRefill;
      }
      if(!refilled){
        Console.Error.WriteLine("ERROR: DID NOT REFILL");
      }
      currDecisionLevel = decisionLevel;
    }
    public void UndoLastLiteral(){
      Backtrack(currDecisionLevel-1);
    }
    public void AddClause(IEnumerable<int> clause){
      // assuming the literals in the clause are from the original variable range
      ClauseData cd = new() { Literals = clause};
      int clauseNo = clauseData.Count;
      foreach (var lit in clause){
        cd.AddLiteral();
        //Console.WriteLine($"accessing index {LiteralIdx(lit)} of a literal {lit} at array of size {literalData.Length}");
        literalData[LiteralIdx(lit)].AddClause(clauseNo);
      }
      clauseData.Add(cd);
      if (cd.IsUnit()){
        unitClauses.Push(clauseNo);
      }
    }

    public void DecideLiteral(int literal){
      currDecisionLevel++;
      AssignLiteral(literal);
    }

    public void UnitPropagation(){
      while(unitClauses.Count > 0 && numConflictingClauses == 0){
        int currClause = unitClauses.Pop();
        //Console.WriteLine($"clauseData Count: {clauseData.Count} accessing {currClause}");
        //CheckEqual(clauseData[currClause].IsUnit(), true);
        if(!clauseData[currClause].IsUnit()){
          continue;
        }
        foreach(int lit in clauseData[currClause].Literals){
          if(literalData[LiteralIdx(lit)].Value == Valuation.UNSATISFIED){
            AssignLiteral(lit);
            break;
          }
        }
      }
    }

    private void AssignLiteral(int literal){
      decisions.Push(new Decision{ DecisionLevel = currDecisionLevel, Literal = literal});
      UndecidedVars.Remove(Math.Abs(literal));
      literalData[LiteralIdx(literal)].Value = Valuation.SATISFIED;
      literalData[LiteralIdx(-literal)].Value = Valuation.FALSIFIED;
      foreach (var clause in literalData[LiteralIdx(literal)].adjacentClauses){
        clauseData[clause].SatisfyLiteral();
      }
      foreach (var clause in literalData[LiteralIdx(-literal)].adjacentClauses){
        clauseData[clause].FalsifyLiteral();
        if(clauseData[clause].IsUnit()){
          unitClauses.Push(clause);
        }
        else{
          if(clauseData[clause].IsConflicting()){
            numConflictingClauses++;
          }
        }
      }
    }
    private void UndoLiteral(int literal, bool refillUnits){
      if(refillUnits){
        unitClauses.Clear();
      }
      CheckEqual(literalData[LiteralIdx(literal)].Value, Valuation.SATISFIED);
      CheckEqual(literalData[LiteralIdx(-literal)].Value, Valuation.FALSIFIED);
      UndecidedVars.Add(Math.Abs(literal));
      literalData[LiteralIdx(literal)].Value = Valuation.UNSATISFIED;
      literalData[LiteralIdx(-literal)].Value = Valuation.UNSATISFIED;
      foreach (var clause in literalData[LiteralIdx(literal)].adjacentClauses){
        clauseData[clause].UnsatisfyLiteral();
        if(refillUnits && clauseData[clause].IsUnit()){
          unitClauses.Push(clause);
        }
      }
      foreach (var clause in literalData[LiteralIdx(-literal)].adjacentClauses){
        if(clauseData[clause].IsConflicting()){
          numConflictingClauses--;
        }
        clauseData[clause].UnfalsifyLiteral();
        if(refillUnits && clauseData[clause].IsUnit()){
          unitClauses.Push(clause);
        }
      }
    }

        private void CheckEqual(Valuation actual, Valuation expected)
        {
            if(actual != expected){
              ErrorLogger.Report(0, $"Wrong Valuation: expected {expected}, got {actual}");
            }
        }

    record class ClauseData {
      void CheckNonnegative(int val, string name){
        if (val < 0){
          ErrorLogger.Report(0, $"{name} became negative which should be impossible");
        }
      }
      int numLiterals;
      int numFalsifiedLiterals;
      int numSatisfiedLiterals;

      public required IEnumerable<int> Literals { get; internal set; }
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
      public bool IsConflicting(){
        return numFalsifiedLiterals == numLiterals;
      }
    }
    List<ClauseData> clauseData = new();
    LiteralData[] literalData;
    Stack<int> unitClauses = new();

    int LiteralIdx(int lit){
      if (lit < 0) {
        return -2*(lit + 1);
      }
      return 2*(lit - 1) + 1;
    }
    Valuation GetValuation(int lit){
      return literalData[LiteralIdx(lit)].Value;
    }

        internal int ChooseDecisionLiteral()
        {
            return UndecidedVars.Min();
        }

        internal List<int> ConstructModel()
        {
          List<int> res = new List<int>();
          for(int _var = 1; _var <= literalData.Length /2; _var++){
            res.Add(literalData[LiteralIdx(_var)].Value == Valuation.SATISFIED ? _var : -_var);
          }
          return res;
        }

    public AdjacencyLists(CommonRepresentation cr){
      literalData = new LiteralData[2 * cr.LiteralCount];
      for(int i = 0; i < literalData.Length; i++){
        literalData[i] = new();
      }
      UndecidedVars = new SortedSet<int>(Enumerable.Range(1, cr.LiteralCount));
      foreach (var clause in cr.Clauses){
        AddClause(clause);
      }
    }

    private class LiteralData {
      public Valuation Value = Valuation.UNSATISFIED;
      public List<int> adjacentClauses = new();
      public void AddClause(int clauseNo){
        adjacentClauses.Add(clauseNo);
      }
    }
  }
}