using System;
using System.Reflection;
using System.Reflection.Emit;

namespace nORM
{
    internal abstract class DatabaseRow { }

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

                var field = ClassBuilder.DefineField("__field_" + ColumnInfo.PropertyMetadata.Name, ColumnInfo.PropertyMetadata.PropertyType, FieldAttributes.InitOnly | FieldAttributes.Private);

                consgen.Emit(OpCodes.Ldarg_0);                              // 'this' for stfld
                consgen.Emit(OpCodes.Ldarg_1);                              // input array
                consgen.Emit(OpCodes.Ldc_I4, field_number);                 // array index
                consgen.Emit(OpCodes.Ldelem_Ref);                           // load input array element
                
                // now value is on the top of stack and 'this' is under.

                Label NotNull = consgen.DefineLabel();
                Label End = consgen.DefineLabel();

                consgen.Emit(OpCodes.Dup);                                  // duplication for the DbNull check  
                consgen.Emit(OpCodes.Ldnull);                               // static field of DBNull
                consgen.Emit(OpCodes.Ldfld, TypeOf.DBNullField);            // DBNull value
                consgen.Emit(OpCodes.Ceq);                                  // check for equality

                // now 1 or 0 is over the previous stack

                consgen.Emit(OpCodes.Brfalse_S, NotNull);                   // if Value != DBNull goto ...

                if (Nullable.GetUnderlyingType(ColumnInfo.ColumnType) != null)
                {
                    consgen.Emit(OpCodes.Pop);                              // else get rid of DBNull value. 'this' is still on the stack
                    consgen.Emit(OpCodes.Pop);                              // remove 'this' from the stack
                    consgen.Emit(OpCodes.Br_S, End);                        // leave the field with the default value
                }
                else
                {
                    consgen.Emit(OpCodes.Ldstr, $"Invalid contract {ContractType.Name}: 'NULL' value in a non-nullable field '{ColumnInfo.FieldName}'.");
                    consgen.Emit(OpCodes.Newobj, typeof(InvalidContractException).GetConstructor(TypeOf.one_string_argument));
                    consgen.Emit(OpCodes.Throw);
                }

                consgen.MarkLabel(NotNull);                                 // so, Value != DBNull

                if (ColumnInfo.ColumnType.IsValueType)
                    consgen.Emit(OpCodes.Unbox_Any, ColumnInfo.ColumnType); // < unbox the value if it is boxed >

                consgen.Emit(OpCodes.Stfld, field);                         // then store the value into the field
                consgen.MarkLabel(End);

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