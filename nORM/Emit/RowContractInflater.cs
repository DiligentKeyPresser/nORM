using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace nORM
{
    /// <summary>
    /// Creates instances of the given row contract
    /// </summary>
    internal static class RowContractInflater<RowContract>
    {
        private static readonly ConstructorInfo RowConstructor;
        private static readonly ReadOnlyCollection<PropertyInfo> fContractFields;

        /// <summary>
        /// Gets the fields defined in the subcontract
        /// </summary>
        private static IEnumerable<PropertyInfo> GetFields(Type Subcontract)
        {
            foreach (var f in Subcontract.GetProperties().Where(prop => Attribute.IsDefined(prop, TypeOf.FieldAttribute))) yield return f;
            foreach (var s in Subcontract.GetInterfaces())
                foreach (var f in GetFields(s)) yield return f;
        }

        static RowContractInflater()
        {
            var ContractType = typeof(RowContract);

#warning Вынести проверку контракта в конструирование базы данных
            if (!ContractType.IsInterface) throw new InvalidContractException(ContractType, "contract must be an interface.");
#warning проверить чтобы все члены были размечены

            // We have to extract all the fields from the contract first
            fContractFields = new ReadOnlyCollection<PropertyInfo>(GetFields(ContractType).ToList());

#warning А надо ли от DatabaseRow наследоваться?
            TypeBuilder ClassBuilder = DbAss.moduleBuilder.DefineType(
                "DBRow_" + ContractType.Name,
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoLayout,
                TypeOf.DatabaseRow, new Type[] { ContractType });

            // генерируем конструктор 
            var constructor = ClassBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, TypeOf.RowArgumentSet);
            var consgen = constructor.GetILGenerator();

            // генерируем свойства
            for (int field_number = 0; field_number < fContractFields.Count; field_number++)
            {
                var FieldProperty = fContractFields[field_number];
#warning Вынести проверку контракта в конструирование базы данных
                if (FieldProperty.CanWrite) throw new InvalidContractException(ContractType, string.Format("row property ({0}) must be readonly", FieldProperty.Name));

                var FieldAttr = Attribute.GetCustomAttribute(FieldProperty, TypeOf.FieldAttribute) as FieldAttribute;
                var field = ClassBuilder.DefineField("__field_" + FieldProperty.Name, FieldProperty.PropertyType, FieldAttributes.InitOnly | FieldAttributes.Private);

                consgen.Emit(OpCodes.Ldarg_0); // для stfld
                consgen.Emit(OpCodes.Ldarg_1);
                consgen.Emit(OpCodes.Ldc_I4, field_number);
                consgen.Emit(OpCodes.Ldelem_Ref);

                if (FieldProperty.PropertyType.IsValueType)
                    consgen.Emit(OpCodes.Unbox_Any, FieldProperty.PropertyType);

                consgen.Emit(OpCodes.Stfld, field);

                var prop = ClassBuilder.DefineProperty(FieldProperty.Name, FieldProperty.Attributes, FieldProperty.PropertyType, null);

                var getter = ClassBuilder.DefineMethod(
                    "get_" + FieldProperty.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    FieldProperty.PropertyType, Type.EmptyTypes);

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

        /// <summary>
        /// Gets collection of fields of the contract.
        /// </summary>
        internal static IReadOnlyList<PropertyInfo> ContractFields => fContractFields;

        /// <summary>
        /// Creates a new instance of the row contract
        /// </summary>
        /// <param name="data">Objects to store in fields </param>
        public static RowContract Inflate(object[] data) => (RowContract)RowConstructor.Invoke(new object[] { data });

        
    }

}