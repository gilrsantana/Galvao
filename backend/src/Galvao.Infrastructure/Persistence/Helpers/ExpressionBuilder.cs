using System.Linq.Expressions;
using System.Reflection;
using Galvao.Application.Common.Models;

namespace Galvao.Infrastructure.Persistence.Helpers;

public static class ExpressionBuilder
{
    public static Expression<Func<T, bool>>? BuildPredicate<T>(List<FilterItem> filters)
    {
        if (filters == null || filters.Count == 0) return null;

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combined = null;

        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.PropertyName)) continue;

            var prop = typeof(T).GetProperty(filter.PropertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) continue;

            var propAccess = Expression.MakeMemberAccess(parameter, prop);
            Expression? filterExpr = null;

            static object? ConvertValue(object? val, Type type)
            {
                if (val == null) return null;
                var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
                
                if (underlyingType == typeof(Guid))
                {
                    if (val is string s) return Guid.Parse(s);
                    if (val is Guid g) return g;
                }
                
                if (underlyingType.IsEnum)
                {
                    if (val is string e) return Enum.Parse(underlyingType, e, true);
                    return Enum.ToObject(underlyingType, Convert.ToInt64(val));
                }

                if (underlyingType == typeof(DateTime) && val is string dateStr)
                {
                    return DateTime.Parse(dateStr);
                }

                return Convert.ChangeType(val, underlyingType);
            }

            try
            {
                switch (filter.Operation)
                {
                    case FilterOption.Equal:
                        var eqVal = ConvertValue(filter.Value, prop.PropertyType);
                        if (eqVal == null)
                        {
                            // Property == null
                            filterExpr = Expression.Equal(propAccess, Expression.Constant(null, prop.PropertyType));
                        }
                        else
                        {
                            filterExpr = Expression.Equal(propAccess, Expression.Constant(eqVal, prop.PropertyType));
                        }
                        break;

                    case FilterOption.Contains:
                        if (prop.PropertyType == typeof(string) && filter.Value is string sVal)
                        {
                            var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
                            if (containsMethod != null)
                            {
                                filterExpr = Expression.Call(propAccess, containsMethod, Expression.Constant(sVal));
                            }
                        }
                        break;

                    case FilterOption.Range:
                        Expression? startExpr = null;
                        Expression? endExpr = null;

                        if (filter.Start != null)
                        {
                            var startVal = ConvertValue(filter.Start, prop.PropertyType);
                            startExpr = Expression.GreaterThanOrEqual(propAccess, Expression.Constant(startVal, prop.PropertyType));
                        }

                        if (filter.End != null)
                        {
                            var endVal = ConvertValue(filter.End, prop.PropertyType);
                            endExpr = Expression.LessThanOrEqual(propAccess, Expression.Constant(endVal, prop.PropertyType));
                        }

                        filterExpr = (startExpr != null && endExpr != null)
                            ? Expression.AndAlso(startExpr, endExpr)
                            : (startExpr ?? endExpr);
                        break;
                }
            }
            catch
            {
                // Skip invalid/unconvertible filter criteria to prevent crashes
                continue;
            }

            if (filterExpr != null)
            {
                combined = combined == null ? filterExpr : Expression.AndAlso(combined, filterExpr);
            }
        }

        return combined == null ? null : Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    public static IOrderedQueryable<T> ApplyOrdering<T>(IQueryable<T> query, List<OrderingItem> ordering)
    {
        if (ordering == null || ordering.Count == 0)
        {
            var idProp = typeof(T).GetProperty("Id") ?? typeof(T).GetProperties().FirstOrDefault();
            if (idProp == null) return (IOrderedQueryable<T>)query;
            return ApplyOrderingSingle(query, idProp.Name, SortingDirection.Ascending, true);
        }

        IOrderedQueryable<T>? ordered = null;
        for (int i = 0; i < ordering.Count; i++)
        {
            var order = ordering[i];
            if (string.IsNullOrWhiteSpace(order.Field)) continue;

            var prop = typeof(T).GetProperty(order.Field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) continue;

            ordered = ApplyOrderingSingle(query, prop.Name, order.Direction, i == 0, ordered);
        }

        return ordered ?? (IOrderedQueryable<T>)query;
    }

    private static IOrderedQueryable<T> ApplyOrderingSingle<T>(
        IQueryable<T> query,
        string propName,
        SortingDirection dir,
        bool isFirst,
        IOrderedQueryable<T>? existing = null)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var prop = typeof(T).GetProperty(propName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)!;
        var propAccess = Expression.MakeMemberAccess(parameter, prop);
        var orderLambda = Expression.Lambda(propAccess, parameter);

        string method = isFirst
            ? (dir == SortingDirection.Descending ? "OrderByDescending" : "OrderBy")
            : (dir == SortingDirection.Descending ? "ThenByDescending" : "ThenBy");

        var expr = Expression.Call(
            typeof(Queryable),
            method,
            [typeof(T), prop.PropertyType],
            isFirst ? query.Expression : existing!.Expression,
            Expression.Quote(orderLambda));

        return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(expr);
    }
}
