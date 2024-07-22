using Kresat.Representations;

namespace Kresat.Parsers {
    interface IParser {
        public CommonRepresentation cr {get;}
        public CommonRepresentation ToCommonRepresentation();
    }
}