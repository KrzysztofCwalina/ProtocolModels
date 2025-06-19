using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;

// TOOD: what do we do with struct models?
public abstract class JsonModel<T> : IJsonModel<T>, IJsonModel
{
    private JsonProperties additionalProperties = new();
    public JsonView Json => new JsonView(this);

    public ReadOnlySpan<byte> this[ReadOnlySpan<byte> name]
    {
        // TODO: implement adding natural conversions
        set
        {
            Json.Set(name, value);
        }
    }

    public object this[string name]
    {
        set
        {
            ReadOnlySpan<byte> nameBytes = Encoding.UTF8.GetBytes(name);
            if (!TrySetProperty(nameBytes, value))
                throw new ArgumentException($"Property '{name}' not found or cannot be set.");
        }
    }

    // this can be overriden to avoid reflection or when properties are renamed
    protected virtual bool TryGetPropertyType(ReadOnlySpan<byte> name, out Type? type)
    {
        Type modelType = GetType();
        string clrProperty = name.ToClrPropertyName();
        var propertyInfo = modelType.GetProperty(clrProperty);

        if (propertyInfo != null)
        {
            type = propertyInfo.PropertyType;
            return true;
        }
        type = null;
        return false;
    }
    protected virtual bool TryGetProperty(ReadOnlySpan<byte> name, out object? value)
    {
        Type modelType = GetType();
        string clrProperty = name.ToClrPropertyName();
        var propertyInfo = modelType.GetProperty(clrProperty);

        if (propertyInfo != null)
        {
            value = propertyInfo.GetValue(this, null);
            return true;
        }
        value = null;
        return false;
    }
    protected virtual bool TrySetProperty(ReadOnlySpan<byte> name, object value)
    {
        Type modelType = GetType();
        string clrProperty = name.ToClrPropertyName();
        var propertyInfo = modelType.GetProperty(clrProperty);

        if (propertyInfo != null)
        {
            propertyInfo.SetValue(this, value);
            return true;
        }
        Json.Set(name, value);
        return true;
    }

    #region IJsonModel implementations
    protected abstract void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options);
    protected abstract T CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options);

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
                writer.WriteStringValue((string)objValue);
                return SetValue(out value, stream, writer);
            }
            if(type == typeof(float))
            {
                writer.WriteNumberValue((Single)objValue);
                return SetValue(out value, stream, writer);
            }
            if(type == typeof(double[]))
            {
                writer.WriteStartArray();
                foreach (var d in (double[])objValue)
                    writer.WriteNumberValue(d);
                writer.WriteEndArray();
                return SetValue(out value, stream, writer);
            }
            if(type == typeof(string[]))
            {
                writer.WriteStartArray();
                foreach (var s in (string[])objValue)
                    writer.WriteStringValue(s);
                writer.WriteEndArray();
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
        if (!TryGetPropertyType(name, out Type? ptype))
            additionalProperties.Set(name, value);
        else
            SetRealProperty(name, value);
    }

    bool IJsonModel.TryGetPropertyType(ReadOnlySpan<byte> name, out Type value)
        => TryGetPropertyType(name, out value);

    protected void WriteAdditionalProperties(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        => additionalProperties.Write(writer, options);
    #endregion

    private void SetRealProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
    {
        if (!TryGetPropertyType(name, out Type? ptype)){
            throw new Exception("property not found");
        }
        if (ptype.IsArray)
        {
            SetArrayProperty(name, json, ptype);
            return;
        }

        Utf8JsonReader reader = new Utf8JsonReader(json);
        if(!reader.Read()) throw new ArgumentException(nameof(json));
        object value = null;
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                value = reader.GetString()!;
                break;
            case JsonTokenType.Number:
                if(ptype == typeof(float))
                    value = reader.GetSingle();
                else if (ptype == typeof(double))
                    value = reader.GetDouble();
                else if (ptype == typeof(int))
                    value = reader.GetInt32();
                else if (ptype == typeof(long))
                    value = reader.GetInt64();
                else
                    throw new NotSupportedException($"Unsupported numeric type: {ptype}");
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

    private void SetArrayProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json, Type ptype)
    {
        JsonDocument jsonDocument = JsonDocument.Parse(json.ToArray());
        JsonElement root = jsonDocument.RootElement;;
        if (ptype == typeof(double[]))
        {
            var list = new List<double>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetDouble());
            double[] array = list.ToArray();
            if (TrySetProperty(name, array))
                return;
        }
        else if (ptype == typeof(string[]))
        {
            var list = new List<string>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetString() ?? string.Empty);
            string[] array = list.ToArray();
            if (TrySetProperty(name, array))
                return;
        }
        else if (ptype == typeof(int[]))
        {
            var list = new List<int>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetInt32());
            int[] array = list.ToArray();
            if (TrySetProperty(name, array))
                return;
        }
        else if (ptype == typeof(float[]))
        {
            var list = new List<float>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetSingle());
            float[] array = list.ToArray();
            if (TrySetProperty(name, array))
                return;
        }
        throw new NotSupportedException($"Unsupported array type: {ptype}");
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

