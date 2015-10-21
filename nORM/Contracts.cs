using System;

// Типы для формирования контрактов

namespace nORM
{
    /// <summary>
    /// Операции которые можно выполнять над базой данных.
    /// Интерфейсы контракта базы данных должны наследоваться от этого интерфейса.
    /// </summary>
    public interface IDatabase
    {
        // Интерфейс не замещается абстрактным DatabaseContext поскольку интерфейсы БД должны наследоваться от этого интерфейса

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
        /// <summary>
        /// Имя представляемой таблицы.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Имя схемы, которой таблица сопоставлена в бд. По умолчанию dbo.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Атрибут для разметки контракта базы данных.
        /// Служит для пометки свойства, представляющего собой таблицу базы данных.
        /// Свойство должно возвращать специализацию Table интерфейсом контракта строки таблицы.
        /// Свойство должно быть только для чтения.
        /// </summary>
        /// <param name="Name">Имя представляемой таблицы.</param>
        public TableAttribute(string Name)
        {
            TableName = Name;
            SchemaName = "dbo";
        }
        
        internal string GetFullTableName()
        {
#warning StringBuilder
            return string.Format("[{0}].[{1}]", SchemaName, TableName);
        }
    }

#warning А нужен ли такой атрибут?
    /// <summary>
    /// Атрибут служит для разметки контракта строки.
    /// Предназначен для указания имени колонки в таблице, которая соответствует каждому свойству объекта.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class FieldAttribute : Attribute
    {
        /// <summary>
        /// Номер колонки в таблице
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Атрибут служит для разметки контракта строки.
        /// Тип возвращаемого помечаемым свойством результата должен соответствовать типу данных в базе.
        /// </summary>
        /// <param name="column">Имя колонки в таблице</param>
        public FieldAttribute(string column)
        {
            ColumnName = column;
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