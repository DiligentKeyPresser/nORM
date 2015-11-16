using System.Collections.Generic;
using System.Text;

namespace MakeSQL
{
    /// <summary> Basic class for query elements. </summary>
    public abstract class Buildable
    {
        internal Buildable() { }

        internal abstract IEnumerator<string> Compile(SQLContext LanguageContext);

        /// <summary> Transforms the current buildable object into an SQL command text </summary>
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
