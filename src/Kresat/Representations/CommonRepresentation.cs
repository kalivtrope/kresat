using System.Text;
using Kresat.Loggers;

namespace Kresat.Representations {
    class CommonRepresentation {
        public required StringBuilder Comments { get; internal set; }
        public required int LiteralCount {get; internal set;}
        public required List<List<int>> Clauses { get; internal set; }
        public List<string?>? OriginalMapping {get; init;}

        public override string ToString()
        {
            StringBuilder result = Comments;

            result.AppendLine($"p cnf {LiteralCount} {Clauses.Count}");
            foreach(var clause in Clauses){
                foreach(var literal in clause){
                    result.Append($"{literal} ");
                }
                result.AppendLine("0");
            }
            return result.ToString();
        }
    }
    class CommonRepresentationBuilder {
        HashSet<int> vars = new();
        List<List<int>> clauses = new();
        StringBuilder comments = new();
        List<int>? currClause;
        int? expectedClauseNum;
        int? expectedVarNum;

        public List<string?>? OriginalMapping { get; internal set; }

        public void AddComment(string comment){
            comments.AppendJoin(separator:" ", "c", comment, "\n");
        }
        public void SetExpectedClauseNum(int num){
            expectedClauseNum = num;
        }
        public void SetExpectedVarNum(int num){
            expectedVarNum = num;
        }
        public void EndClause(){
            if(currClause is not null){
                AddClause(currClause);
            }
            currClause = null;
        }
        public void AddLiteral(int lit){
            if(currClause == null){
                currClause = [];
            }
            currClause!.Add(lit);
        }
        public void AddClause(IList<int> clause){
            HashSet<int> literals = new();
            bool redundant = false;
            for(int litIdx = 0; litIdx < clause.Count; litIdx++){
                int litNum = clause[litIdx];
                if(litNum == 0){
                    ErrorLogger.Report("Cannot have a 0 literal in clause");
                }
                if(literals.Contains(-litNum)){
                    ErrorLogger.ReportWarning($"Cannot have both {litNum} and {-litNum} in the same clause");
                    redundant = true;
                }
                if(literals.Contains(litNum)){
                    clause.RemoveInPlace(litIdx);
                    litIdx--;
                    ErrorLogger.ReportWarning($"Duplicit literal: {litNum}");
                }
                vars.Add(Math.Abs(litNum));
                literals.Add(litNum);
            }
            if(!redundant){
                clauses.Add(clause.ToList());
            }
        }

        public void CheckEquals(int expected, int actual, string what){
            if(expected != actual){
                ErrorLogger.ReportWarning($"Wrong {what}: expected {expected}, got {actual}");
            }
        }
        public CommonRepresentation Build(){
            if(expectedClauseNum is not null){
                CheckEquals(expectedClauseNum.Value, clauses.Count, "clause number");
            }
            if(expectedVarNum is not null){
                CheckEquals(expectedVarNum.Value, vars.Max(), "max variable number");
            }
            return new CommonRepresentation {
                OriginalMapping = this.OriginalMapping,
                Comments = comments,
                LiteralCount = vars.Max(),
                Clauses = clauses
            };
        }

    }
}