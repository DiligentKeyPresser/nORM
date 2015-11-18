namespace MakeSQL
{
    /// <summary> Anything to select from </summary>
    public interface ISelectSource
    {
        /// <summary> Builds FROM clause </summary>
        Builder SourceDefinion { get; }
    }

    /// <summary> Anything to insert from </summary>
    public interface IInsertSource
    {
        Builder InsertSourceDefinion { get; }
    }

    /// <summary> Anything to use in a SELECT clause </summary>
    public interface IColumnDefinion : IUnnamedColumnDefinion
    {
        Builder NamedColumnDefinion { get; }
    }

    /// <summary> Anything to use as computed column definion </summary>
    public interface IUnnamedColumnDefinion
    {
        /// <summary> The column definion builder </summary>
        Builder ColumnDefinion { get; }
    }

    /// <summary> Anything to use as a standalone query </summary>
    public interface IQuery
    {
        /// <summary> Builds a query text </summary>
        Builder Query { get; }
    }
}
