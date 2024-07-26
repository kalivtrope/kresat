/*
- two references associated with each clause C
- list of watch occurrences for each literal
- watch references are not ordered
  - move independently around the clause
- satisfied clauses detected lazily as in H/T lists

*/

namespace Kresat.Representations {
  static class ListExtensions {
    public static void Swap<T>(this IList<T> list, int idx1, int idx2){
      if(idx1 == idx2) return;
      T tmp = list[idx1];
      list[idx1] = list[idx2];
      list[idx2] = tmp;
    }
    public static void RemoveInPlace<T>(this IList<T> list, int idx){
      list.Swap(idx, list.Count-1);
      list.RemoveAt(list.Count-1);
    }
  }
  class WatchedLiterals {
    public bool HasContradiction = false;
    public int currDecisionLevel = 0;
    public int unitPropSteps = 0;
    internal record struct Decision {
      public int DecisionLevel { get; internal set; }
      public int Literal { get; internal set; }
    }
    internal Stack<Decision> decisions = new();

    static int LiteralIdx(int lit){
      if (lit < 0) {
        return -2*(lit + 1);
      }
      return 2*(lit - 1) + 1;
    }
    HashSet<int> UndecidedVars = new();
    Stack<Clause> unitClauses = new();

    class Literal {
      public Valuation Value = Valuation.UNSATISFIED;
      public List<Watch> Watches = new();
      public required List<Literal> literalData {get; init;}
      public void Satisfy(){
        Value = Valuation.SATISFIED;
      }
      public void Falsify(){
        Value = Valuation.FALSIFIED;
        for(int i = 0; i < Watches.Count; i++){
          //Console.WriteLine($"before falsification: {Watches[i].LitPos}");
          int prev = Watches[i].LitPos;
          if(Watches[i].FindNext()){
            int newLit = Watches[i].clause.Literals[Watches[i].LitPos];
            if(prev == Watches[i].LitPos){
              throw new ArgumentException("aaa");
            }
            //Console.WriteLine($"after falsification: {Watches[i].LitPos}");
            literalData[LiteralIdx(newLit)].Watches.Add(Watches[i]);
            Watches.RemoveInPlace(i);
            i--;
          }
        }
      }
      public void Unsatisfy(){
        Value = Valuation.UNSATISFIED;
      }
    }
    class Clause {
      public Watch? w1 {get; private set;}
      public Watch? w2 {get; private set;}
      public List<int> Literals {get;private set;}
      List<Literal> literalData;
      public Clause(List<int> literals, List<Literal> literalData){
        this.Literals = literals;
        this.literalData = literalData;
        ConstructWatches();
      }
      void ConstructWatches(){
        if(Literals.Count <= 1){
          return;
        }
        w1 = new Watch(LitPos: 0, clause: this);
        w2 = new Watch(LitPos: 1, clause: this);
        w1.FindNext();
        w2.FindNext();
        literalData[LiteralIdx(GetLiteralForWatch(w1))].Watches.Add(w1);
        literalData[LiteralIdx(GetLiteralForWatch(w2))].Watches.Add(w2);
      }
      public bool IsUnit(){  
        //  i) clause is unit if it has size 1 and the only literal is unsatisfied   
        // ii) clause is unit if one of the watches points to a falsified literal
        //     and the other watch points to an unsatisfied literal
        return  (Literals.Count == 1 && literalData[LiteralIdx(Literals[0])].Value == Valuation.UNSATISFIED)
             || (Literals.Count >= 2 && (
                 (GetValue(w1!) == Valuation.UNSATISFIED && GetValue(w2!) == Valuation.FALSIFIED)
                  || (GetValue(w2!) == Valuation.UNSATISFIED && GetValue(w1!) == Valuation.FALSIFIED)
             ));
      }
      public bool IsFalsified(){
        return (Literals.Count == 1 && literalData[LiteralIdx(Literals[0])].Value == Valuation.FALSIFIED)
              || (Literals.Count >= 2 && (
                GetValue(w1) == Valuation.FALSIFIED && GetValue(w2) == Valuation.FALSIFIED
              ));
      }

