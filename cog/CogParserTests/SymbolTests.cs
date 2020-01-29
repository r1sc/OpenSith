using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CogParserTests
{
    [TestClass]
    public class SymbolTests
    {
        [DataTestMethod]
        [DataRow("flex", "cost", 200.0f)]
        [DataRow("int", "mycost", 53)]
        public void ParseFlexSymbolWithAssignment(string symbolType, string varname, object value)
        {
            var symbol = CogParser.SymbolParser.ParseSymbol($"{symbolType}        {varname}={value.ToString().Replace(',','.')}                       local");
            Assert.IsTrue(symbol.Name == varname);
            Assert.IsTrue(symbol.SymbolUse == CogParser.SymbolUse.Local);

            switch (symbolType)
            {
                case "flex":
                    var flexSymbol = (CogParser.SymbolFlex)symbol;
                    Assert.IsTrue(flexSymbol.Value == (float)value);
                    break;
                case "int":
                    var intSymbol = (CogParser.SymbolInt)symbol;
                    Assert.IsTrue(intSymbol.Value == (int)value);
                    break;
                default:
                    Assert.Fail("Symbol type not tested for");
                    break;
            }
        }

        [TestMethod]
        public void ParseFlexSymbolWithoutAssignment()
        {
            var symbol = CogParser.SymbolParser.ParseSymbol("flex        cost                       local");
            var flexSymbol = (CogParser.SymbolFlex)symbol;
            Assert.IsTrue(flexSymbol.Name == "cost");
            Assert.IsTrue(flexSymbol.Value == null);
            Assert.IsTrue(flexSymbol.SymbolUse == CogParser.SymbolUse.Local);
        }

        [TestMethod]
        public void ParseIntSymbolWithoutAssignment()
        {
            var symbol = CogParser.SymbolParser.ParseSymbol("flex        cost                       local");
            var flexSymbol = (CogParser.SymbolFlex)symbol;
            Assert.IsTrue(flexSymbol.Name == "cost");
            Assert.IsTrue(flexSymbol.Value == null);
            Assert.IsTrue(flexSymbol.SymbolUse == CogParser.SymbolUse.Local);
        }
    }
}
