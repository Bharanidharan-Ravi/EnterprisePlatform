using System.Collections;

namespace APIPlatform.Foundation.Interfaces;

/// <summary>
/// A future-proof primary key representation supporting single, natural, or composite keys
/// without forcing repository contracts to assume a single scalar id. Implements
/// <see cref="IReadOnlyDictionary{TKey,TValue}"/> directly (rather than wrapping one) so
/// callers get enumeration and LINQ support for free. Key names are matched
/// case-insensitively since field names may originate from SQL, SAP, or config sources
/// with inconsistent casing conventions.
/// </summary>
public sealed class EntityKey : IReadOnlyDictionary<string, object?>, IEquatable<EntityKey>
{
    private readonly IReadOnlyDictionary<string, object?> _values;

    public EntityKey(IReadOnlyDictionary<string, object?> values) =>
        _values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a single-value key. Caller must name the key field — Foundation never assumes "Id".</summary>
    public static EntityKey Single(string keyName, object? value) =>
        new(new Dictionary<string, object?> { [keyName] = value });

    /// <summary>Creates a composite/natural key from named parts.</summary>
    public static EntityKey Composite(IReadOnlyDictionary<string, object?> values) => new(values);

    public object? this[string key] => _values[key];
    public IEnumerable<string> Keys => _values.Keys;
    public IEnumerable<object?> Values => _values.Values;
    public int Count => _values.Count;
    public bool ContainsKey(string key) => _values.ContainsKey(key);
    public bool TryGetValue(string key, out object? value) => _values.TryGetValue(key, out value);
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(EntityKey? other)
    {
        if (other is null) return false;
        if (_values.Count != other._values.Count) return false;
        foreach (var (k, v) in _values)
        {
            if (!other._values.TryGetValue(k, out var ov) || !Equals(v, ov)) return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as EntityKey);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kv in _values.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(kv.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(kv.Value);
        }
        return hash.ToHashCode();
    }
}
