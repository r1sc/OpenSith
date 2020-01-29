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
    public abstract class Expression
    { }

    public class FloatExpression : Expression
    {
        public float Value { get; set; }
    }

    public class IntExpression : Expression
    {
        public int Value { get; set; }
    }
    public class StringExpression : Expression
    {
        public string Value { get; set; }
    }

    public enum ExpressionType
    {
        GreaterThan,
        LessThan,
        GreaterThanOrEqualTo,
        LessThanOrEqualTo,
        EqualTo,
        Multiply,
        Divide,
        Plus,
        Minus
    }

    public class BinaryExpression : Expression
    {
        public Expression Left { get; set; }
        public ExpressionType Operator { get; set; }
        public Expression Right { get; set; }
    }

    public class NegateExpression : Expression
    {
        public Expression NegatedExpression { get; set; }
    }

    public static class ExpressionParsers
    {
        public static Expression ParseExpression(string src)
        {
            return ExpressionParser.Parse(src);
        }


        static TextParser<ExpressionType> Operator(string op, ExpressionType opType)
        {
            return Span.EqualTo(op).ConsumeWS().Value(opType);
        }

        static readonly TextParser<ExpressionType> Add = Operator("+", ExpressionType.Plus);
        static readonly TextParser<ExpressionType> Subtract = Operator("-", ExpressionType.Minus);
        static readonly TextParser<ExpressionType> Multiply = Operator("*", ExpressionType.Multiply);
        static readonly TextParser<ExpressionType> Divide = Operator("/", ExpressionType.Divide);

        public static TextParser<float> FloatParser =
            from first in Character.Digit.AtLeastOnce()
            from dot in Character.EqualTo('.')
            from rest in Character.Digit.AtLeastOnce()
            select float.Parse(new string(first) + "." + new string(rest), CultureInfo.InvariantCulture);

        static readonly TextParser<Expression> NumberParser =
             FloatParser.Select(x => (Expression)new FloatExpression { Value = x })
            .Try().Or(Numerics.IntegerInt32.Select(x => (Expression)new IntExpression { Value = x }));


        static readonly TextParser<Expression> Factor =
            (from lparen in Character.EqualTo('(')
             from expr in Parse.Ref(() => ExpressionParser)
             from rparen in Character.EqualTo(')')
             select expr).Named("expression")
            .Or(NumberParser);

        static readonly TextParser<Expression> Operand =
            ((from sign in Character.EqualTo('-')
              from factor in Factor
              select (Expression)new NegateExpression { NegatedExpression = factor }
             ).Or(Factor)).ConsumeWS();

        static Expression MakeBinary(ExpressionType expressionType, Expression a, Expression b) =>
            new BinaryExpression
            {
                Left = a,
                Operator = expressionType,
                Right = b
            };

        static readonly TextParser<Expression> Term = Parse.Chain(Multiply.Or(Divide), Operand, MakeBinary);

        public static readonly TextParser<Expression> ExpressionParser = Parse.Chain(Add.Or(Subtract), Term, MakeBinary);


    }
}
