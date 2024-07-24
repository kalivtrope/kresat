namespace Kresat.Solvers {
    record struct Verdict {
        public required readonly bool Satisfiable {get;init;}
        public readonly List<int>? Model {get;init;}

        public string ToString(List<string?> Mapping){
            if(!Satisfiable){
                return "UNSAT";
            }
            var Mapped = Model!.Select(
                                i => $"{(i < 0 && Mapping[Math.Abs(i)] is not null ? '-' : null)}{Mapping[Math.Abs(i)]}"
                            ).Where(i => !string.IsNullOrEmpty(i))
                            .OrderBy(i => i[0] == '-' ? i[1..] : i);
            return $"SAT {string.Join(' ', Mapped)}";
        }
        public override string ToString(){
            return $"{(Satisfiable ? "SAT" : "UNSAT")} {(Model is not null ? string.Join(' ', Model) : null)}";
        }
    }
}