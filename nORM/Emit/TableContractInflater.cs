using System;
using System.Reflection;
using System.Reflection.Emit;

namespace nORM
{
    internal static class TableContractInflater<TableContract, RowContract> where TableContract : ITable<RowContract>
    {
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
                "DBTable_" + TableContractType.Name,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoLayout,
                typeof(Table<RowContract>));

            var BaseConstructor = BasicTableType.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                null, TypeOf.TableArgumentSet, null);
            var constructor = ClassBuilder.DefineConstructor(MethodAttributes.Public, BaseConstructor.CallingConvention, TypeOf.RowArgumentSet);
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

        public static TableContract Inflate(DatabaseContext ConnectionContext, string TableName) => (TableContract)TableConstructor.Invoke(new object[] { ConnectionContext, TableName });
    }
}