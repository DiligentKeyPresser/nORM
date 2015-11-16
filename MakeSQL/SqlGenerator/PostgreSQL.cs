namespace MakeSQL
{
    public sealed class PostgreSQLContext : SQLContext
    {
        #region Singleton
        private PostgreSQLContext() { }
        public static PostgreSQLContext Singleton { get; } = new PostgreSQLContext();
        #endregion        
    }
}
