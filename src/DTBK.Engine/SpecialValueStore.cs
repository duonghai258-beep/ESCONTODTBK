namespace DTBK.Engine;

/// <summary>
/// Instance-owned SpecialValue storage matching the forensic boundary:
/// dictionary-backed object values with explicit Contains/get/set/add/remove operations.
/// No provider or database fallback is performed here.
/// </summary>
public sealed class SpecialValueStore
{
    private readonly Dictionary<string, object> _values = new();

    public bool Contains(string key) => _values.ContainsKey(key);

    public object this[string key]
    {
        get => _values[key];
        set => _values[key] = value;
    }

    public void Add(string key, object value) => _values.Add(key, value);

    public bool Remove(string key) => _values.Remove(key);

    public int Count => _values.Count;

    public IReadOnlyDictionary<string, object> Snapshot() => new Dictionary<string, object>(_values);
}
