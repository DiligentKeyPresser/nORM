using System;
using System.Collections.Generic;
using System.Text;

namespace MakeSQL
{
    /// <summary>
    /// Sql generator for the SQL Command element.
    /// Can be used to build SQL Query in the given SQL context.
    /// Does not cache built strings itself.
    /// </summary>
    public struct Builder
    {
        // Opaque yet thin wrapper of IEnumerator<string>. 
        // Allows to hide an implementation from the library user.

        private readonly Func<SQLContext, IEnumerator<string>> getter;

        internal Builder(Func<SQLContext, IEnumerator<string>> func) { getter = func; }

        internal IEnumerator<string> Compile(SQLContext LanguageContext) => getter(LanguageContext);

        /// <summary> Builds SQL text in the given context </summary>
        /// <param name="LanguageContext"> TSQL, PostgreSQL or other available options </param>
        public string Build(SQLContext LanguageContext)
        {
            var builder = new StringBuilder();

            var enumerator = getter(LanguageContext);
            while (enumerator.MoveNext())
                builder.Append(enumerator.Current);

            return builder.ToString();
        }
    }
}
