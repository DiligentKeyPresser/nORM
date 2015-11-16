namespace MakeSQL
{
    public interface ISelectSource
    {
        Buildable Definion { get; }
    }

    public interface IColumnDefinion : IUnnamedColumnDefinion
    {
        Buildable Definion { get; }
    }

    public interface IUnnamedColumnDefinion
    {
        Buildable Definion { get; }
    }

    public interface IQuery
    {
        /// <summary> Builds a query text </summary>
        /// <param name="LanguageContext"> A factory corresponding to current SQL flavor </param>
        string Build(SQLContext LanguageContext);
    }
}
