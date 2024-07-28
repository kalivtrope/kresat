/*
- two references associated with each clause C
- list of watch occurrences for each literal
- watch references are not ordered
  - move independently around the clause
- satisfied clauses detected lazily as in H/T lists

*/

namespace Kresat.Representations {
    internal class WatchClause : IClause<WatchLiteral>,
                                 ICreateFromLiterals<WatchClause, WatchLiteral> {
    internal class Watch {
      public int LitPos = 0;
      public WatchLiteral Literal;
      WatchClause clause;
      public Watch(int LitPos, WatchClause clause){
        this.LitPos = LitPos;
        this.clause = clause;
        this.Literal = clause.Literals[LitPos]; 
      }
      bool CanBeWatched(int pos){
        return clause.Literals[pos].Value != Valuation.FALSIFIED
            && clause.GetOtherWatchPos(this) != pos;
      }
      public bool FindNext(){
        // returns false if there's no unwatched unsatisfied literal left in the clause
        int startPos = LitPos;
        int currPos = startPos;
        do {
          if(CanBeWatched(currPos)){
            LitPos = currPos;
            Literal = clause.Literals[LitPos];
            return true;
          }
          currPos = (currPos + 1) % clause.Literals.Count;
        } while(currPos != startPos);
        return false;
      }
    }
      Watch? w1 {get; set;}
      Watch? w2 {get; set;}
      public List<WatchLiteral> Literals {get;set;}
      public static WatchClause Create(List<WatchLiteral> _literals){
        return new WatchClause(_literals);
      }
      public WatchClause(List<WatchLiteral> _literals){
        this.Literals = _literals;
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
        GetLiteralForWatch(w1).ClausesWithWatch.Add(this);
        GetLiteralForWatch(w2).ClausesWithWatch.Add(this);
      }
      public bool IsUnit(){  
        //  i) clause is unit if it has size 1 and the only literal is unsatisfied   
        // ii) clause is unit if one of the watches points to a falsified literal
        //     and the other watch points to an unsatisfied literal
        return  (Literals.Count == 1 && Literals[0].Value == Valuation.UNSATISFIED)
             || (Literals.Count >= 2 && (
                 (GetValue(w1!) == Valuation.UNSATISFIED && GetValue(w2!) == Valuation.FALSIFIED)
                  || (GetValue(w2!) == Valuation.UNSATISFIED && GetValue(w1!) == Valuation.FALSIFIED)
             ));
      }
      public bool IsFalsified(){
        return (Literals.Count == 1 && Literals[0].Value == Valuation.FALSIFIED)
              || (Literals.Count >= 2 && (
                GetValue(w1!) == Valuation.FALSIFIED && GetValue(w2!) == Valuation.FALSIFIED
              ));
      }

      public WatchLiteral GetUnitLiteral(){
        if(w1 is null){
          return Literals[0];
        }
        else if(GetValue(w1) == Valuation.UNSATISFIED){
          return GetLiteralForWatch(w1);
        }
        else{
          if(GetValue(w2) != Valuation.UNSATISFIED){
              throw new ArgumentException(nameof(w2));
          }
          return GetLiteralForWatch(w2);  
        }
      }
      int GetOtherWatchPos(Watch w){
        if(w == w1){
          return w2!.LitPos;
        }
        else{
          return w1!.LitPos;
        }
      }
      public WatchLiteral FindNextForLiteral(WatchLiteral literal){
        if(literal == w1!.Literal){
          w1.FindNext();
          return w1.Literal;
        }
        else{
          w2!.FindNext();
          return w2.Literal;
        }
      }
      public Valuation GetValue(int litPos){
        return Literals[litPos].Value;
      }
      public WatchLiteral GetLiteralForWatch(Watch w){
        return w.Literal;
      }
      public Valuation GetValue(Watch w){
        return w.Literal.Value;
      }
    }


  internal sealed class WatchLiteral : ILiteral<WatchLiteral> {
      public List<WatchClause> ClausesWithWatch = new();
      public Valuation Value {get; private set;} = Valuation.UNSATISFIED;
      public int LitNum {get;set;}
      public WatchLiteral Opposite { get; set; }
      public void Satisfy(){
        Value = Valuation.SATISFIED;
      }
      public IEnumerable<IClause<WatchLiteral>> GetClauses(){
        foreach(var clause in ClausesWithWatch){
          yield return clause;
        }
      }
      public void Falsify(){
        Value = Valuation.FALSIFIED;
        for(int i = 0; i < ClausesWithWatch.Count; i++){
          var currClause = ClausesWithWatch[i];
          WatchLiteral newLit = currClause.FindNextForLiteral(this);
          if(newLit != this){
            newLit.ClausesWithWatch.Add(currClause);
            ClausesWithWatch.RemoveInPlace(i);
            i--;
          }
        }
      }
      public void Unsatisfy(){
        Value = Valuation.UNSATISFIED;
      }
    }
    internal class WatchedLiterals : UnitPropagationDS<WatchLiteral, WatchClause>
    {
        public WatchedLiterals(CommonRepresentation cr) : base(cr){}
    }
}