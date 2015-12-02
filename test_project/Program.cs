using nORM;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace test_project
{
    public interface ITable1Row : ITable1RowData
    {
        [Field("id")]
        int ID { get; }
    }

    public interface ITable1RowData
    {
        [Field("count")]
        int Count { get; }

        [Field("name")]
        string Name { get; }
    }

    public class Ins : ITable1RowData
    {
        public int Count => 222;
        public string Name => "two";
    }

    public interface ITable2Row 
    {
        [Field("id")]
        int ID { get; }

        [Field("data")]
        int? data { get; }
    }

    public interface ITestTable : ITable<ITable1Row>
    {

    }


    public interface ITestDB : IDatabase
    {
        [Table(".table1")]
        ITestTable Table1 { get; }

        [Table(".table2")]
        ITable<ITable2Row> Table2 { get; }
    }

    class Program
    {
        

        static void Main(string[] args)
        {
            var CopyDatabase = Database<ITestDB>.Inflate(new SqlServerConnector("federalcom", "normtest", "normuser", "normpass"));
            CopyDatabase.BeforeCommandExecute += Console.WriteLine;

        //    CopyDatabase.Table1.Insert(new { t = 4, y = 8 });

            CopyDatabase.Table1.Insert(Enumerable.Range(0,2).Select(i=>new { Count = i, Name = i.ToString() }));

            Console.ReadKey();
        }
    }

}
