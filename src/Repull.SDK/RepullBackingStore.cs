using Microsoft.Kiota.Abstractions.Store;

namespace Repull.SDK;

/// <summary>
/// The backing store every generated model uses (scripts/regen.sh points the
/// generated constructors here). It exists so an explicit <c>null</c> reaches the
/// API: Kiota leaves a null property out of the JSON body, which made
/// <c>"pets": null</c> (remove the pet fee) impossible to send. The store records
/// which properties were set — by your code or by deserialization — and Kiota's
/// serializer writes <c>"name": null</c> for each one set to null. A property you
/// never set is still left out.
/// </summary>
/// <remarks>
/// Unlike Kiota's <see cref="InMemoryBackingStore"/>, it does not do change
/// tracking: every set value is sent, whether it came from your code or from a
/// response or <c>KiotaJsonSerializer.Deserialize</c>. That keeps the request
/// identical to a client generated without a backing store, except for nulls.
/// </remarks>
public sealed class RepullBackingStore : IBackingStore
{
    private readonly Dictionary<string, object?> _store = new();
    private readonly Dictionary<string, Action<string, object?, object?>> _subscriptions = new();

    /// <summary>Creates the store for a new model instance.</summary>
    public static IBackingStore Create() => new RepullBackingStore();

    /// <inheritdoc />
    public bool InitializationCompleted { get; set; } = true;

    /// <summary>Ignored: every set value is always returned and serialized.</summary>
    public bool ReturnOnlyChangedValues { get; set; }

    /// <inheritdoc />
    public T? Get<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _store.TryGetValue(key, out var value) && value is T typed ? typed : default;
    }

    /// <inheritdoc />
    public void Set<T>(string key, T? value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        _store.TryGetValue(key, out var old);
        _store[key] = value;
        foreach (var callback in _subscriptions.Values.ToList())
        {
            callback(key, old, value);
        }
    }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, object?>> Enumerate() => _store.ToList();

    /// <summary>Every property explicitly set to null; Kiota's serializer writes each as <c>"key": null</c>.</summary>
    public IEnumerable<string> EnumerateKeysForValuesChangedToNull() =>
        _store.Where(kv => kv.Value == null).Select(kv => kv.Key).ToList();

    /// <inheritdoc />
    public string Subscribe(Action<string, object, object> callback)
    {
        var id = Guid.NewGuid().ToString();
        Subscribe(callback, id);
        return id;
    }

    /// <inheritdoc />
    public void Subscribe(Action<string, object, object> callback, string subscriptionId)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        _subscriptions[subscriptionId] = (k, o, n) => callback(k, o!, n!);
    }

    /// <inheritdoc />
    public void Unsubscribe(string subscriptionId) => _subscriptions.Remove(subscriptionId);

    /// <inheritdoc />
    public void Clear() => _store.Clear();
}
