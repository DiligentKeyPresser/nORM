using MakeSQL;
using System;
using System.Reflection;

namespace nORM
{
    /// <summary> SQL command notifier. </summary>
    /// <param name="CommandText"> Text of the command. </param>
    public delegate void BasicCommandHandler(string CommandText);

    /// <summary>
    /// Операции которые можно выполнять над базой данных.
    /// Интерфейсы контракта базы данных должны наследоваться от этого интерфейса.
    /// </summary>
    public interface IDatabase
    {
        /// <summary> Событие позволяет производить мониторинг выполняемых с помощью данного контекста запросов к бд. </summary>
        event BasicCommandHandler BeforeCommandExecute;
    }

#warning сделать базовый атрибут с недопустимостью множественного применения
    /// <summary>
    /// Атрибут для разметки контракта базы данных.
    /// Служит для пометки свойства, представляющего собой таблицу базы данных.
    /// Свойство должно возвращать специализацию Table интерфейсом контракта строки таблицы.
    /// Свойство должно быть доступно только для чтения.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class TableAttribute : Attribute
    {
#warning remove
        /// <summary> Internal metfod to extract a table name from database contract dynamically. </summary>
        /// <param name="contract_property"> Property representing the table in the database contract. </param>
        internal static QualifiedIdentifier extract_name_from_property(PropertyInfo contract_property)
        {
            return IsDefined(contract_property, typeof(TableAttribute)) ?
                (GetCustomAttribute(contract_property, typeof(TableAttribute)) as TableAttribute).TableName :
                null;
        }

        /// <summary> Name of the table. </summary>
        internal QualifiedIdentifier TableName => fieldTableName;

        private readonly string fieldTableName;

        /// <summary>
        /// Атрибут для разметки контракта базы данных.
        /// Служит для пометки свойства, представляющего собой таблицу базы данных.
        /// Свойство должно возвращать специализацию Table интерфейсом контракта строки таблицы.
        /// Свойство должно быть только для чтения.
        /// </summary>
        /// <param name="Name"> Optionally qualified name of the table. </param>
        public TableAttribute(string Name) { fieldTableName = Name; }
    }

#warning А нужен ли такой атрибут?
    /// <summary>
    /// Атрибут служит для разметки контракта строки.
    /// Предназначен для указания имени колонки в таблице, которая соответствует каждому свойству объекта.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class FieldAttribute : Attribute
    {
        /// <summary> Column name </summary>
        internal LocalIdentifier ColumnName => fieldColumnName;

        private readonly string fieldColumnName;

        internal FieldAttribute GetCustomAttribute(object p, Type fieldAttribute)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Атрибут служит для разметки контракта строки.
        /// Тип возвращаемого помечаемым свойством результата должен соответствовать типу данных в базе.
        /// </summary>
        /// <param name="column">Имя колонки в таблице</param>
        public FieldAttribute(string column)
        {
            fieldColumnName = column;
        }
    }

#warning корректно реализовать исключение!
    /// <summary>
    /// Исключение, оповещающее о попытке использовать неверный контракт
    /// </summary>
    [Serializable]
    public class InvalidContractException : Exception
    {
        /// <summary>
        /// Тип, который не удалось использовать в качестве контракта
        /// </summary>
        public Type ContractType { get; }

        /// <summary>
        /// Был использован недействительный контракт
        /// </summary>
        /// <param name="type">Тип, нарушающий правила для контрактов</param>
        /// <param name="Message">Причина, по которой контракт не может быть принят</param>
        public InvalidContractException(Type type, string Message)
            : base(string.Format("{0} contract is invalid: {1}", type.FullName, Message))
        {
            ContractType = type;
        }
    }
}