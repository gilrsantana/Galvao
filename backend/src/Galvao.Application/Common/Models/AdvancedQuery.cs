using System.Linq.Expressions;
using Galvao.Domain.Base;

namespace Galvao.Application.Common.Models;

public enum FilterOption
{
    Equal,
    Contains,
    Range
}

public class FilterItem
{
    public string PropertyName { get; set; } = string.Empty;
    public FilterOption Operation { get; set; }
    public object? Value { get; set; }
    public object? Start { get; set; }
    public object? End { get; set; }
}

public enum SortingDirection
{
    Ascending,
    Descending
}

public class OrderingItem
{
    public string Field { get; set; } = "Id";
    public SortingDirection Direction { get; set; } = SortingDirection.Ascending;
}

public class AdvancedQuery<T> where T : BaseEntity
{
    public List<FilterItem> Filters { get; set; } = [];
    public List<OrderingItem> Ordering { get; set; } = [];
    public bool NoTracking { get; set; } = true;
    public List<Expression<Func<T, object>>> Includes { get; set; } = [];

    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 25;
}
