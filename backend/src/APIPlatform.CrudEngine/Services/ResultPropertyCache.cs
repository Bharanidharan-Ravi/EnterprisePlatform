using System.Linq.Expressions;

namespace APIPlatform.CrudEngine.Services;


/// <summary>Caches Task.Result property getters (per concrete Task&lt;T&gt; type) and generic
/// property getters used to read OperationResult/Result&lt;T&gt;/ErrorInfo without repeated
/// GetProperty/GetValue reflection (Req 13).</summary>
internal static class ResultPropertyCache
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Func<object, object?>> Cache = new();

    public static object? GetResult(Task task)
    {
        var getter = Cache.GetOrAdd(task.GetType(), t =>
        {
            var prop = t.GetProperty("Result") ?? throw new MissingMemberException(t.Name, "Result");
            var param = Expression.Parameter(typeof(object), "t");
            var body = Expression.Convert(Expression.Property(Expression.Convert(param, t), prop), typeof(object));
            return Expression.Lambda<Func<object, object?>>(body, param).Compile();
        });
        return getter(task);
    }
}

internal static class PropertyAccessorCache
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, string), Func<object, object?>?> Cache = new();

    public static Func<object, object?>? Get(Type type, string propertyName) =>
        Cache.GetOrAdd((type, propertyName), key =>
        {
            var (t, name) = key;
            var prop = t.GetProperty(name);
            if (prop is null) return null;

            var param = Expression.Parameter(typeof(object), "o");
            var body = Expression.Convert(Expression.Property(Expression.Convert(param, t), prop), typeof(object));
            return Expression.Lambda<Func<object, object?>>(body, param).Compile();
        });
}
