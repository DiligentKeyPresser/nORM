using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using static ExpLess.PartialEvaluator;
using System;

namespace ExpLess.Test
{

    public abstract class BinaryOpTest<OperandType>
    {
        private readonly Expression Constants;
        private readonly Expression ConstantInstanceFieldAccess;
        private readonly Expression ComplexParameterized;
        protected readonly OperandType i;
        protected OperandType prop1 { get; set; }

        protected abstract OperandType getConstant1();
        protected abstract OperandType getConstant2();
        protected abstract OperandType getConstant3();

        protected BinaryOpTest(ExpressionType NodeType)
        {
            i = getConstant3();
            Expression
                c1 = Expression.Constant(getConstant1()),
                c2 = Expression.Constant(getConstant2()),
                f1 = Expression.MakeMemberAccess(Expression.Constant(this), GetType().GetField("i", BindingFlags.NonPublic | BindingFlags.Instance)),
                p1 = Expression.Parameter(GetType());

            Constants = Expression.MakeBinary(NodeType, c1, c2);
            ConstantInstanceFieldAccess = Expression.MakeBinary(NodeType, c1, f1);
            ComplexParameterized = Expression.MakeBinary(NodeType, c1, Expression.MakeMemberAccess(p1, GetType().GetProperty("prop1", BindingFlags.NonPublic | BindingFlags.Instance)));
        }

        [TestMethod]
        public void CheckForConstants() => Assert.IsTrue(PreEvaluate(Constants) is ConstantExpression, "An operation with constant operands has not been transformed to constant expression.");

#warning make a test for simple parameters
        [TestMethod]
        public virtual void CheckForComplexParameterized() => Assert.AreEqual(ComplexParameterized, PreEvaluate(ComplexParameterized), "Parameter access expression should not be modified, but the result is not reference equal to the input.");

#warning make tests for deeper closure contexts
#warning make tests for static objects
        [TestMethod]
        public void CheckForConstantInstanceFieldAccess() => Assert.IsTrue(PreEvaluate(ConstantInstanceFieldAccess) is ConstantExpression, "An operation with constant instance fiels access has not been transformed to constant expression.");
    }

    public class BinaryOpTest : BinaryOpTest<int>
    {
        protected BinaryOpTest(ExpressionType NodeType) : base(NodeType) { }

        protected override int getConstant1() => 9;
        protected override int getConstant2() => 2;
        protected override int getConstant3() => 5;
    }

    public class BoolBinaryOpTest : BinaryOpTest<bool>
    {
        protected BoolBinaryOpTest(ExpressionType NodeType) : base(NodeType)
        {
            node_type = NodeType;
        }

        private readonly ExpressionType node_type;

        protected override bool getConstant1() => true;
        protected override bool getConstant2() => false;
        protected override bool getConstant3() => true;

        [TestMethod]
        public override void CheckForComplexParameterized()
        {
            var p1 = Expression.Parameter(GetType());

            var ComplexParameterized_true_1 = Expression.MakeBinary(node_type, 
                Expression.Constant(true), Expression.MakeMemberAccess(p1, GetType().GetProperty("prop1", BindingFlags.NonPublic | BindingFlags.Instance)));
            var exp_true_1 = PreEvaluate(ComplexParameterized_true_1);
            var res_true_1 = exp_true_1 is ConstantExpression;
            var val_true_1 = res_true_1 ? (bool)(exp_true_1 as ConstantExpression).Value == true : false;
            
            var ComplexParameterized_false_1 = Expression.MakeBinary(node_type,
                Expression.Constant(false), Expression.MakeMemberAccess(p1, GetType().GetProperty("prop1", BindingFlags.NonPublic | BindingFlags.Instance)));
            var exp_false_1 = PreEvaluate(ComplexParameterized_false_1);
            var res_false_1 = exp_false_1 is ConstantExpression;
            var val_false_1 = res_false_1 ? (bool)(exp_false_1 as ConstantExpression).Value == true : false;

            var ComplexParameterized_true_2 = Expression.MakeBinary(node_type, Expression.MakeMemberAccess(p1, GetType().GetProperty("prop1", BindingFlags.NonPublic | BindingFlags.Instance)),
                Expression.Constant(true));
            var exp_true_2 = PreEvaluate(ComplexParameterized_true_2);
            var res_true_2 = exp_true_2 is ConstantExpression;
            var val_true_2 = res_true_2 ? (bool)(exp_true_2 as ConstantExpression).Value == true : false;

            var ComplexParameterized_false_2 = Expression.MakeBinary(node_type, Expression.MakeMemberAccess(p1, GetType().GetProperty("prop1", BindingFlags.NonPublic | BindingFlags.Instance)),
                Expression.Constant(false));
            var exp_false_2 = PreEvaluate(ComplexParameterized_false_2);
            var res_false_2 = exp_false_2 is ConstantExpression;
            var val_false_2 = res_false_2 ? (bool)(exp_false_2 as ConstantExpression).Value == true : false;

            switch (node_type)
            {
                case ExpressionType.AndAlso:
                    if (!(!res_true_1 && !res_true_2 && res_false_1 && res_false_2)) Assert.Fail("Wrong branches have been reduced.");
                    if (val_false_1 || val_false_2) Assert.Fail("AND should not return true in one of the operands is false.");
                    break;
                case ExpressionType.OrElse:
                    if (!(res_true_1 && res_true_2 && !res_false_1 && !res_false_2)) Assert.Fail("Wrong branches have been reduced.");
                    if (!val_true_1 || !val_true_2) Assert.Fail("OR should not return false in one of the operands is true.");
                    break;
                case ExpressionType.ExclusiveOr:
                    if (res_true_1 || res_true_2 || res_false_1 || res_false_2) Assert.Fail("XOR should not be reduced to constant if one of operands is expression parameter.");
                    break;
                default:
                    throw new InternalTestFailureException($"Unexpected expression {node_type} in BoolBinaryOpTest.CheckForComplexParameterized() test.");
            }

        }
    }
    
    [TestClass]
    public class ExclusiveOrTest : BoolBinaryOpTest
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
    public class OrElseTest : BoolBinaryOpTest
    {
        public OrElseTest() : base(ExpressionType.OrElse) { }
    }

    [TestClass]
    public class AndAlsoTest : BoolBinaryOpTest
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
    public class CoalesceTest : BinaryOpTest<object>
    {
        public CoalesceTest() : base(ExpressionType.Coalesce) { }

        protected override object getConstant1() => "string 1";

        protected override object getConstant2() => null;

        protected override object getConstant3() => "wopopop";
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