using Superpower;
using Superpower.Parsers;
using System.Globalization;
using System.Linq;

namespace CogParser
{
    public enum SymbolUse
    {
        Desc,
        Local,
        NoLink,
        LinkId,
        Mask
    }

    public abstract class Symbol
    {
        public string Name { get; set; }

        public SymbolUse SymbolUse { get; set; }

        public int? LinkId { get; set; }
    }

    public class SymbolFlex : Symbol
    {
        public float? Value { get; set; }
    }

    public class SymbolInt : Symbol
    {
        public int? Value { get; set; }
    }

    public static class SymbolParser
    {

        private static TextParser<(SymbolUse use, int? linkId)> SymbolUseParser =
            Span.EqualTo("desc").Value((SymbolUse.Desc, (int?)null))
            .Or(Span.EqualTo("local").Value((SymbolUse.Local, (int?)null)))
            .Or(Span.EqualTo("nolink").Value((SymbolUse.NoLink, (int?)null)))
            .Or(Span.EqualTo("linkid").Then(_ => CommonParsers.ParseAssignment(Numerics.IntegerInt32))
                .Select(x => (SymbolUse.LinkId, (int?)x)));

        private static TextParser<float> SymbolFloatParser =
            Numerics.DecimalDouble.Select(n => (float)n);

        // flex        cost=200.0                       local
        private static TextParser<Symbol> SymbolFlexParser =
            from symbolType in Span.EqualTo("flex").ConsumeWS()
            from symbolName in CommonParsers.Identifier.ConsumeWS()
            from assignment in CommonParsers.ParseAssignment(SymbolFloatParser).ConsumeWS().Optional()
            from symbolUse in SymbolUseParser
            select (Symbol)new SymbolFlex
            {
                Name = symbolName,
                SymbolUse = symbolUse.use,
                LinkId = symbolUse.linkId,
                Value = assignment
            };

        // int        cost=53                       local
        private static TextParser<Symbol> SymbolIntParser =
            from symbolType in Span.EqualTo("int").ConsumeWS()
            from symbolName in CommonParsers.Identifier.ConsumeWS()
            from assignment in CommonParsers.ParseAssignment(Numerics.IntegerInt32).ConsumeWS().Optional()
            from symbolUse in SymbolUseParser
            select (Symbol)new SymbolInt
            {
                Name = symbolName,
                SymbolUse = symbolUse.use,
                LinkId = symbolUse.linkId,
                Value = assignment
            };


        public static Symbol ParseSymbol(string src)
        {
            return SymbolFlexParser.Or(SymbolIntParser).Parse(src);
        }
    }
}
