using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace ExpLess.Test
{
    [TestClass]
    public class BasicAssumptions
    {
        private readonly Expression const1 = Expression.Constant(5);
        private readonly Expression const2 = Expression.Constant(5);

        private Expression GetExpression(Expression<Func<int>> source) => source;

        [TestMethod]
        public void EqualityCheck() => Assert.AreNotEqual(const1, const2, "Different expression instances appear to be aqual.");

        [TestMethod]
        public void RuntimeOptimization() => Assert.IsTrue(Expression.Add(const1, const2).ToString().Contains("+"), "Expression is optimized at runtime.");

        [TestMethod]
        public void CompileTimeOptimization() => Assert.IsFalse(GetExpression(() => 5 + 9).ToString().Contains("+"), "Expression is not optimized at compile time.");

    }
}