using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DataWF.Collections.Query;

public static class QQueryExtensions
{
    public static IQQuery Where(this IQueryProvider queryable, string query) => queryable.Query().Where(query);
    public static IEnumerable<T> Where<T>(this IQueryProvider<T> queryable, string query) => queryable.Query().Where(query);

    public static IQQuery Where(this IQueryProvider queryable, Expression query) => queryable.Query().Where(query);
    public static IEnumerable<T> Where<T>(this IQueryProvider<T> queryable, Expression query) => queryable.Query().Where(query);

    public static IQQuery<T> Select<T, K>(this IQQuery<T> query, Expression<Func<T, K>> keySelector) =>
        query.Select(keySelector);

    public static IQQuery<T> OrderBy<T, K>(this IQQuery<T> query, Expression<Func<T, K>> keySelector) =>
        query.OrderBy(keySelector);

    public static IQQuery<T> OrderByDescending<T, K>(this IQQuery<T> query, Expression<Func<T, K>> keySelector) =>
        query.OrderByDescending(keySelector);

    
}
