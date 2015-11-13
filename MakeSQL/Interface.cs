using System.Collections.Generic;
using System.Text;

namespace MakeSQL
{

#warning separate file
    namespace Internals
    {
        /// <summary>
        /// Composite query component
        /// </summary>
        public interface IBuildable
        {
            Builder Builder { get; }
        }

        /// <summary>
        /// Basic class for composite query components
        /// </summary>
        public abstract class Builder : IBuildable
        {
            internal Builder() { }

            Builder IBuildable.Builder => this;

            internal abstract IEnumerator<string> Compile(QueryFactory LanguageContext);

            internal string Build(QueryFactory LanguageContext)
            {
                var builder = new StringBuilder();
                var enumerator = Compile(LanguageContext);

                while (enumerator.MoveNext())
                    builder.Append(enumerator.Current);
                return builder.ToString();                                    
            }
        }
    }

    public interface ISelectSource : Internals.IBuildable
    {
        
    }

    public abstract class QueryFactory
    {
        public abstract ISelectQuery Select(ISelectSource Source);

        internal virtual string LeftEscapingSymbol => "\"";
        internal virtual string RightEscapingSymbol => "\"";
    }

    /// <summary>
    /// A query
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// Builds a query from scratch
        /// </summary>
        /// <param name="LanguageContext"> A factory corresponding to current SQL flavor </param>
        string Build(QueryFactory LanguageContext);
    }

    public interface ISelectQuery : IQuery, Internals.IBuildable
    {
        ISelectQuery Clone();
        ISelectQuery Where(string Clause);
        IQuery MakeCount();
        IQuery MakeLongCount();
        IQuery MakeAny();
    }




}
