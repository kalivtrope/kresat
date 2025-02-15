using Kresat.Solvers;

namespace Kresat.Representations {
    internal class WatchClause : IClause<WatchLiteral>, IPurgeable<WatchLiteral>,
                                 ICreateFromLiterals<WatchClause, WatchLiteral> {
      public List<WatchLiteral> Literals {get;set;}
      public static WatchClause Create(List<WatchLiteral> _literals){
        return new WatchClause(_literals);
      }
      public bool IsDeleted {get;private set;} = false;
      IEnumerable<WatchLiteral> IPurgeable<WatchLiteral>.DeleteSelf(){
        IsDeleted = true;
        if(Literals.Count >= 2){
          return [ Literals[0], Literals[1] ];
        }
        return [];
      }

      public WatchClause(List<WatchLiteral> _literals){
        this.Literals = _literals;
        ConstructWatches();
      }
      public bool IsSatisfied(){
        return (Literals.Count == 1 && Literals[0].Value == Valuation.SATISFIED)
        || (Literals.Count >= 2 && (Literals[0].Value == Valuation.SATISFIED || Literals[1].Value == Valuation.SATISFIED)); 
      }

      bool CanBeWatched(int pos){
        return Literals[pos].Value != Valuation.FALSIFIED;
      }
      int[] LastPos = [1,1];
      void ConstructWatches(){
        if(Literals.Count <= 1){
          return;
        }
        FindNext(0);
        FindNext(1);
        Literals[0].ClausesWithWatch.Add(this);
        Literals[1].ClausesWithWatch.Add(this);
      }
      void FindNext(int litIdx){
        if(Literals.Count <= 2){
          return;
        }
        if(CanBeWatched(litIdx)){
          return;
        }
        int startPos = LastPos[litIdx]+1;
        if(startPos >= Literals.Count){
          startPos = 2;
        }
        int currPos = startPos;
        do {
          if(CanBeWatched(currPos)){
            Literals.Swap(litIdx, currPos);
            LastPos[litIdx] = currPos;
            break;
          }
          currPos++;
          if(currPos >= Literals.Count)
            currPos = 2;
        } while(currPos != startPos);
      }
      public bool IsUnit(){  
        //  i) clause is unit if it has size 1 and the only literal is unsatisfied   
        // ii) clause is unit if one of the watches points to a falsified literal
        //     and the other watch points to an unsatisfied literal
        return  (Literals.Count == 1 && Literals[0].Value == Valuation.UNSATISFIED)
             || (Literals.Count >= 2 && (
                 (Literals[0].Value == Valuation.UNSATISFIED && Literals[1].Value == Valuation.FALSIFIED)
                  || (Literals[1].Value == Valuation.UNSATISFIED && Literals[0].Value == Valuation.FALSIFIED)
             ));
      }
      public bool IsFalsified(){
        return (Literals.Count == 1 && Literals[0].Value == Valuation.FALSIFIED)
              || (Literals.Count >= 2 && (
                Literals[0].Value == Valuation.FALSIFIED && Literals[1].Value == Valuation.FALSIFIED
              ));
      }

      public WatchLiteral GetUnitLiteral(){
        if(Literals.Count == 1){
          return Literals[0];
        }
        else if(Literals[0].Value == Valuation.UNSATISFIED){
          return Literals[0];
        }
        else{
          if(Literals[1].Value != Valuation.UNSATISFIED){
              throw new Exception(Literals[1].ToString());
          }
          return Literals[1]; 
        }
      }
      public WatchLiteral FindNextForLiteral(WatchLiteral literal){
        if(literal == Literals[0]){
          FindNext(0);
          return Literals[0];
        }
        else{
          FindNext(1);
          return Literals[1];
        }
      }

    }

  internal sealed class WatchLiteral : ILiteral<WatchLiteral>, ILearning<WatchClause> {
      public List<WatchClause> ClausesWithWatch = new();
      public Valuation Value {get; private set;} = Valuation.UNSATISFIED;
      public int LitNum {get;set;}
      public WatchLiteral Opposite { get; set; }
      public WatchClause? Antecedent { get; set; }
      public int DecisionLevel { get; set; }

        public void Satisfy(){
        Value = Valuation.SATISFIED;
      }
      public IReadOnlyList<IClause<WatchLiteral>> GetClauses(){
        return ClausesWithWatch;
      }
      public void Falsify(){
        Value = Valuation.FALSIFIED;
        for(int i = 0; i < ClausesWithWatch.Count; i++){
          var currClause = ClausesWithWatch[i];
          if(currClause.IsSatisfied()) continue;
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

    internal class WatchedLiteralsWithLearning : UnitPropagationDSWithLearning<WatchLiteral, WatchClause>
    {
        public WatchedLiteralsWithLearning(CommonRepresentation cr, ResetDeletionConfiguration config) : base(cr, config){}

        public sealed override void PurgeDeletedClausesOfLiterals(HashSet<WatchLiteral> literals)
        {
          foreach(var lit in literals){
            for(int i = 0; i < lit.ClausesWithWatch.Count; i++){
              if(lit.ClausesWithWatch[i].IsDeleted){
                lit.ClausesWithWatch.RemoveInPlace(i);
                i--;
              }
            }
          }
        }
    }
}