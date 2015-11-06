using nORM;
using System;
using System.Diagnostics;
using System.Linq;

namespace test_project
{
    public interface ITable1Row
    {
        [Field("id")]
        int ID { get; }

        [Field("count")]
        int Count { get; }

        [Field("name")]
        string Name { get; }
    }


    public interface ITestDB : IDatabase
    {
        [Table("table1")]
        Table<ITable1Row> Table1 { get; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var CopyDatabase = Database<ITestDB>.Inflate(new SqlServerConnector("federalcom", "normtest", "normuser", "normpass"));
            CopyDatabase.BeforeCommandExecute += Console.WriteLine;

            Console.WriteLine("partial evaluation");
            int tri = 3;
            int seven = 7;
            var arrrr = new int[] { 5, 8, 9 };
            int? nint = 8;
            var obj = "sad";
            Func<int, bool> pred = c =>
            {
                nint++;
                Console.WriteLine("1");
                return c > 0;
            };
            // "(tri ^ 2)" => UB???
            // tri << nint??0   - isLifted
          //  Console.WriteLine(CopyDatabase.Table1.Any(r => new List<int>() { Capacity = 180 }.GetHashCode() > 5)); 


         //   Debugger.Break();

            Console.WriteLine("all");
            Console.WriteLine(CopyDatabase.Table1.All(r=>r.ID > 1));

            Console.WriteLine();
            Debugger.Break();

            Console.WriteLine("any");

            Console.WriteLine(CopyDatabase.Table1.Where(r => r.Count > 1).Any());
            Console.WriteLine(CopyDatabase.Table1.Where(r => r.Count > 10000).Any());
            Console.WriteLine(CopyDatabase.Table1.Any(r=>r.Count > 2));

            Console.WriteLine();
            Console.WriteLine("lazyness test");
            var lt1 = CopyDatabase.Table1.OrderBy(r => r.Count);
            Console.WriteLine("foreach");
            Console.WriteLine();
            foreach (var e in lt1) Console.WriteLine(e.Name);
            Console.WriteLine();
            foreach (var e in lt1) Console.WriteLine(e.Name);

            Console.WriteLine();
            Console.WriteLine(CopyDatabase.Table1.Any().ToString());
            Console.WriteLine(CopyDatabase.Table1.First().Name);

            Console.WriteLine("select()");
            var arr = CopyDatabase.Table1.Select(f => f.Name).ToArray();

            Console.WriteLine();
            Console.WriteLine("count()");
            Console.WriteLine(CopyDatabase.Table1.Count());

            Console.WriteLine();
            Console.WriteLine("count(...)");
            Console.WriteLine(CopyDatabase.Table1.Count(r=>r.ID > 3));

            Console.WriteLine();
            Console.WriteLine("LongCount()");
            Console.WriteLine(CopyDatabase.Table1.LongCount());

            Console.WriteLine();
            Console.WriteLine("LongCount(...)");
            Console.WriteLine(CopyDatabase.Table1.LongCount(r => r.ID > 3));

            Console.WriteLine();
            Console.WriteLine("loop");
            foreach (var r in CopyDatabase.Table1) Console.WriteLine(r.ID + " " + r.Name);

            Console.WriteLine();
            Console.WriteLine("where");
            var filtered = CopyDatabase.Table1.Where(b => b.Count < 8).Where(b => b.ID > 2).ToArray();
            foreach (var r in filtered) Console.WriteLine(r.ID + " " + r.Name);

            Console.WriteLine();
            Console.WriteLine("where.Count()");
            Console.WriteLine(CopyDatabase.Table1.Where(b => b.Count <= 5).Count());

            Console.WriteLine();
            Console.WriteLine("Take(3)");
            var t = CopyDatabase.Table1.Take(3).Where(r => r.ID != 0).ToArray();
            foreach (var r in t) Console.WriteLine(r.ID + " " + r.Name);

            Console.WriteLine();
            Console.WriteLine("simple select");
            // foreach (var r in CopyDatabase.Брак.Select(r=>r.Text)) Console.WriteLine(r);*/

            Debugger.Break();
        }
    }

}
