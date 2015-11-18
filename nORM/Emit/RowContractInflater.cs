using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace nORM
{
    /// <summary> Creates instances of the given row contract </summary>
    internal static class RowContractInflater<RowContract>
    {
        private static readonly ConstructorInfo RowConstructor;

        static RowContractInflater()
        {
            var ContractType = typeof(RowContract);

#warning Вынести проверку контракта в конструирование базы данных
            if (!ContractType.IsInterface) throw new InvalidContractException(ContractType, "contract must be an interface.");
#warning проверить чтобы все члены были размечены

#warning А надо ли от DatabaseRow наследоваться?
            TypeBuilder ClassBuilder = DbAss.moduleBuilder.DefineType(
                "DBRow_" + ContractType.Name,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoLayout,
                TypeOf.DatabaseRow, new Type[] { ContractType });

            // генерируем конструктор 
            var constructor = ClassBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, TypeOf.RowArgumentSet);
            var consgen = constructor.GetILGenerator();

            // генерируем свойства
            for (int field_number = 0; field_number < RowContractInfo<RowContract>.Columns.Count; field_number++)
            {
                var ColumnInfo = RowContractInfo<RowContract>.Columns[field_number];
#warning Вынести проверку контракта в конструирование базы данных
                if (ColumnInfo.PropertyMetadata.CanWrite) throw new InvalidContractException(ContractType, string.Format("row property ({0}) must be readonly", ColumnInfo.PropertyMetadata.Name));

              //  var FieldAttr = Attribute.GetCustomAttribute(ColumnInfo.PropertyMetadata, TypeOf.FieldAttribute) as FieldAttribute;
                var field = ClassBuilder.DefineField("__field_" + ColumnInfo.PropertyMetadata.Name, ColumnInfo.PropertyMetadata.PropertyType, FieldAttributes.InitOnly | FieldAttributes.Private);

                consgen.Emit(OpCodes.Ldarg_0); // для stfld
                consgen.Emit(OpCodes.Ldarg_1);
                consgen.Emit(OpCodes.Ldc_I4, field_number);
                consgen.Emit(OpCodes.Ldelem_Ref);

                if (ColumnInfo.ColumnType.IsValueType)
                    consgen.Emit(OpCodes.Unbox_Any, ColumnInfo.ColumnType);

                consgen.Emit(OpCodes.Stfld, field);

                var prop = ClassBuilder.DefineProperty(ColumnInfo.ContractName, PropertyAttributes.None, ColumnInfo.ColumnType, null);

                var getter = ClassBuilder.DefineMethod(
                    "get_" + ColumnInfo.ContractName,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    ColumnInfo.ColumnType, Type.EmptyTypes);

                var gettergen = getter.GetILGenerator();

                gettergen.Emit(OpCodes.Ldarg_0); // this
                gettergen.Emit(OpCodes.Ldfld, field);
                gettergen.Emit(OpCodes.Ret);

                prop.SetGetMethod(getter);
            }

            consgen.Emit(OpCodes.Ret);
            var RowType = ClassBuilder.CreateType();
            RowConstructor = RowType.GetConstructor(TypeOf.RowArgumentSet);
        }

        /// <summary> Creates a new instance of the row contract </summary>
        /// <param name="data">Objects to store in fields </param>
        public static RowContract Inflate(object[] data) => (RowContract)RowConstructor.Invoke(new object[] { data });
    }
}