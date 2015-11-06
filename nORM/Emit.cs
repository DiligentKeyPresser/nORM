using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

// Динамические типы данных

// Данный атрибут необходим  для разделения внутренних методов с динамической сборкой
[assembly: InternalsVisibleTo("DynamicDBTypeAssembly")]

namespace nORM
{
    /// <summary>
    /// Служебный класс для хранения ссылки на динамическую сборку.
    /// Имя сборки должно совпадать с именем в атрибуте InternalsVisibleTo.
    /// </summary>
    internal static class DbAss
    {
        // класс необходим потому, что при хранении ссылок внутри Database<DbContract> будет создана
        // отдельная сборка на каждый контракт, а это не позволит использовать атрибут InternalsVisibleTo,
        // что приведет к необходимости сделать все внутренние члены публичными.

        public static readonly AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("DynamicDBTypeAssembly"), AssemblyBuilderAccess.Run);
        public static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
    }

    /// <summary>
    /// Кеш ссылок на типы
    /// </summary>
    internal static class TypeOf
    {
        public static readonly Type Bool = typeof(bool);
        public static readonly Type Int32 = typeof(int);
        public static readonly Type Int16 = typeof(short);
        public static readonly Type String = typeof(string);
        public static readonly Type Connector = typeof(Connector);
        public static readonly Type DatabaseContext = typeof(DatabaseContext);
        public static readonly Type DatabaseRow = typeof(DatabaseRow);
        public static readonly Type TableAttribute = typeof(TableAttribute);
        public static readonly Type FieldAttribute = typeof(FieldAttribute);
        public static readonly Type Queryable = typeof(Queryable);
        public static readonly Type Expression_generic = typeof(Expression<>);
        public static readonly Type IQueryable_generic = typeof(IQueryable<>);


        /// <summary>
        /// Массив типов аргументов конструктора контекста БД
        /// </summary>
        public static readonly Type[] DBContextArgumentSet = new Type[] { Connector };

        /// <summary>
        /// Массив типов аргументов конструктора таблицы
        /// </summary>
        public static readonly Type[] TableArgumentSet = new Type[] { DatabaseContext, String };

        /// <summary>
        /// Массив типов аргументов конструктора строки
        /// </summary>
        public static readonly Type[] RowArgumentSet = new Type[] { typeof(object[]) };
    }

    /// <summary>
    /// Служебный класс для реализации контрактов баз данных
    /// </summary>
    /// <typeparam name="DbContract"> Контракт базы данных, который необходимо реализовать </typeparam>
    public static class Database<DbContract> where DbContract : class, IDatabase
    {
        // Статические члены не внесены в DatabaseContext чтобы иметь возможность хранить динамический тип в статике
        // не делая класс DatabaseContext generic'ом

        private static readonly Type ProxyType;

        static Database()
        {
            var ContractType = typeof(DbContract);
            if (!ContractType.IsInterface) throw new InvalidContractException(ContractType, "contract must be an interface.");

            TypeBuilder ClassBuilder = DbAss.moduleBuilder.DefineType(
                "DBDynamic_" + ContractType.Name, 
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoLayout, 
                TypeOf.DatabaseContext, new Type[] { ContractType });

#warning проверить чтобы все члены были размечены
#warning хорошо бы проверять контракт целиком, чтобы избежать проверок в рантайме

            // генерируем конструктор 
            var BaseConstructor = TypeOf.DatabaseContext.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, 
                null, TypeOf.DBContextArgumentSet, null);
            
            var constructor = ClassBuilder.DefineConstructor(
                MethodAttributes.Public, BaseConstructor.CallingConvention, 
                TypeOf.DBContextArgumentSet);

            var consgen = constructor.GetILGenerator();

            // base constructor
            // this
            consgen.Emit(OpCodes.Ldarg_0);
            // 1 argument
            consgen.Emit(OpCodes.Ldarg_1);
            consgen.Emit(OpCodes.Call, BaseConstructor);

            // генерируем свойства - таблицы

            foreach (var TableProperty in ContractType.GetProperties().Where(prop => Attribute.IsDefined(prop, TypeOf.TableAttribute)))
            {             
                if (TableProperty.CanWrite) 
                    throw new InvalidContractException(ContractType, string.Format("table property ({0}) must be readonly", TableProperty.Name));

#warning убеждаться что свойства имеют подходящий тип (в них можно записать Table<>)
                
                var TAttr = Attribute.GetCustomAttribute(TableProperty, TypeOf.TableAttribute) as TableAttribute;
                
                var Tableconstructor = TableProperty.PropertyType.GetConstructor(
                    BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, 
                    null, TypeOf.TableArgumentSet, null);

                var field = ClassBuilder.DefineField("__table_" + TableProperty.Name, TableProperty.PropertyType, FieldAttributes.InitOnly | FieldAttributes.Private);
                
                consgen.Emit(OpCodes.Ldarg_0); // для stfld
                consgen.Emit(OpCodes.Ldarg_0); // для конструктора
                consgen.Emit(OpCodes.Ldstr, TAttr.GetFullTableName());
                consgen.Emit(OpCodes.Newobj, Tableconstructor);
                consgen.Emit(OpCodes.Stfld, field);

                var prop = ClassBuilder.DefineProperty(TableProperty.Name, TableProperty.Attributes, TableProperty.PropertyType, null);
                
                var getter = ClassBuilder.DefineMethod(
                    "get_" + TableProperty.Name, 
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual, 
                    TableProperty.PropertyType, Type.EmptyTypes);

                var gettergen = getter.GetILGenerator();

                gettergen.Emit(OpCodes.Ldarg_0); // this
                gettergen.Emit(OpCodes.Ldfld, field);
                gettergen.Emit(OpCodes.Ret);

                prop.SetGetMethod(getter);
            }

            consgen.Emit(OpCodes.Ret);
            ProxyType = ClassBuilder.CreateType();
        }

        public static DbContract Inflate(Connector Connection)
        {
            return Activator.CreateInstance(ProxyType, Connection) as DbContract;
        }
    }

    internal static class RowContractInflater<RowContract>
    {
        public static Type RowType { get; }
        private static ConstructorInfo RowConstructor;

        static RowContractInflater()
        {
            var ContractType = typeof(RowContract);

#warning Вынести проверку контракта в конструирование базы данных
            if (!ContractType.IsInterface) throw new InvalidContractException(ContractType, "contract must be an interface.");
#warning проверить чтобы все члены были размечены

#warning А надо ли от DatabaseRow наследоваться?
            TypeBuilder ClassBuilder = DbAss.moduleBuilder.DefineType(
                "DBRow_" + typeof(RowContract).Name, 
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass | TypeAttributes.AutoLayout, 
                TypeOf.DatabaseRow, new Type[] { ContractType });

            // генерируем конструктор 
            var constructor = ClassBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, TypeOf.RowArgumentSet);
            var consgen = constructor.GetILGenerator();

            // генерируем свойства
#warning порядок полей гарантирован???
            var FieldProperties = ContractType.GetProperties().Where(prop => Attribute.IsDefined(prop, TypeOf.FieldAttribute)).ToArray();
            for (int field_number = 0; field_number < FieldProperties.Length; field_number++ )
            {
                var FieldProperty = FieldProperties[field_number];
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
            RowType = ClassBuilder.CreateType();

            RowConstructor = RowType.GetConstructor(TypeOf.RowArgumentSet);
        }

        public static RowContract Inflate(object[] data)
        {
            return (RowContract)RowConstructor.Invoke(new object[] { data });
        }
    }
}