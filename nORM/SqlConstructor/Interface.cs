﻿using System;

namespace nORM
{
    namespace SQL
    {
        internal abstract class Query
        {
            // The produced string for any particular query can be reused any time, 
            // but it's more likely to be usen one time only.
            // So let's keep it as a WeakReference and let GC to eat it up. 
            private volatile WeakReference<string> sql_string = null;
            private readonly object sql_cache_sync = new object();

            public string GetText()
            {
                lock (sql_cache_sync)
                {
                    string stored_query = null;
                    if (sql_string == null)
                    {
                        var new_sql = Build();
                        sql_string = new WeakReference<string>(new_sql);
                        return new_sql;
                    }
                    if (!sql_string.TryGetTarget(out stored_query))
                    {
                        var new_sql = Build();
                        sql_string.SetTarget(new_sql);
                        return new_sql;
                    }
                    return stored_query;
                }
            }

            protected abstract string Build();
        }

        internal abstract class SelectQuery: Query
        {

        }
    }
}