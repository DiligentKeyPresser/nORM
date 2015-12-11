using ExpLess;
using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace timetest
{
    class Program
    {
        private static Expression Convert<T>(Expression<Func<T, bool>> a) => a;

        static void Main(string[] args)
        {
         //   var t = 

            var E = Convert<int>(i =>i * 56 + (i - 1) > 0.5);

            new DiscriminatedExpression(E);

            var s = new Stopwatch();
            s.Reset();
            s.Start();
            for (int i = 0; i < 1000; i++)
            {
                var e = new DiscriminatedExpression(E);
                var ev = e.PreEvaluate;
                var r = ev.Build;

            }
            s.Stop();
            Console.WriteLine(s.Elapsed);

            var t = new Stopwatch();
            t.Reset();
            t.Start();
            for (int i = 0; i < 100000; i++)
            {
                var e = new DiscriminatedExpression(E);
                var ev = e.PreEvaluate;
                var r = ev.Build;
            }
            t.Stop();
            Console.WriteLine(t.Elapsed);

            Debugger.Break();
        }
    }
}
