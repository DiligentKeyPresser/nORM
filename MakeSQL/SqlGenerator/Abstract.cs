namespace MakeSQL
{
    public abstract class QueryFactory
    {
        internal QueryFactory() { }

        /// <summary>
        /// Creates a simple select query which can be extended or used as a subquery
        /// </summary>
        /// <param name="Source"> A qualified name of table/view or</param>
        public abstract ISelectQuery Select(ISelectSource Source);

        internal virtual string LeftEscapingSymbol => "\"";
        internal virtual string RightEscapingSymbol => "\"";
    }
}