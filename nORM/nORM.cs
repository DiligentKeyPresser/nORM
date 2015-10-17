using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;



#warning сунуть как можно больше проверок в дебаг
#warning материализация в список - вероятно проблема. фикс должен коснуться как материализации, так и метода выполнения в контексте бд

namespace nORM
{
    /// <summary> Обработчик события создания текста SQL команды. Предназначается для мониторинга работы оболочки. </summary>
    /// <param name="CommandText"> Текст созданной команды </param>
    public delegate void BasicCommandHandler(string CommandText);
         
    public abstract class DatabaseContext: IDatabase
    {
        public string Host { get; private set; }
        public string Database { get; private set; }

        private readonly string ConnectionString;

        public event BasicCommandHandler BeforeCommandExecute;

        internal DatabaseContext(string host, string database, string user, string password)
        {
            Host = host;
            Database = database;
            ConnectionString = string.Format("Data Source={0};Initial Catalog={1};Persist Security Info=True;User ID={2};Password={3};", host, database, user, password);
        }

        internal object ExecuteScalar(string Query)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(Query, connection))
            {
                if (BeforeCommandExecute != null) BeforeCommandExecute(Query);

                connection.Open();
                return command.ExecuteScalar();
            }
        }

        internal IEnumerable<RowContract> ExecuteContract<RowContract>(string Query)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(Query, connection))
            {
                if (BeforeCommandExecute != null) BeforeCommandExecute(Query);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    int ColumnCount = reader.FieldCount;
                    var row = new object[ColumnCount];

                    // список нужен, иначе будет возвращаться итератор по уничтоженному IDisposable
                    var result = new List<RowContract>();
#warning нельзя ли заранее узнать размер?
                    while (reader.Read())
                    {
                        reader.GetValues(row);
                        result.Add(RowContractInflater<RowContract>.Inflate(row));
                    }
                    return result;
                }
            }
        }
    }
        
