using nORM;
using System;
using System.Linq;

namespace test_project
{
    public interface ITable1Row
    {
        [Field(0)]
        int ID { get; }

        [Field(1)]
        int Count { get; }

        [Field(2)]
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
            var CopyDatabase = Database<ITestDB>.Inflate("federalcom", "normtest", "normuser", "normpass");
            CopyDatabase.BeforeCommandExecute += Console.WriteLine;


            Console.WriteLine("count()");
            Console.WriteLine(CopyDatabase.Table1.Count());

            Console.WriteLine("count(...)");
            Console.WriteLine(CopyDatabase.Table1.Count(r=>r.ID > 3));

            Console.WriteLine();
            Console.WriteLine("loop");
            foreach (var r in CopyDatabase.Table1) Console.WriteLine(r.ID + " " + r.Name);

            Console.WriteLine();
            Console.WriteLine("where");
            var filtered = CopyDatabase.Table1.Where(b => b.Count < 8).Where(b => b.ID > 2).ToArray();
            foreach (var r in filtered) Console.WriteLine(r.ID + " " + r.Name);

            Console.WriteLine();
            Console.WriteLine("where.Count()");
            var filteredCount = CopyDatabase.Table1.Where(b => b.Count <= 5).Count();

            Console.WriteLine();
            Console.WriteLine("Take(3)");
            var t = CopyDatabase.Table1.Take(3).Where(r => r.ID != 0).ToArray();
            foreach (var r in t) Console.WriteLine(r.ID + " " + r.Name);

            Console.WriteLine();
            Console.WriteLine("simple select");
            // foreach (var r in CopyDatabase.Брак.Select(r=>r.Text)) Console.WriteLine(r);*/

        }
    }

}
