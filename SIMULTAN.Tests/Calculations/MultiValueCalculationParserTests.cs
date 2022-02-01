using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Calculations
{
    [TestClass]
    public class MultiValueCalculationParserTests
    {
        [TestMethod]
        public void ParseSimple()
        {
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);

            // a * b
            {
                var expr = parser.ParseFunction("a * b");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionParameter);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionParameter)step.Right;
                Assert.AreEqual("b", right.Symbol);
            }

            // a + b
            {
                var expr = parser.ParseFunction("a + b");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_SUM_REPEAT_COLUMN, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionParameter);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionParameter)step.Right;
                Assert.AreEqual("b", right.Symbol);
            }

            // a + b * c
            {
                var expr = parser.ParseFunction("a + b * c");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_SUM_REPEAT_COLUMN, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionBinary);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var rightStep = (SimMultiValueExpressionBinary)step.Right;
                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, rightStep.Operation);
                Assert.IsTrue(rightStep.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(rightStep.Right is SimMultiValueExpressionParameter);

                var rightLeft = (SimMultiValueExpressionParameter)rightStep.Left;
                Assert.AreEqual("b", rightLeft.Symbol);

                var rightRight = (SimMultiValueExpressionParameter)rightStep.Right;
                Assert.AreEqual("c", rightRight.Symbol);
            }

            // (a + b) * c
            {
                var expr = parser.ParseFunction("(a + b) * c");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionBinary);
                Assert.IsTrue(step.Right is SimMultiValueExpressionParameter);

                var leftStep = (SimMultiValueExpressionBinary)step.Left;
                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_SUM_REPEAT_COLUMN, leftStep.Operation);
                Assert.IsTrue(leftStep.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(leftStep.Right is SimMultiValueExpressionParameter);

                var leftLeft = (SimMultiValueExpressionParameter)leftStep.Left;
                Assert.AreEqual("a", leftLeft.Symbol);

                var leftRight = (SimMultiValueExpressionParameter)leftStep.Right;
                Assert.AreEqual("b", leftRight.Symbol);


                var right = (SimMultiValueExpressionParameter)step.Right;
                Assert.AreEqual("c", right.Symbol);
            }
        }
        [TestMethod]
        public void ParseSimpleWithConstants()
        {
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);

            // a * 2.5
            {
                var expr = parser.ParseFunction("a * 2.5");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionDoubleConstant);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionDoubleConstant)step.Right;
                Assert.AreEqual(2.5, right.Value);
            }

            // a * PI
            {
                var expr = parser.ParseFunction("a * PI");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionDoubleConstant);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionDoubleConstant)step.Right;
                AssertUtil.AssertDoubleEqual(Math.PI, right.Value);
            }
        }
        [TestMethod]
        public void ParseSimpleWithUnary()
        {
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);

            // a * -2.5
            {
                var expr = parser.ParseFunction("a * -2.5");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionUnary);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionUnary)step.Right;
                Assert.AreEqual(MultiValueCalculationUnaryOperation.Negate, right.Operation);
                Assert.IsTrue(right.Operand is SimMultiValueExpressionDoubleConstant);

                var rightOperand = (SimMultiValueExpressionDoubleConstant)right.Operand;
                Assert.AreEqual(2.5, rightOperand.Value);
            }

            // a * -PI
            {
                var expr = parser.ParseFunction("a * -PI");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionUnary);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionUnary)step.Right;
                Assert.AreEqual(MultiValueCalculationUnaryOperation.Negate, right.Operation);
                Assert.IsTrue(right.Operand is SimMultiValueExpressionDoubleConstant);

                var rightOperand = (SimMultiValueExpressionDoubleConstant)right.Operand;
                Assert.AreEqual(Math.PI, rightOperand.Value);
            }

            // a * -Sin(-PI)
            {
                var expr = parser.ParseFunction("a * -Sin(-PI)");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionUnary);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionUnary)step.Right;
                Assert.AreEqual(MultiValueCalculationUnaryOperation.Negate, right.Operation);
                Assert.IsTrue(right.Operand is SimMultiValueExpressionDoubleConstant);

                var rightOperand = (SimMultiValueExpressionDoubleConstant)right.Operand;
                Assert.AreEqual(Math.Sin(-Math.PI), rightOperand.Value);
            }
        }
        [TestMethod]
        public void ParseUnaryWithParameter()
        {
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);

            // a * -2.5
            {
                var expr = parser.ParseFunction("b * -a");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionUnary);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("b", left.Symbol);

                var right = (SimMultiValueExpressionUnary)step.Right;
                Assert.AreEqual(MultiValueCalculationUnaryOperation.Negate, right.Operation);
                Assert.IsTrue(right.Operand is SimMultiValueExpressionParameter);

                var rightOperand = (SimMultiValueExpressionParameter)right.Operand;
                Assert.AreEqual("a", rightOperand.Symbol);
            }
        }

        [TestMethod]
        public void ParseTranspose()
        {
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);

            // a * -2.5
            {
                var expr = parser.ParseFunction("Transpose(a)");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionUnary);
                var step = (SimMultiValueExpressionUnary)mvExpr;

                Assert.AreEqual(MultiValueCalculationUnaryOperation.Transpose, step.Operation);
                Assert.IsTrue(step.Operand is SimMultiValueExpressionParameter);

                var left = (SimMultiValueExpressionParameter)step.Operand;
                Assert.AreEqual("a", left.Symbol);
            }
        }


        [TestMethod]
        public void ParseConstantExpressions()
        {
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);

            // a * Sin(-2.5)
            {
                var expr = parser.ParseFunction("a * Sin(-2.5)");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionDoubleConstant);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionDoubleConstant)step.Right;
                AssertUtil.AssertDoubleEqual(-0.5984721441, right.Value);
            }

            // a * Sin(-(1 + 0.25) * 2)
            {
                var expr = parser.ParseFunction("a * Sin(-(1 + 0.25) * 2)");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionDoubleConstant);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionDoubleConstant)step.Right;
                AssertUtil.AssertDoubleEqual(-0.5984721441, right.Value);
            }
        }
        [TestMethod]
        public void ParseMathConstantExpression()
        {
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);

            // a * Sin(-PI/2)
            {
                var expr = parser.ParseFunction("a * Sin(-PI/2)");
                var mvExpr = MultiValueCalculationParser.Parse(expr);

                Assert.IsTrue(mvExpr is SimMultiValueExpressionBinary);
                var step = (SimMultiValueExpressionBinary)mvExpr;

                Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, step.Operation);
                Assert.IsTrue(step.Left is SimMultiValueExpressionParameter);
                Assert.IsTrue(step.Right is SimMultiValueExpressionDoubleConstant);

                var left = (SimMultiValueExpressionParameter)step.Left;
                Assert.AreEqual("a", left.Symbol);

                var right = (SimMultiValueExpressionDoubleConstant)step.Right;
                AssertUtil.AssertDoubleEqual(-1.0, right.Value);
            }
        }



        [TestMethod]
        public void ParseWrongMethodOnVariable()
        {
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);

            {
                var expr = parser.ParseFunction("a * Sin(b)");
                Assert.ThrowsException<NotSupportedException>(() => { var mvExpr = MultiValueCalculationParser.Parse(expr); });
            }
        }
        [TestMethod]
        public void ParseWrongOperands()
        {
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);

            {
                var expr = parser.ParseFunction("a - b");
                Assert.ThrowsException<NotSupportedException>(() => { var mvExpr = MultiValueCalculationParser.Parse(expr); });
            }

            {
                var expr = parser.ParseFunction("a / b");
                Assert.ThrowsException<NotSupportedException>(() => { var mvExpr = MultiValueCalculationParser.Parse(expr); });
            }
        }
    }
}
