using Kresat.Representations;

namespace Kresat.Parsers {
    interface IParser {
        public CommonRepresentationBuilder cr {get;}
        public CommonRepresentation ToCommonRepresentation();
    }
}