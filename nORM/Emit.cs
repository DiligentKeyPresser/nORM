using MakeSQL;
using nORM.SQL;
using System;
using System.Collections.Generic;
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
        public static readonly Type IEnumerable_generic = typeof(IEnumerable<>);
        public static readonly Type ITable_generic = typeof(ITable<>);
        public static readonly Type TableContractInflater = typeof(TableContractInflater<,>);
        public static readonly Type IInsertable = typeof(IInsertable<>);
        
        [Obsolete("old", true)]
        public static readonly Type IQueryFactory = typeof(IQueryFactory);

        /// <summary>
        /// Массив типов аргументов конструктора контекста БД
        /// </summary>
        public static readonly Type[] DBContextArgumentSet = new Type[] { Connector };

        /// <summary>
        /// Basic table constructor arguments
        /// </summary>
        public static readonly Type[] TableArgumentSet = new Type[] { DatabaseContext, typeof(QualifiedIdentifier) };

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
                typeof(DatabaseContext<DbContract>), new Type[] { ContractType });

#warning проверить чтобы все члены были размечены
#warning хорошо бы проверять контракт целиком, чтобы избежать проверок в рантайме

            // генерируем конструктор 
            var BaseConstructor = typeof(DatabaseContext<DbContract>).GetConstructor(
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

            foreach (var TableProperty in DatabaseContext<DbContract>.Tables)
            {
#warning move outside    
                if (TableProperty.CanWrite) 
                    throw new InvalidContractException(ContractType, string.Format("table property ({0}) must be readonly", TableProperty.Name));

                var field = ClassBuilder.DefineField("__table_" + TableProperty.Name, TableProperty.PropertyType, FieldAttributes.InitOnly | FieldAttributes.Private);

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
}