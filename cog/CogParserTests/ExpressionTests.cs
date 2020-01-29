using System;
using CogParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CogParserTests
{
    [TestClass]
    public class ExpressionTests
    {
        [TestMethod]
        public void TestFloat()
        {
            var expr = ExpressionParsers.ParseExpression("53.23");
            var floatExpr = (FloatExpression)expr;
            Assert.IsTrue(floatExpr.Value == 53.23f);
        }

        [TestMethod]
        public void TestInt()
        {
            var expr = ExpressionParsers.ParseExpression("15");
            var intExpr = (IntExpression)expr;
            Assert.IsTrue(intExpr.Value == 15);
        }

        [TestMethod]
        public void TestAddition()
        {
            var expr = ExpressionParsers.ParseExpression("15 + 34");
            var binary = (BinaryExpression)expr;
            Assert.IsTrue(binary.Operator == ExpressionType.Plus);
            Assert.IsTrue(((IntExpression)binary.Left).Value == 15);
            Assert.IsTrue(((IntExpression)binary.Right).Value == 34);
        }

        [TestMethod]
        public void TestNegate()
        {
            var expr = ExpressionParsers.ParseExpression("-15 + 34");
            var binary = (BinaryExpression)expr;
            Assert.IsTrue(binary.Operator == ExpressionType.Plus);

            var left = (NegateExpression)binary.Left;

            Assert.IsTrue(((IntExpression)left.NegatedExpression).Value == 15);
            Assert.IsTrue(((IntExpression)binary.Right).Value == 34);
        }

        [TestMethod]
        public void TestComplex()
        {
            var expr = ExpressionParsers.ParseExpression("15 + 34");
            var binary = (BinaryExpression)expr;
            Assert.IsTrue(binary.Operator == ExpressionType.Plus);
            Assert.IsTrue(((IntExpression)binary.Left).Value == 15);
            Assert.IsTrue(((IntExpression)binary.Right).Value == 34);
        }
    }
}
