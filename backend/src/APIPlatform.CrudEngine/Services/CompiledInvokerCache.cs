using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace APIPlatform.CrudEngine.Services;

/// <summary>
/// Caches compiled invokers for the reflection-based repository calls BatchCrudExecutor makes
/// (Req 13). MakeGenericMethod/MethodInfo lookups happen once per (Type, methodName) instead of
/// once per batch unit; the compiled delegate is a plain Func&lt;object, object?[], object?&gt;
/// invoked directly thereafter — no further reflection cost per call.
/// </summary>
internal static class CompiledInvokerCache
{
    private static readonly ConcurrentDictionary<(Type, string), Func<object, object?[], object?>> Cache = new();

    public static Func<object, object?[], object?> GetInvoker(Type targetType, string methodName)
    {
        return Cache.GetOrAdd((targetType, methodName), key =>
        {
            var (type, name) = key;
            var method = type.GetMethod(name) ?? throw new MissingMethodException(type.Name, name);
            var parameters = method.GetParameters();

            var targetParam = Expression.Parameter(typeof(object), "target");
            var argsParam = Expression.Parameter(typeof(object?[]), "args");

            var castTarget = Expression.Convert(targetParam, type);
            var callArgs = parameters.Select((p, i) =>
                Expression.Convert(Expression.ArrayIndex(argsParam, Expression.Constant(i)), p.ParameterType));

            var call = Expression.Call(castTarget, method, callArgs);
            var body = Expression.Convert(call, typeof(object));

            return Expression.Lambda<Func<object, object?[], object?>>(body, targetParam, argsParam).Compile();
        });
    }
}
