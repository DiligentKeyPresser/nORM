using System;

namespace MakeSQL
{
    /// <summary> Contains extension methods to build query elements </summary>
    public static class Converters
    {
        /// <summary> Creates a SQL CAST(AS) operator </summary>
        public static Cast cast(this IUnnamedColumnDefinion From, Type As) => new Cast(From, As);

#warning make all possible overloads
        /// <summary> Creates a SQL literal </summary>
        public static Constant literal(this int From) => new Constant(From);

        /// <summary> Creates an inline SQL function call </summary>
        public static FunctionCall invoke(this Function Func, params IUnnamedColumnDefinion[] Args) => new FunctionCall(Func, Args);

        /// <summary> Gives the column a new name </summary>
        public static RenamedColumn name(this IUnnamedColumnDefinion Self, LocalIdentifier Alias) => new RenamedColumn(Self, Alias);

        /// <summary> Gives the subquery a new name </summary>
        public static SubQuery name(this SelectQuery Self, LocalIdentifier Alias) => new SubQuery(Self, Alias);

    }
}
