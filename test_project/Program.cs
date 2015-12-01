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
        public int Count => 111;
        public string Name => "one";
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

            Console.WriteLine(CopyDatabase.Table1.InsertReturning<ITable1RowData, int>(new Ins(), CopyDatabase.Table1.Columns.Single(c => c.RealName == "id")));

            Console.ReadKey();
        }
    }

}
