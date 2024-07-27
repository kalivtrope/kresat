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
      Watch? w1 {get; set;}
      Watch? w2 {get; set;}
      public List<int> Literals {get;set;}
      public List<WatchLiteral> literalData {get;set;}
      public static WatchClause Create(List<int> literals, List<WatchLiteral> literalData){
        return new WatchClause(literals, literalData);
      }
      public WatchClause(List<int> literals, List<WatchLiteral> literalData){
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
        literalData.At(GetLiteralForWatch(w1)).Watches.Add(w1);
        literalData.At(GetLiteralForWatch(w2)).Watches.Add(w2);
      }
      public bool IsUnit(){  
        //  i) clause is unit if it has size 1 and the only literal is unsatisfied   
        // ii) clause is unit if one of the watches points to a falsified literal
        //     and the other watch points to an unsatisfied literal
        return  (Literals.Count == 1 && literalData.At(Literals[0]).Value == Valuation.UNSATISFIED)
             || (Literals.Count >= 2 && (
                 (GetValue(w1!) == Valuation.UNSATISFIED && GetValue(w2!) == Valuation.FALSIFIED)
                  || (GetValue(w2!) == Valuation.UNSATISFIED && GetValue(w1!) == Valuation.FALSIFIED)
             ));
      }
      public bool IsFalsified(){
        return (Literals.Count == 1 && literalData.At(Literals[0]).Value == Valuation.FALSIFIED)
              || (Literals.Count >= 2 && (
                GetValue(w1) == Valuation.FALSIFIED && GetValue(w2) == Valuation.FALSIFIED
              ));
      }

      public int GetUnitLiteral(){
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
      public override string ToString(){
        return $"literals: {string.Join(' ',
                            Literals.Select(l => l.ToString() + '/' + literalData.At(l).Value.ToString()))
                            } unit: {IsUnit()} falsified: {IsFalsified()} watches: {(Literals.Count == 1 ? null : w1.LitIdx.ToString() + ' ' + w2.LitIdx.ToString())}";
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
      public Valuation GetValue(int litIdx){
        return literalData.At(litIdx).Value;
      }
      public int GetLiteralForWatch(Watch w){
        return w.LitIdx;
      }
      public Valuation GetValue(Watch w){
        return GetValue(w.LitIdx);
      }
    }

  internal class Watch {
      public WatchClause clause;
      int LitPos = 0;
      public int LitIdx;
      public Watch(int LitPos, WatchClause clause){
        this.clause = clause;
        this.LitPos = LitPos;
        LitIdx = clause.Literals[LitPos];
      }
      bool CanBeWatched(int pos){
        return clause.GetValue(clause.Literals[pos]) != Valuation.FALSIFIED
            && clause.GetOtherWatch(this).LitPos != pos;
      }
      public bool FindNext(){
        // returns false if there's no unwatched unsatisfied literal left in the clause
        int startPos = LitPos;
        int currPos = startPos;
        do {
          if(CanBeWatched(currPos)){
            LitPos = currPos;
            LitIdx = clause.Literals[LitPos];
            return true;
          }
          currPos = (currPos + 1) % clause.Literals.Count;
        } while(currPos != startPos);
        return false;
      }
    }
  internal sealed class WatchLiteral : ILiteral<WatchLiteral>, ICreateFromLiteralData<WatchLiteral> {
      public List<Watch> Watches = new();
      public Valuation Value {get; private set;} = Valuation.UNSATISFIED;
      public List<WatchLiteral> literalData { get; set; }

      public WatchLiteral(List<WatchLiteral> literalData){
        this.literalData = literalData;
      }
      public static WatchLiteral Create(List<WatchLiteral> literalData){
        return new WatchLiteral(literalData);    
      }
      public void Satisfy(){
        Value = Valuation.SATISFIED;
      }
      public IEnumerable<IClause<WatchLiteral>> GetClauses(){
        foreach(var watch in Watches){
          yield return watch.clause;
        }
      }
      public void Falsify(){
        Value = Valuation.FALSIFIED;
        for(int i = 0; i < Watches.Count; i++){
          int prev = Watches[i].LitIdx;
          if(Watches[i].FindNext()){
            int newLit = Watches[i].LitIdx;
            if(prev == Watches[i].LitIdx){
              throw new ArgumentException("aaa");
            }
            literalData.At(newLit).Watches.Add(Watches[i]);
            Watches.RemoveInPlace(i);
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