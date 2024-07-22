using System.Text;
using Kresat.Loggers;

namespace Kresat.Representations {
    class CommonRepresentation {
        HashSet<int> vars = new();
        List<List<int>> clauses = new();
        StringBuilder comments = new();
        List<int>? currClause;
        int? expectedClauseNum;
        int? expectedVarNum;
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
        public void AddClause(IEnumerable<int> clause){
            HashSet<int> literals = new();
            foreach(var literal in clause){
                if(literal == 0){
                    ErrorLogger.Report(0, "Cannot have a 0 literal in clause");
                }
                if(literals.Contains(-literal)){
                    ErrorLogger.Report(0, $"Cannot have both {literal} and {-literal} in the same clause");
                }
                vars.Add(Math.Abs(literal));
                literals.Add(literal);
            }
            clauses.Add(clause.ToList());
        }
        public void CheckEquals(int expected, int actual, string what){
            if(expected != actual){
                ErrorLogger.Report(0, $"Wrong {what}: expected {expected}, got {actual}");
            }
        }
        public override string ToString()
        {
            StringBuilder result = comments;
            int maxVar = vars.Max();
            if(expectedClauseNum is not null){
                CheckEquals(expectedClauseNum.Value, clauses.Count, "clause number");
            }
            if(expectedVarNum is not null){
                CheckEquals(expectedVarNum.Value, maxVar, "max variable number");
            }
            result.AppendLine($"p cnf {maxVar} {clauses.Count}");
            foreach(var clause in clauses){
                foreach(var literal in clause){
                    result.Append($"{literal} ");
                }
                result.AppendLine("0");
            }
            return result.ToString();
            
        }

    }
}