using Superpower;
using Superpower.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CogParser
{

    public abstract class Statement { }

    public class Assignment : Statement
    {
        public string Identifier { get; set; }
        public Expression Expression { get; set; }
    }

    public static class MessageParser
    {
        private static TextParser<Statement> AssignmentParser =
            from identifier in CommonParsers.Identifier.ConsumeWS()
            from eq in Character.EqualTo('=').ConsumeWS()
            from expression in ExpressionParsers.ExpressionParser
            select (Statement)new Assignment
            {
                Identifier = identifier,
                Expression = expression
            };
    }
}
