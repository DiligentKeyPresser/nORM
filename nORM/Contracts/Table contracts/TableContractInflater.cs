﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MakeSQL;

namespace nORM
{
    /// <summary>
    /// Creates instances of the given table contract
    /// </summary>
    /// <typeparam name="TableContract"> User-defined table contract </typeparam>
    /// <typeparam name="RowContract"> Row contract, extracted from the table contract </typeparam>
    internal static class TableContractInflater<TableContract, RowContract> where TableContract : ITable<RowContract>
    {
        // RowContract can be inferred from TableContract, but explicit constraint saves a lot of efforts, so i stucked at this approach.

        private static readonly ConstructorInfo TableConstructor;

        static TableContractInflater()
        {
            var TableContractType = typeof(TableContract);
            var RowContractType = typeof(RowContract);
            var BasicTableType = typeof(Table<RowContract>);

#warning Вынести проверку контракта в конструирование базы данных
            if (!TableContractType.IsInterface) throw new InvalidContractException(TableContractType, "table contract must be an interface.");
            if (!RowContractType.IsInterface) throw new InvalidContractException(RowContractType, "row contract must be an interface.");

            TypeBuilder ClassBuilder = DbAss.moduleBuilder.DefineType(
                $"DBTable_{TableContractType.Name}_{RowContractType.Name}",
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoLayout,
                BasicTableType, new Type[] { typeof(TableContract) });

            var BaseConstructor = BasicTableType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                null, TypeOf.TableArgumentSet, null);
            var constructor = ClassBuilder.DefineConstructor(MethodAttributes.Public, BaseConstructor.CallingConvention, TypeOf.TableArgumentSet);
            var consgen = constructor.GetILGenerator();

            // base constructor
            // this
            consgen.Emit(OpCodes.Ldarg_0);
            // 2 arguments
            consgen.Emit(OpCodes.Ldarg_1);
            consgen.Emit(OpCodes.Ldarg_2);
            consgen.Emit(OpCodes.Call, BaseConstructor);

#warning insert additional operations here

            consgen.Emit(OpCodes.Ret);
            var TableType = ClassBuilder.CreateType();
            TableConstructor = TableType.GetConstructor(TypeOf.TableArgumentSet);
        }

        /// <summary>
        /// Creates an instance of the table contract.
        /// </summary>
        /// <param name="ConnectionContext"> Database context to send the query </param>
        /// <param name="TableName"> The name of the table in the database </param>
        public static TableContract Inflate(DatabaseContext ConnectionContext, QualifiedIdentifier TableName) => (TableContract)TableConstructor.Invoke(new object[] { ConnectionContext, TableName });
    }

    internal static class TableContractHelpers
    {
        /// <summary> Checks if the given table is an ITable itself </summary>
        private static bool IsBasicTableContract(Type Contract) => Contract.IsGenericType && Contract.GetGenericTypeDefinition() == TypeOf.ITable_generic;

        /// <summary> Finds an `ITable` interface in the given table contract </summary>
        internal static Type ExtractBasicTableInterface(Type TableContract) => IsBasicTableContract(TableContract) ? TableContract : TableContract.GetInterfaces().First(IsBasicTableContract);
    }
}