#warning непонятно, нужен ли такой тип
    internal abstract class DatabaseRow { }
    
    internal sealed class RowProvider : IQueryProvider
    {
        #region Одиночка

        private RowProvider() { }

        static RowProvider() { Singleton = new RowProvider(); }

        public static RowProvider Singleton { get; private set; }

        #endregion

        public IQueryable CreateQuery(Expression expression)
        {
#warning выяснить нужен ли, и если нужен реализовать по примеру Execute   
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var mc_expr = expression as MethodCallExpression;

            // проверка структуры дерева выражения, в релизе будем ее опускать
#if DEBUG
            if (mc_expr == null) throw new InvalidProgramException("range query must be a method call.");
            if (mc_expr.Method.DeclaringType != TypeOf.Queryable) throw new InvalidProgramException("query method must be a Queryable member");
#endif
            var const_arg_0 = mc_expr.Arguments[0] as ConstantExpression;
#if DEBUG
            if (const_arg_0 == null) throw new InvalidProgramException("range query method must be applyed to source directly");
            if (!(const_arg_0.Value is RowSource)) throw new InvalidProgramException("range query method must be applyed to RowSource");
#endif 
            var TargetObject = const_arg_0.Value as RowSource;

            var Query = TargetObject as Query<TElement>;
            if (Query == null)
            {
#warning typeof x2
                var Constructor = typeof(Query<TElement>).GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, new Type[] { typeof(Table<TElement>) }, null);
                Query = (Query<TElement>)Constructor.Invoke(new object[] { const_arg_0.Value });
            }

#warning магическая константа
            if (mc_expr.Method.Name == "Where")
            {
                if (!Query.AddWhereCondition(mc_expr.Arguments[1])) goto failed_to_translate;
            }
            else goto failed_to_translate;

            return Query;

        failed_to_translate: 
            // попадаем сюда если пришедший метод не транслируется в SQL
            // и делегируем выполение дальнейшей работы поставщику LinqToObjects
            // в данном месте выражения будет выполнена материализация сущностей
            
            var TheEnumerable = TargetObject as IEnumerable<TElement>;
            if (TheEnumerable != null)
            {
#warning неправильно делать материализацию сразу
#warning toArray - эффективно ли так и работает ли List<TElement>?
                var List = TheEnumerable as List<TElement> ?? TheEnumerable.ToList();
                var Arr = List.AsQueryable<TElement>();
                return Arr.Provider.CreateQuery<TElement>(
                    mc_expr.Update(
                        null,
#warning эффективно ли?
                        new Expression[] { Expression.Constant(Arr) }.Union(mc_expr.Arguments.Skip(1))));
            }
            return null;
        }
            
        /// <remarks href="https://msdn.microsoft.com/ru-ru/library/bb549414(v=vs.110).aspx">
        /// Метод Execute выполняет запросы, возвращающие единственное значение (а не перечислимую последовательность значений). 
        /// Деревья выражений, представляющие запросы, возвращающие перечислимые результаты, выполняются при перечислении объекта IQueryable<T>, 
        /// содержащего соответствующее дерево выражения.
        /// </remarks>
        TResult IQueryProvider.Execute<TResult>(Expression expression) { return (TResult)execute_scalar(expression); }
        object IQueryProvider.Execute(Expression expression) { return execute_scalar(expression); }

        private static object execute_scalar(Expression expression)
        {
            var mc_expr = expression as MethodCallExpression;

            // проверка структуры дерева выражения, в релизе будем ее опускать
#if DEBUG
            if (mc_expr == null) throw new InvalidProgramException("scalar query must be a method call.");
            if (mc_expr.Method.DeclaringType != TypeOf.Queryable) throw new InvalidProgramException("query method must be a Queryable memder.");
#endif 
            var const_arg_0 = mc_expr.Arguments[0] as ConstantExpression;
#if DEBUG
            if (const_arg_0 == null) throw new InvalidProgramException("scalar query method must be applyed to source directly");
            if (!(const_arg_0.Value is RowSource)) throw new InvalidProgramException("scalar query method must be applyed to RowSource"); 
#endif
            var TargetObject = const_arg_0.Value as RowSource;

            // рассматриваем различные скалярные штуки

#warning магическая константа
            if (mc_expr.Method.Name == "Count")
            {
                var SqlQuery = TargetObject.GetSQL();
                SqlQuery.Count();

#warning печален ли избыточный каст?
                return TargetObject.Context.ExecuteScalar(SqlQuery);
            }
            else goto failed_to_translate;


        failed_to_translate:
            // попадаем сюда если пришедший метод не транслируется в SQL
            // и делегируем выполение дальнейшей работы поставщику LinqToObjects

#warning реализовать
            // но уже это совсем другая история
            throw new NotImplementedException();
            /*
            var TheEnumerable = TargetObject as IEnumerable<TElement>;
            if (TheEnumerable != null)
            {
#warning toArray - эффективно ли так и работает ли List<TElement>?
                var List = TheEnumerable as List<TElement> ?? TheEnumerable.ToList();
                var Arr = List.AsQueryable<TElement>();
                return Arr.Provider.CreateQuery<TElement>(
                    mc_expr.Update(
                        null,
#warning эффективно ли?
 new Expression[] { Expression.Constant(Arr) }.Union(mc_expr.Arguments.Skip(1))));
            }        
            return null; */
        }
    }

    /// <summary>
    /// Служебный класс, способный представлять любой запрос к базе данных, возвращающий таблицу
    /// </summary>
    public abstract class RowSource
    {
        // Интерфейс не подходит изза ограничения на область видимости его членов

        abstract internal string GetSQL();

        abstract internal string GetSQLArray();

        internal RowSource(DatabaseContext ConnectionContext) 
        {
            Context = ConnectionContext;
        }

        // Обращение к данному свойству производится довольно часто, стоит сохранить ссылку, а не ходить в провайдер
        internal DatabaseContext Context { get; private set; }
    }

    /// <summary>
    /// Класс для представления любого объекта, из которого можно запросом получать строки - 
    /// таблицы, представления, результаты выполнения функций и других запросов
    /// </summary>
    /// <typeparam name="RowContract"> Тип строк, которые можно получить из данного объекта </typeparam>
    public abstract class RowSource<RowContract> : RowSource, IQueryable<RowContract> 
    {
        protected static readonly Type contract_type = typeof(RowContract);
        /// <summary>
        /// Тип строк, которые можно получить из данного объекта
        /// </summary>
        public Type ElementType { get { return contract_type; } }
        public Expression Expression { get; private set; }
        public IQueryProvider Provider { get { return RowProvider.Singleton; } }
        public RowSource(DatabaseContext ConnectionContext) : base(ConnectionContext)
        {
            Expression = Expression.Constant(this);
        }
        public abstract IEnumerator<RowContract> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

#warning Запрос должен быть немутабельным!!! создавать новый объект на каждый вызов метода модификации!!!
#warning IOrderedQueryable<RowContract> ???
#warning public???
    public class Query<RowContract> : RowSource<RowContract> 
    {
        internal Query(Table<RowContract> Source) : base(Source.Context)
        {
            sql_query = Source.GetSQL();
        }    

        internal bool AddWhereCondition(Expression Predicate) 
        { 
#warning ваще не так 
            return false;
        }

     //   private readonly Plain_TSQL_SelectQuery sql_query;
      /*  internal override Plain_TSQL_SelectQuery GetSQL()
        {
#warning клонировать
#warning когда запросы будут немутабельны можно будет не клонировать

            return sql_query;
        }*/

        public override IEnumerator<RowContract> GetEnumerator()
        {
            // обращение не к GetSQL, а к полю намеренно
            return Context.ExecuteContract<RowContract>(sql_query).GetEnumerator();
        }
    }

    public sealed class Table<RowContract> : RowSource<RowContract> 
    {
        /// <summary>
        /// Имя таблицы в базе данных
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Конструктор для вызова из динамического класса базы данных.
        /// Вручную не вызывается нигде.
        /// </summary>
        internal Table(DatabaseContext ConnectionContext, string TableName)
            :base(ConnectionContext)
        {
            Name = TableName;
            sql_array = new string[] { "SELECT ", "* ", "FROM ", Name, " " };
        } 

        private string[] sql_array;
        private string sql_query = null;

        internal override string[] GetSQLArray() { return sql_array; }
        internal override string GetSQL() { return sql_query ?? (sql_query = string.Concat(sql_array)); }

        public override IEnumerator<RowContract> GetEnumerator()
        {
            return Context.ExecuteContract<RowContract>(GetSQL()).GetEnumerator();
        }
    }
}
