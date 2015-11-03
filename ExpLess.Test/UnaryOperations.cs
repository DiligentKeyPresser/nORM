using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq.Expressions;
using System.Reflection;
using static ExpLess.PartialEvaluator;

namespace ExpLess.Test
{
    public abstract class UnaryOpTest<OperandType>
    {
        protected abstract OperandType getConstant1();

        protected readonly ExpressionType exp_type;

        protected OperandType prop1 { get; set; }

        public UnaryOpTest(ExpressionType Type)
        {
            exp_type = Type;
        }

        protected Expression ParamAccessExpression() => Expression.MakeMemberAccess(Expression.Parameter(GetType()), GetType().GetProperty("prop1", BindingFlags.NonPublic | BindingFlags.Instance));

        [TestMethod]
        public virtual void CheckForConstant() => Assert.IsTrue(PreEvaluate(Expression.MakeUnary(exp_type, Expression.Constant(getConstant1()), null)) is ConstantExpression, "An operation with constant argument has not been transformed to constant expression.");

        [TestMethod]
        public virtual void CheckForParam()
        {
            var e = Expression.MakeUnary(exp_type, ParamAccessExpression(), null);
            Assert.AreEqual(e, PreEvaluate(e), "An operation with unknown argument has been transformed to constant expression.");
        }

    }

    public abstract class UnaryOpTest<OperandType, ResultType> : UnaryOpTest<OperandType>
    {
        public UnaryOpTest(ExpressionType Type) : base(Type) { }

        [TestMethod]
        public override void CheckForConstant() => Assert.IsTrue(PreEvaluate(Expression.MakeUnary(exp_type, Expression.Constant(getConstant1()), typeof(ResultType))) is ConstantExpression, "An operation with constant argument has not been transformed to constant expression.");

        [TestMethod]
        public virtual void CheckForParam()
        {
            var e = Expression.MakeUnary(exp_type, ParamAccessExpression(), typeof(ResultType));
            Assert.AreEqual(e, PreEvaluate(e), "An operation with unknown argument has been transformed to constant expression.");
        }
    }

    public abstract class UnaryIntOpTest : UnaryOpTest<int>
    {
        protected override int getConstant1() => 5;

        public UnaryIntOpTest(ExpressionType Type) : base(Type) { }
    }

    [TestClass]
    public class NotTest : UnaryIntOpTest
    {
        public NotTest() : base (ExpressionType.Not) { }
    }

    [TestClass]
    public class UnaryPlusTest : UnaryIntOpTest
    {
        public UnaryPlusTest() : base(ExpressionType.UnaryPlus) { }
    }

    [TestClass]
    public class NegateTest : UnaryIntOpTest
    {
        public NegateTest() : base(ExpressionType.Negate) { }
    }

    [TestClass]
    public class ConvertTest : UnaryOpTest<int, float>
    {
        public ConvertTest() : base(ExpressionType.Convert) { }

        protected override int getConstant1() => 9;
    }

    [TestClass]
    public class TypeAsTest : UnaryOpTest<string, object>
    {
        public TypeAsTest() : base(ExpressionType.TypeAs) { }

        protected override string getConstant1() => "In CRUD we trust.";
    }

    [TestClass]
    public class ArrayLengthTest : UnaryOpTest<string[]>
    {
        public ArrayLengthTest() : base(ExpressionType.ArrayLength) { }

        protected override string[] getConstant1() => new string[] { "quick", "brown", "fox" };
    }

    [TestClass]
    public class NegateCheckedTest : UnaryIntOpTest
    {
        public NegateCheckedTest() : base(ExpressionType.NegateChecked) { }
    }

    [TestClass]
    public class ConvertCheckedTest : UnaryOpTest<int, float>
    {
        public ConvertCheckedTest() : base(ExpressionType.ConvertChecked) { }

        protected override int getConstant1() => 95;
    }

    [TestClass]
    public class OnesComplementTest : UnaryIntOpTest
    {
        public OnesComplementTest() : base(ExpressionType.OnesComplement) { }
    }
}