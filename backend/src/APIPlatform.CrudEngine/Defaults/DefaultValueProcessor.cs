using APIPlatform.CrudEngine.Models;
using APIPlatform.Foundation.Interfaces;

namespace APIPlatform.CrudEngine.Defaults;

/// <summary>Default IDefaultValueProcessor. No-ops for entities with no registered config
/// (IEntityDefaultValueProvider is optional — resolved as null-safe via TryGetConfig).</summary>
public sealed class DefaultValueProcessor : IDefaultValueProcessor
{
    private readonly IEntityDefaultValueProvider _provider;
    private readonly IClock _clock;

    public DefaultValueProcessor(IEntityDefaultValueProvider provider, IClock clock)
    {
        _provider = provider;
        _clock = clock;
    }

    public void Apply<TEntity>(CrudContext<TEntity> context) where TEntity : class
    {
        if (context.Entity is null) return;
        if (context.Operation is not (CrudOperationType.Create or CrudOperationType.Update)) return;

        var config = _provider.TryGetConfig(context.EntityName);
        if (config is null) return;

        var entityType = typeof(TEntity);

        foreach (var binding in config.Bindings)
        {
            var prop = entityType.GetProperty(binding.FieldName);
            if (prop is null || !prop.CanWrite) continue;

            object? newValue = binding.Kind switch
            {
                DefaultValueKind.UtcNowOnCreate when context.Operation == CrudOperationType.Create => _clock.UtcNow,
                DefaultValueKind.UtcNowOnUpdate when context.Operation == CrudOperationType.Update => _clock.UtcNow,
                DefaultValueKind.ConstantValue => binding.ConstantValue,
                DefaultValueKind.IncrementVersion => IncrementVersion(prop.GetValue(context.Entity)),
                _ => null
            };

            if (newValue is not null && prop.PropertyType.IsInstanceOfType(newValue))
                prop.SetValue(context.Entity, newValue);
        }
    }

    private static object? IncrementVersion(object? current) => current switch
    {
        int i => i + 1,
        long l => l + 1,
        _ => 1
    };
}
