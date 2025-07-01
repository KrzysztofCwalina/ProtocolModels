using System.ClientModel.Primitives;
using System.Reflection;
using System.Text;
using System.Text.Json;

public abstract class ExtensibleModel<T> : JsonModel<T>, IExtensibleModel
{
    private JsonProperties additionalProperties = new();

    // method to access and manipulate model properties (whether CLR or JSON)

    // this overload (ROS) is more efficient for JSON properties than CLR properties
    protected bool TryGetClrOrJsonProperty(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> json)
    {
        // Try to get CLR property first
        if (TryGetClrProperty(name, out object? clrValue))
        {
            // this allocates
            json = clrValue.ToJson().Span;
            return true;
        }
        // If CLR property doesn't exist, try additional properties
        if (additionalProperties.TryGet(name, out json))
        {
            return true;
        }
        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found.");
    }

    protected void SetClrOrJsonProperty(ReadOnlySpan<byte> name, object value)
    {
        if (TrySetClrProperty(name, value))
        {
            return;
        }
        
        // No CLR property exists, store in additionalProperties
        additionalProperties.Set(name, value);
        return;
    }

    protected void SetClrOrJsonProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        // Check if a CLR property with this name exists
        if (!TryGetClrPropertyType(name, out Type? ptype))
        {
            // No CLR property exists, store in additionalProperties
            additionalProperties.Set(name, value);
            return;
        }

        // A CLR property exists
        object clrValue = SerializationHelpers.JsonToClrValue(value, ptype);
        if (!TrySetClrProperty(name, clrValue))
        {
            throw new NotImplementedException();
        }
    }

    #region IExtensibleModel implementations
    bool IExtensibleModel.TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
        => TryGetClrOrJsonProperty(name, out value);
    void IExtensibleModel.Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        => SetClrOrJsonProperty(name, value);
    bool IExtensibleModel.TryGetPropertyType(ReadOnlySpan<byte> name, out Type? value)
        => TryGetClrPropertyType(name, out value);
    #endregion
    #region IJsonModel<T> helpers
    // the following two methods are used by IJsonModel<T> implemetnations to write and read additional properties
    protected void WriteAdditionalProperties(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    => additionalProperties.Write(writer, options);
    protected static void ReadAdditionalProperty(JsonView model, JsonProperty property)
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes(property.Name);
        byte[] valueBytes = Encoding.UTF8.GetBytes(property.Value.GetRawText());
        model.Set(nameBytes, (ReadOnlySpan<byte>)valueBytes);
    }
    #endregion
    #region PSEUDOREFLECTION
    // this can be overriden to avoid reflection or when properties are renamed
    protected virtual bool TryGetClrPropertyType(ReadOnlySpan<byte> name, out Type? type)
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
    protected virtual bool TryGetClrProperty(ReadOnlySpan<byte> name, out object? value)
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
    protected virtual bool TrySetClrProperty(ReadOnlySpan<byte> name, object value)
    {
        Type modelType = GetType();
        string clrProperty = name.ToClrPropertyName();
        PropertyInfo propertyInfo = modelType.GetProperty(clrProperty);
        if (propertyInfo != null)
        {
            Type propertyType = propertyInfo?.PropertyType!;
            Type valueType = value.GetType();
            if (propertyType.IsAssignableFrom(valueType))
            {
                try
                {
                    propertyInfo.SetValue(this, value);
                    return true;
                }
                catch
                {
                    // set additional property (below)
                }
            }
        }
        throw new ArgumentException($"Property '{Encoding.UTF8.GetString(name)}' does not exist or is not assignable.", nameof(name));
    }
    #endregion
    #region CONVENIENCE
    public JsonView Json => new JsonView((IExtensibleModel)this);
    // cute helper APIs
    public ReadOnlySpan<byte> this[ReadOnlySpan<byte> name]
    {
        set => Json.Set(name, value);
    }
    public object this[string name]
    {
        set => SetClrOrJsonProperty(Encoding.UTF8.GetBytes(name), value);
    }
    #endregion
}

internal static class SerializationHelpers
{
    public static ReadOnlyMemory<byte> ToJson(this object? objValue)
    {
        if (objValue == null) return "null"u8.ToArray();

        var stream = new MemoryStream(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);

        Type type = objValue.GetType();
        if (type == typeof(double))
        {
            writer.WriteNumberValue((double)objValue);
        }
        else if (type == typeof(string))
        {
            writer.WriteStringValue((string)objValue);
        }
        else if (type == typeof(float))
        {
            writer.WriteNumberValue((Single)objValue);
        }
        if (type == typeof(double[]))
        {
            writer.WriteStartArray();
            foreach (var d in (double[])objValue)
                writer.WriteNumberValue(d);
            writer.WriteEndArray();
        }
        if (type == typeof(string[]))
        {
            writer.WriteStartArray();
            foreach (var s in (string[])objValue)
                writer.WriteStringValue(s);
            writer.WriteEndArray();
        }
        else
        {
            throw new NotImplementedException($"Unsupported property type: {type}");
        }
        writer.Flush();
        ReadOnlyMemory<byte> memory = stream.GetBuffer().AsMemory(0, (int)stream.Position);
        return memory;
    }

    public static object JsonToClrValue(ReadOnlySpan<byte> value, Type? ptype)
    {
        Utf8JsonReader reader = new Utf8JsonReader(value);
        // Try to convert JSON to the CLR property type
        object convertedValue;
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                if (ptype == typeof(string))
                {
                    return reader.GetString();
                }
                break;
            case JsonTokenType.Number:
                if (ptype == typeof(int))
                {
                    return reader.GetInt32();
                }
                else if (ptype == typeof(double))
                {
                    return reader.GetDouble();
                }
                break;
            case JsonTokenType.True:
            case JsonTokenType.False:
                if (ptype == typeof(bool))
                {
                    return reader.GetBoolean();
                }
                break;
            case JsonTokenType.StartArray:
                if (ptype.IsArray)
                {
                    try
                    {
                        return JsonToClrArray(value, ptype.GetElementType()!);
                    }
                    catch { }
                }
                break;
        }

        return true;
    }

    private static object JsonToClrArray(ReadOnlySpan<byte> json, Type itemType)
    {
        JsonDocument jsonDocument = JsonDocument.Parse(json.ToArray());
        JsonElement root = jsonDocument.RootElement;

        if (itemType == typeof(double[]))
        {
            var list = new List<double>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetDouble());
            double[] array = list.ToArray();
            return array;
        }
        else if (itemType == typeof(string[]))
        {
            var list = new List<string>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetString() ?? string.Empty);
            string[] array = list.ToArray();
            return array;
        }
        else if (itemType == typeof(int[]))
        {
            var list = new List<int>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetInt32());
            int[] array = list.ToArray();
            return array;
        }
        else if (itemType == typeof(float[]))
        {
            var list = new List<float>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetSingle());
            float[] array = list.ToArray();
            return array;
        }
        throw new NotSupportedException($"Unsupported array type: {itemType}");
    }
}

