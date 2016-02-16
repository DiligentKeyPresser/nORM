using System;
using System.Collections.Generic;

namespace MakeSQL
{
    /// <summary>
    /// Represents local alias.
    /// (Like in 'AS' clause of a JOIN)
    /// </summary>
    internal sealed class LocalToken { }

    /// <summary>
    /// Converts LocalTokens into the string representation.
    /// Keeps tokens unique.
    /// </summary>
#warning Not a thread-safe implementation
    internal sealed class LocalTokenizationContext
    {
        private int NextIndex = 0;
        private readonly Dictionary<LocalToken, string> RegisteredTokens = new Dictionary<LocalToken, string>();

        public string this[LocalToken Token]
        {
            get
            {
                if (Token == null) throw new ArgumentNullException("Token");

                string result = null;
                if (!RegisteredTokens.TryGetValue(Token, out result))
#warning This string literal does not guarantee uniqueness
                    result = RegisteredTokens[Token] = "RTN_" + NextIndex++;
                return result;
            }
        } 
    }
}
