using Superpower;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CogParser
{
    public static class CommonParsers
    {
        public static TextParser<string> Identifier =
            from nameFirst in Character.Letter
            from nameRest in Character.LetterOrDigit.Or(Character.In('_')).Many()
            select nameFirst + new string(nameRest);

        public static TextParser<T> ConsumeWS<T>(this TextParser<T> textParser) =>
            from main in textParser
            from skipped in Span.WhiteSpace.Many()
            select main;

        public static TextParser<T> ParseAssignment<T>(TextParser<T> valueParser)
        {
            return from eq in Character.EqualTo('=').ConsumeWS()
                   from value in valueParser
                   select value;
        }

        
    }
}
