using System.Collections;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Store;
using Microsoft.Kiota.Serialization.Json;

namespace Repull.SDK;

/// <summary>
/// JSON reader for this SDK: Kiota's <see cref="JsonParseNodeFactory"/>, plus it keeps
/// an explicit <c>null</c>. Kiota skips a property whose JSON value is null, so reading
/// <c>{"pets": null}</c> (e.g. with <c>KiotaJsonSerializer.DeserializeAsync</c>) produced
/// a model that sent <c>{}</c>. After Kiota reads a model, this records every
/// null-valued property in the model's <see cref="RepullBackingStore"/>, so the null is
/// sent back on a write. Models of other Kiota clients are left exactly as Kiota reads them.
/// </summary>
/// <remarks>The generated <see cref="RepullClient"/> registers it for <c>application/json</c> (see scripts/regen.sh).</remarks>
public sealed class RepullJsonParseNodeFactory : IAsyncParseNodeFactory
{
    private readonly JsonParseNodeFactory _inner = new();

    /// <summary>Registers this reader for <c>application/json</c>, replacing Kiota's default one.</summary>
    public static void Register()
    {
        var factory = new RepullJsonParseNodeFactory();
        ParseNodeFactoryRegistry.DefaultInstance.ContentTypeAssociatedFactories[factory.ValidContentType] = factory;
    }

    /// <inheritdoc />
    public string ValidContentType => _inner.ValidContentType;

    /// <inheritdoc />
    [Obsolete("Use GetRootParseNodeAsync instead")]
    public IParseNode GetRootParseNode(string contentType, Stream content)
    {
        Validate(contentType, content);
        using var document = JsonDocument.Parse(content);
        return Wrap(document.RootElement.Clone());
    }

    /// <inheritdoc />
    public async Task<IParseNode> GetRootParseNodeAsync(string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        Validate(contentType, content);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
        return Wrap(document.RootElement.Clone());
    }

    private void Validate(string contentType, Stream content)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            throw new ArgumentNullException(nameof(contentType));
        }

        if (!ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException($"expected a {ValidContentType} content type");
        }

        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }
    }

    private static IParseNode Wrap(JsonElement element) => new NullKeepingParseNode(new JsonParseNode(element), element);

    /// <summary>Delegates to Kiota's <see cref="JsonParseNode"/>, then restores JSON nulls on the models it read.</summary>
    private sealed class NullKeepingParseNode : IParseNode
    {
        private readonly IParseNode _inner;
        private readonly JsonElement _element;

        public NullKeepingParseNode(IParseNode inner, JsonElement element)
        {
            _inner = inner;
            _element = element;
        }

        public Action<IParsable>? OnBeforeAssignFieldValues
        {
            get => _inner.OnBeforeAssignFieldValues;
            set => _inner.OnBeforeAssignFieldValues = value;
        }

        public Action<IParsable>? OnAfterAssignFieldValues
        {
            get => _inner.OnAfterAssignFieldValues;
            set => _inner.OnAfterAssignFieldValues = value;
        }

        public T GetObjectValue<T>(ParsableFactory<T> factory)
            where T : IParsable
        {
            var result = _inner.GetObjectValue(factory);
            RestoreNulls(_element, result);
            return result;
        }

        public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory)
            where T : IParsable
        {
            var result = _inner.GetCollectionOfObjectValues(factory).ToList();
            RestoreNulls(_element, result);
            return result;
        }

        public IParseNode? GetChildNode(string identifier)
        {
            if (_element.ValueKind != JsonValueKind.Object || !_element.TryGetProperty(identifier, out var child))
            {
                return _inner.GetChildNode(identifier);
            }

            var inner = _inner.GetChildNode(identifier);
            return inner == null ? null : new NullKeepingParseNode(inner, child);
        }

        public string? GetStringValue() => _inner.GetStringValue();

        public bool? GetBoolValue() => _inner.GetBoolValue();

        public byte? GetByteValue() => _inner.GetByteValue();

        public sbyte? GetSbyteValue() => _inner.GetSbyteValue();

        public int? GetIntValue() => _inner.GetIntValue();

        public float? GetFloatValue() => _inner.GetFloatValue();

        public long? GetLongValue() => _inner.GetLongValue();

        public double? GetDoubleValue() => _inner.GetDoubleValue();

        public decimal? GetDecimalValue() => _inner.GetDecimalValue();

        public Guid? GetGuidValue() => _inner.GetGuidValue();

        public DateTimeOffset? GetDateTimeOffsetValue() => _inner.GetDateTimeOffsetValue();

        public TimeSpan? GetTimeSpanValue() => _inner.GetTimeSpanValue();

        public Date? GetDateValue() => _inner.GetDateValue();

        public Time? GetTimeValue() => _inner.GetTimeValue();

        public IEnumerable<T> GetCollectionOfPrimitiveValues<T>() => _inner.GetCollectionOfPrimitiveValues<T>();

        public IEnumerable<T?> GetCollectionOfEnumValues<T>()
            where T : struct, Enum => _inner.GetCollectionOfEnumValues<T>();

        public T? GetEnumValue<T>()
            where T : struct, Enum => _inner.GetEnumValue<T>();

        public byte[]? GetByteArrayValue() => _inner.GetByteArrayValue();
    }

    /// <summary>
    /// Walks the JSON next to the model Kiota built from it. For each property Kiota
    /// knows that is JSON null, sets it to null in the model's RepullBackingStore (the
    /// store keys are the JSON names). Recurses into nested models, lists of models and
    /// oneOf/anyOf wrappers.
    /// </summary>
    internal static void RestoreNulls(JsonElement element, object? model)
    {
        switch (model)
        {
            case null:
                return;
            case IComposedTypeWrapper and IBackedModel wrapper:
                // A oneOf/anyOf wrapper: the JSON belongs to whichever member it holds.
                foreach (var member in wrapper.BackingStore.Enumerate())
                {
                    if (member.Value is IParsable)
                    {
                        RestoreNulls(element, member.Value);
                    }
                }

                return;
            case IList list when element.ValueKind == JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (index >= list.Count)
                    {
                        break;
                    }

                    RestoreNulls(item, list[index++]);
                }

                return;
            case IParsable parsable and IBackedModel { BackingStore: RepullBackingStore store } when element.ValueKind == JsonValueKind.Object:
                var known = parsable.GetFieldDeserializers();
                foreach (var property in element.EnumerateObject())
                {
                    if (!known.ContainsKey(property.Name))
                    {
                        continue;
                    }

                    if (property.Value.ValueKind == JsonValueKind.Null)
                    {
                        store.Set<object?>(property.Name, null);
                    }
                    else if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        RestoreNulls(property.Value, store.Get<object>(property.Name));
                    }
                }

                return;
        }
    }
}
