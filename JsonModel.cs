using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

// scenarios:
// 1. Add a property to output (serialization only)
// 2. Ignore a property from input (serialization)
// 3. Change property type in output (serialization only)
// 4. Ignore a property from output (deserialization)
// 5. Read spillover property (deserialization)

public readonly struct JsonView
{
    private readonly IJsonModel _model;

    internal JsonView(IJsonModel model)
    {
        _model = model;
    }

    public void Set(ReadOnlySpan<byte> name, string value)
    {
        MemoryStream stream = new(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStringValue(value);
        writer.Flush();
        ReadOnlySpan<byte> json = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        Set(name, json);
    }

    public void Set(ReadOnlySpan<byte> name, double value)
    {
        MemoryStream stream = new(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteNumberValue(value);
        writer.Flush();
        ReadOnlySpan<byte> json = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        Set(name, json);
    }

    // add or change value
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
        => _model.Set(name, json);
    public void Set(string name, ReadOnlySpan<byte> json)
        => Set(Encoding.UTF8.GetBytes(name), json);

    // TODO: add Set overloads for other types (int, bool, etc.)

    public string GetString(ReadOnlySpan<byte> name)
    {
        if (_model.TryGet(name, out ReadOnlySpan<byte> value) && value.Length > 0)
            return value.AsString();
        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found or has no value.");
    }
    public double GetDouble(ReadOnlySpan<byte> name)
    {
        if (_model.TryGet(name, out ReadOnlySpan<byte> value) && value.Length > 0)
            return value.AsDouble();
        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found or has no value.");
    }
    // get spillover (or real?) property or array value
    public bool TryGet(string name, out ReadOnlySpan<byte> value)
        => TryGet(Encoding.UTF8.GetBytes(name), out value);
    public bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value) 
        => _model.TryGet(name, out value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        => _model.WriteAdditionalProperties(writer, options);
}

public struct JsonProperties
{
    private Dictionary<string, ReadOnlyMemory<byte>> _properties;
    public void Set(string name, ReadOnlySpan<byte> value)
    {
        if (_properties == null)
            _properties = new Dictionary<string, ReadOnlyMemory<byte>>();
        _properties[name] = value.ToArray();
    }
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        => Set(Encoding.UTF8.GetString(name), value);
    public bool TryGet(string name, out ReadOnlySpan<byte> value)
    {
        ReadOnlyMemory<byte> memory = default;
        if (_properties != null && _properties.TryGetValue(name, out memory))
        {
            value = memory.Span;
            return true;
        }
        value = default;
        return false;
    }
    public bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
    {
        string strName = Encoding.UTF8.GetString(name);
        return TryGet(strName, out value);
    }

    internal void Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        foreach (var kvp in _properties)
        {
            writer.WritePropertyName(kvp.Key);
            writer.WriteRawValue(kvp.Value.Span, true); // true to escape the value
        }
    }

}

// it's internal, so we can modify it later
internal interface IJsonModel
{
    bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value);
    void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value);

    void WriteAdditionalProperties(Utf8JsonWriter writer, ModelReaderWriterOptions options);
}

// TOOD: what do we do with struct models?
public abstract class JsonModel<T> : IJsonModel<T>, IJsonModel
{
    private JsonProperties additionalProperties = new();
    public JsonView Json => new JsonView(this);

    protected abstract bool TryGetProperty(ReadOnlySpan<byte> name, out object value);
    protected abstract bool HasProperty(ReadOnlySpan<byte> name);
    protected abstract bool TrySetProperty(ReadOnlySpan<byte> name, object value);
    protected abstract void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options);
    protected abstract T CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options);

    public object this[string name]
    {
        // TODO: implement adding natural conversions
        set => throw new NotImplementedException();
        get => throw new NotImplementedException();
    }
    bool IJsonModel.TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
    {
        if(additionalProperties.TryGet(name, out value))
            return true;

        if (TryGetProperty(name, out object objValue))
        {
            var stream = new MemoryStream(24);
            Utf8JsonWriter writer = new Utf8JsonWriter(stream);

            Type type = objValue.GetType();
            if(type == typeof(double))
            {
                writer.WriteNumberValue((double)objValue);
                return SetValue(out value, stream, writer);
            }
            if (type == typeof(string))
            {
                value = Encoding.UTF8.GetBytes((string)objValue);
                return SetValue(out value, stream, writer);
            }
            if(type == typeof(float))
            {
                writer.WriteNumberValue((Single)objValue);
                return SetValue(out value, stream, writer);
            }
            throw new NotImplementedException($"Unsupported property type: {type}");
        }

        value = default;
        return false;

        bool SetValue(out ReadOnlySpan<byte> value, MemoryStream stream, Utf8JsonWriter writer)
        {
            writer.Flush();
            value = stream.GetBuffer().AsSpan(0, (int)stream.Position);
            return true;
        }
    }

    void IJsonModel.Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        if (HasProperty(name))
        {
            SetRealProperty(name, value);
        }
        else
        {
            additionalProperties.Set(name, value);
        }
    }

    void IJsonModel.WriteAdditionalProperties(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        => additionalProperties.Write(writer, options);

    private void SetRealProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
    {
        Utf8JsonReader reader = new Utf8JsonReader(json);
        if(!reader.Read()) throw new ArgumentException(nameof(json));
        object value = null;
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                value = reader.GetString()!;
                break;
            case JsonTokenType.Number:
                value = reader.GetDouble();
                break;
            case JsonTokenType.True:
            case JsonTokenType.False:
                value = reader.GetBoolean();
                break;
            default:
                throw new NotSupportedException($"Unsupported JSON token type: {reader.TokenType}");
        }
        if (TrySetProperty(name, value))
            return;
    }

    #region MRW
    T IJsonModel<T>.Create(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
        => CreateCore(ref reader, options);

    T IPersistableModel<T>.Create(BinaryData data, ModelReaderWriterOptions options)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data.ToMemory().Span);
        return CreateCore(ref reader, options);
    }

    string IPersistableModel<T>.GetFormatFromOptions(ModelReaderWriterOptions options)  => "J";

    void IJsonModel<T>.Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        => WriteCore(writer, options);

    BinaryData IPersistableModel<T>.Write(ModelReaderWriterOptions options)
    {
        MemoryStream stream = new MemoryStream();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        WriteCore(writer, options);
        byte[] buffer = stream.GetBuffer();
        ReadOnlyMemory<byte> memory = buffer.AsMemory(0, (int)stream.Position);
        return new BinaryData(memory);
    }

    #endregion
}


