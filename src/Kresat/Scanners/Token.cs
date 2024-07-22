namespace Kresat.Scanners {
    public enum SmtLibTokenType {
        LEFT_PAREN, RIGHT_PAREN,
        OR, AND, NOT,
        IDENTIFIER,
        EOF
    }
    public enum DimacsTokenType {
        NUM_CLAUSES, NUM_VARS, LITERAL, CLAUSE_END, EOF
    }
    public interface IToken<TSelf, TTokenType, TPayload>
        where TSelf : IToken<TSelf, TTokenType, TPayload> {
        static abstract TSelf Create(TTokenType arg);
        static abstract TSelf Create(TTokenType arg1, TPayload arg2);
        public TTokenType Type {get;}
        public TPayload? Payload {get;}
    }

    public class DimacsToken : IToken<DimacsToken, DimacsTokenType, int>
    {
        public DimacsTokenType Type {get;private set;}
        public int Payload {get;private set;}
        public DimacsToken(DimacsTokenType type){
            Type = type;
        }
        public DimacsToken(DimacsTokenType type, int identifier){
            Type = type;
            Payload = identifier;
        }
        public override string ToString()
        {
            return $"{Type} {Payload}";
        }

        public static DimacsToken Create(DimacsTokenType arg)
        {
            return new DimacsToken(arg);
        }

        public static DimacsToken Create(DimacsTokenType arg1, int arg2)
        {
            return new DimacsToken(arg1, arg2);
        }
    }
    public class SmtLibToken : IToken<SmtLibToken, SmtLibTokenType, string>
    {
        public SmtLibTokenType Type {get;private set;}
        public string? Payload {get;private set;}
        public SmtLibToken(SmtLibTokenType type){
            Type = type;
        }
        public override string ToString()
        {
            return $"{Type} {Payload}";
        }
        public SmtLibToken(SmtLibTokenType type, string identifier){
            Type = type;
            Payload = identifier;
        }

        public static SmtLibToken Create(SmtLibTokenType type)
        {
            return new SmtLibToken(type);
        }

        public static SmtLibToken Create(SmtLibTokenType type, string identifier)
        {
            return new SmtLibToken(type, identifier);
        }
    }
}