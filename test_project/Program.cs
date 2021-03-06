﻿using nORM;
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
            var CopyDatabase = Database<ITestDB>.Inflate(new SqlServerConnector("server", "normtest", "normuser", "normpass"));
            CopyDatabase.BeforeCommandExecute += Console.WriteLine;

            Console.WriteLine(CopyDatabase.Table1.Where(r => r.ID > 2 & r.Count < 10000).Count());

            Console.ReadKey();
        }
    }

}
