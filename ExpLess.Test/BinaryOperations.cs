using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using static ExpLess.PartialEvaluator;

namespace ExpLess.Test
{

    public class BinaryOpTest
    {
        private readonly Expression Constants;
        private readonly Expression ConstantInstanceFieldAccess;
        private readonly Expression ComplexParameterized;
        private readonly int i = 5;

        protected BinaryOpTest(ExpressionType NodeType)
        {
            Expression
                c1 = Expression.Constant(9),
                c2 = Expression.Constant(2),
                f1 = Expression.MakeMemberAccess(Expression.Constant(this), typeof(BinaryOpTest).GetField("i", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)),
                p1 = Expression.Parameter(typeof(List<int>));

            Constants = Expression.MakeBinary(NodeType, c1, c2);
            ConstantInstanceFieldAccess = Expression.MakeBinary(NodeType, c1, f1);
            ComplexParameterized = Expression.MakeBinary(NodeType, c1, Expression.MakeMemberAccess(p1, typeof(List<int>).GetProperty("Count")));
        }

        [TestMethod]
        public void CheckForConstants() => Assert.IsTrue(PreEvaluate(Constants) is ConstantExpression, "An operation with constant operands has not been transformed to constant expression.");

#warning make a test for simple parameters
        [TestMethod]
        public void CheckForComplexParameterized() => Assert.AreEqual(ComplexParameterized, PreEvaluate(ComplexParameterized), "Parameter access expression should not be modified, but the result is not reference equal to the input.");

#warning make tests for deeper closure contexts
#warning make tests for static objects
        [TestMethod]
        public void CheckForConstantInstanceFieldAccess() => Assert.IsTrue(PreEvaluate(ConstantInstanceFieldAccess) is ConstantExpression, "An operation with constant instance fiels access has not been transformed to constant expression.");
    }


    [TestClass]
    public class ExclusiveOrTest : BinaryOpTest
    {
        public ExclusiveOrTest() : base(ExpressionType.ExclusiveOr) { }
    }

    [TestClass]
    public class AndTest : BinaryOpTest
    {
        public AndTest() : base(ExpressionType.And) { }
    }

    [TestClass]
    public class OrTest : BinaryOpTest
    {
        public OrTest() : base(ExpressionType.Or) { }
    }

    [TestClass]
    public class NotEqualTest : BinaryOpTest
    {
        public NotEqualTest() : base(ExpressionType.NotEqual) { }
    }

    [TestClass]
    public class EqualTest : BinaryOpTest
    {
        public EqualTest() : base(ExpressionType.Equal) { }
    }


    [TestClass]
    public class OrElseTest : BinaryOpTest
    {
        public OrElseTest() : base(ExpressionType.OrElse) { }
    }

    [TestClass]
    public class AndAlsoTest : BinaryOpTest
    {
        public AndAlsoTest() : base(ExpressionType.AndAlso) { }
    }

    [TestClass]
    public class GreaterThanOrEqualTest : BinaryOpTest
    {
        public GreaterThanOrEqualTest() : base(ExpressionType.GreaterThanOrEqual) { }
    }

    [TestClass]
    public class GreaterThanTest : BinaryOpTest
    {
        public GreaterThanTest() : base(ExpressionType.GreaterThan) { }
    }

    [TestClass]
    public class LessThanOrEqualTest : BinaryOpTest
    {
        public LessThanOrEqualTest() : base(ExpressionType.LessThanOrEqual) { }
    }

    [TestClass]
    public class LessThanTest : BinaryOpTest
    {
        public LessThanTest() : base(ExpressionType.LessThan) { }
    }

    [TestClass]
    public class ModuloTest : BinaryOpTest
    {
        public ModuloTest() : base(ExpressionType.Modulo) { }
    }

    [TestClass]
    public class DivideTest : BinaryOpTest
    {
        public DivideTest() : base(ExpressionType.Divide) { }
    }

    [TestClass]
    public class MultiplyTest : BinaryOpTest
    {
        public MultiplyTest() : base(ExpressionType.Multiply) { }
    }

    [TestClass]
    public class RightShiftTest : BinaryOpTest
    {
        public RightShiftTest() : base(ExpressionType.RightShift) { }
    }

    [TestClass]
    public class LeftShiftTest : BinaryOpTest
    {
        public LeftShiftTest() : base(ExpressionType.LeftShift) { }
    }

    [TestClass]
    public class ArrayIndexTest : BinaryOpTest
    {
        public ArrayIndexTest() : base(ExpressionType.ArrayIndex) { }
    }

    [TestClass]
    public class CoalesceTest : BinaryOpTest
    {
        public CoalesceTest() : base(ExpressionType.Coalesce) { }
    }

    [TestClass]
    public class AddTest : BinaryOpTest
    {
        public AddTest() : base(ExpressionType.Add) { }
    }

    [TestClass]
    public class SubstractTest : BinaryOpTest
    {
        public SubstractTest() : base(ExpressionType.Subtract) { }
    }
}