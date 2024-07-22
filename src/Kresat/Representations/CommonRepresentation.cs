using System.Text;
using Kresat.Loggers;

namespace Kresat.Representations {
    class CommonRepresentation {
        HashSet<int> vars = new();
        List<List<int>> clauses = new();
        StringBuilder comments = new();
        List<int>? currClause;
        public void AddComment(string comment){
            comments.AppendJoin(separator:" ", "c", comment, "\n");
        }
        public void BeginClause(){
            currClause = new();
        }
        public void EndClause(){
            AddClause(currClause!);
            currClause = null;
        }
        public void AddLiteral(int lit){
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

        public override string ToString()
        {
            StringBuilder result = comments;
            result.AppendLine($"p cnf {vars.Count} {clauses.Count}");
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