      public override string ToString(){
        return $"literals: {string.Join(' ',
                            Literals.Select(l => l.ToString() + '/' + literalData[LiteralIdx(l)].Value.ToString()))
                            } unit: {IsUnit()} falsified: {IsFalsified()} watches: {(Literals.Count == 1 ? null : w1.LitPos.ToString() + ' ' + w2.LitPos.ToString())}";
      }
      public Watch GetOtherWatch(Watch w){
        if(w == w1){
          return w2!;
        }
        else if(w == w2){
          return w1!;
        }
        throw new ArgumentException(nameof(w));
      }
      public Valuation GetValue(int pos){
        //Console.WriteLine($"accessing {pos} of array of size {Literals.Count}");
        //Console.WriteLine($"accessing {LiteralIdx(Literals[pos])} of array of size {literalData.Count}");
        return literalData[LiteralIdx(Literals[pos])].Value;
      }
      public int GetLiteralForWatch(Watch w){
        return Literals[w.LitPos];
      }
      public Valuation GetValue(Watch w){
        return GetValue(w.LitPos);
      }
    }
    public void Backtrack(int decisionLevel){
      HasContradiction = false;
      unitClauses.Clear();
      while(decisions.Count > 0 && decisions.Peek().DecisionLevel > decisionLevel){
        Decision decision = decisions.Pop();
        UndoLiteral(decision.Literal);
      }
      currDecisionLevel = decisionLevel;
    }
    public void UnitPropagation(){
      while(unitClauses.Count > 0 && !HasContradiction){
        unitPropSteps++;
        Clause currClause = unitClauses.Pop();
        if(!currClause.IsUnit()){
          if(currClause.IsFalsified()){
            HasContradiction = true;
          }
          continue;
        }
        if(currClause.w1 is null){
          AssignLiteral(currClause.Literals[0]);
        }
        else if(currClause.GetValue(currClause.w1) == Valuation.UNSATISFIED){
          AssignLiteral(currClause.GetLiteralForWatch(currClause.w1));
        }
        else{
          if(currClause.GetValue(currClause.w2) != Valuation.UNSATISFIED){
            throw new ArgumentException(nameof(currClause.w2));
          }
          AssignLiteral(currClause.GetLiteralForWatch(currClause.w2));
        }
      }      
    }
    private void AssignLiteral(int literal){
      //Console.WriteLine($"assigning {literal}");
      decisions.Push(new Decision { DecisionLevel = currDecisionLevel, Literal = literal });
      UndecidedVars.Remove(Math.Abs(literal));
      literalData[LiteralIdx(literal)].Satisfy();
      literalData[LiteralIdx(-literal)].Falsify();
      /*foreach(var clause in clauseData){
        Console.WriteLine($"D: {clause}");
      }*/
      foreach(var watch in literalData[LiteralIdx(-literal)].Watches){
        if(watch.clause.IsUnit()){
          //Console.WriteLine($"found unit {watch.clause}");
          unitClauses.Push(watch.clause);
        }
        if(watch.clause.IsFalsified()){
          //Console.WriteLine($"found falsified");
          HasContradiction = true;
        }
      }
    }
    private void UndoLiteral(int literal)
    {
      //Console.WriteLine($"undoing {literal}");
      UndecidedVars.Add(Math.Abs(literal));
      literalData[LiteralIdx(literal)].Unsatisfy();;
      literalData[LiteralIdx(-literal)].Unsatisfy();
    }

    class Watch {
      public Clause clause;
      public int LitPos {get; private set;} = 0;
      public Watch(int LitPos, Clause clause){
        this.clause = clause;
        this.LitPos = LitPos;
      }
      bool CanBeWatched(int pos){
        return clause.GetValue(pos) != Valuation.FALSIFIED && clause.GetOtherWatch(this).LitPos != pos;
      }
      public bool FindNext(){
        // returns false if there's no unwatched unsatisfied literal left in the clause
        int startPos = LitPos;
        int currPos = startPos;
        do {
          if(CanBeWatched(currPos)){
            LitPos = currPos;
            return true;
          }
          currPos = (currPos + 1) % clause.Literals.Count;
        } while(currPos != startPos);
        return false;
      }
    }
    List<Clause> clauseData;
    List<Literal> literalData;

    internal int ChooseDecisionLiteral()
        {
            return UndecidedVars.First();
        }

    public void DecideLiteral(int literal){
      currDecisionLevel++;
      AssignLiteral(literal);
    }

     public void UndoLastLiteral(){
      Backtrack(currDecisionLevel-1);
    }
    public WatchedLiterals(CommonRepresentation cr){
      literalData = new List<Literal>( new Literal[2*cr.LiteralCount]);
      for(int i = 0; i < literalData.Count; i++){
        literalData[i] = new Literal{literalData = literalData};
      }
      clauseData = new();
      UndecidedVars = new HashSet<int>(Enumerable.Range(1, cr.LiteralCount));
      foreach(var clause in cr.Clauses){
        AddClause(clause);
      }
    }
        private void AddClause(List<int> literals)
        {
          Clause clause = new(literals, literalData);
          clauseData.Add(clause);
          //Console.WriteLine($"add clause: {clause}");
          if(clause.IsUnit()){
            unitClauses.Push(clause);
          }
        }
        internal List<int> ConstructModel()
        {
          List<int> res = new List<int>();
          for(int _var = 1; _var <= literalData.Count / 2; _var++){
            if(literalData[LiteralIdx(_var)].Value == Valuation.UNSATISFIED){
              throw new ArgumentException();
            }
            res.Add(literalData[LiteralIdx(_var)].Value == Valuation.SATISFIED ? _var : -_var);
          }
          return res;
        }
    }
}