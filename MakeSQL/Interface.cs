using MakeSQL.Internals;
using System.Collections.Generic;
using System.Text;

namespace MakeSQL
{

#warning separate file
    namespace Internals
    {
#warning probably we dont need this anymore
        public interface IBuildable
        {
            Builder Builder { get; }
        }

        public abstract class Builder : IBuildable
        {
            internal Builder() { }

            Builder IBuildable.Builder => this;

            internal abstract IEnumerator<string> Compile(SQLContext LanguageContext);

            internal string Build(SQLContext LanguageContext)
            {
                var builder = new StringBuilder();
                var enumerator = Compile(LanguageContext);

                while (enumerator.MoveNext())
                    builder.Append(enumerator.Current);
                return builder.ToString();                                    
            }
        }
    }

    public interface ISelectSource : IBuildable { }

    public interface IColumnDefinion : IBuildable { }

    public interface IQuery
    {
        /// <summary> Builds a query text </summary>
        /// <param name="LanguageContext"> A factory corresponding to current SQL flavor </param>
        string Build(SQLContext LanguageContext);
    }
}
