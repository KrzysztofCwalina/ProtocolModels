using System.Buffers.Text;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

public readonly struct JsonView
{
    private readonly IJsonModel _model;
    private readonly byte[] _path;

    internal JsonView(IJsonModel model) : this(model, Array.Empty<byte>())
    {}

    private JsonView(IJsonModel model, byte[] path)
    {
        _model = model;
        _path = path;
    }

    public void Set(ReadOnlySpan<byte> name, string value)
    {
        // Check if this is an array index operation (contains '/')
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            ArrayModifiers.SetArrayItem(_model, name.Slice(0, slashIndex), name.Slice(slashIndex + 1), value);
            return;
        }

        MemoryStream stream = new(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStringValue(value);
        writer.Flush();
        ReadOnlySpan<byte> json = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        Set(name, json);
    }

    public void Set(ReadOnlySpan<byte> name, double value)
    {
        // Check if this is an array index operation (contains '/')
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            ArrayModifiers.SetArrayItem(_model, name.Slice(0, slashIndex), name.Slice(slashIndex + 1), value);
            return;
        }

        MemoryStream stream = new(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteNumberValue(value);
        writer.Flush();
        ReadOnlySpan<byte> json = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        Set(name, json);
    }

    public void Set(ReadOnlySpan<byte> name, int value)
    {
        // Check if this is an array index operation (contains '/')
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            ArrayModifiers.SetArrayItem(_model, name.Slice(0, slashIndex), name.Slice(slashIndex + 1), value);
            return;
        }

        MemoryStream stream = new(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteNumberValue(value);
        writer.Flush();
        ReadOnlySpan<byte> json = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        Set(name, json);
    }

    public void Set(ReadOnlySpan<byte> name, object value)
    {
        if (value is string strValue)
        {
            Set(name, strValue);
            return;
        }

        if (value is double doubleValue)
        {
            Set(name, doubleValue);
            return;
        }

        if (value is int intValue)
        {
            Set(name, intValue);
            return;
        }

        throw new NotImplementedException();
    }
    // add or change value
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
        => _model.Set(name, json);
    public void Set(string name, ReadOnlySpan<byte> json)
        => Set(Encoding.UTF8.GetBytes(name), json);
    
    public JsonView this[string name]
    {
        get
        {
            // Create path by combining current path with new name
            byte[] namePath = Encoding.UTF8.GetBytes(name);
            byte[] fullPath;
            
            if (_path.Length > 0)
            {
                // We're already nested, append to the existing path
                fullPath = new byte[_path.Length + 1 + namePath.Length];
                _path.CopyTo(fullPath, 0);
                fullPath[_path.Length] = (byte)'/';
                namePath.CopyTo(fullPath, _path.Length + 1);
            }
            else
            {
                fullPath = namePath;
            }
            
            return new JsonView(_model, fullPath);
        }
    }

    public string GetString(ReadOnlySpan<byte> name)
    {
        // Handle nested paths using span operations
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            // Split into property name and sub-path
            ReadOnlySpan<byte> propertyName = name.Slice(0, slashIndex);
            ReadOnlySpan<byte> subPath = name.Slice(slashIndex); // Keep the leading slash for JsonPointer
            
            // Get the JSON for the property
            ReadOnlySpan<byte> propertyJson = GetPropertyValue(propertyName);
            
            // Use JsonPointer to navigate the remaining path
            return propertyJson.GetString(subPath);
        }

        // Regular property access
        if (_path.Length > 0)
        {
            // We're already in a nested object, get the JSON for that path
            ReadOnlySpan<byte> json = GetPropertyValue(_path);
            
            // Create JSON pointer format for the property name
            ReadOnlySpan<byte> jsonPointer = ConstructJsonPointer(name);
            
            // Use JsonPointer to find and extract the string value
            return json.GetString(jsonPointer);
        }
        else
        {
            // Direct property access on the model
            ReadOnlySpan<byte> valueData = GetPropertyValue(name);
            return valueData.GetString();
        }
    }

    public double GetDouble(ReadOnlySpan<byte> name)
    {
        // Handle nested paths using span operations
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            // Split into property name and sub-path
            ReadOnlySpan<byte> propertyName = name.Slice(0, slashIndex);
            ReadOnlySpan<byte> subPath = name.Slice(slashIndex); // Keep the leading slash for JsonPointer
            
            // Get the JSON for the property
            ReadOnlySpan<byte> propertyJson = GetPropertyValue(propertyName);
            
            // Use JsonPointer to navigate the remaining path
            return propertyJson.GetDouble(subPath);
        }

        // Regular property access
        if (_path.Length > 0)
        {
            // We're already in a nested object, get the JSON for that path
            ReadOnlySpan<byte> json = GetPropertyValue(_path);
            
            // Create JSON pointer format for the property name
            ReadOnlySpan<byte> jsonPointer = ConstructJsonPointer(name);
            
            // Use JsonPointer to find and extract the double value
            return json.GetDouble(jsonPointer);
        }
        else
        {
            // Direct property access on the model
            ReadOnlySpan<byte> valueData = GetPropertyValue(name);
            return valueData.GetDouble();
        }
    }
    
    /// <summary>
    /// Constructs a JSON pointer path from a property name, handling both simple names and nested paths
    /// </summary>
    private ReadOnlySpan<byte> ConstructJsonPointer(ReadOnlySpan<byte> name)
    {
        // If name doesn't start with '/', add it to make it a valid JSON pointer
        if (name.Length == 0 || name[0] != (byte)'/')
        {
            byte[] jsonPointer = new byte[name.Length + 1];
            jsonPointer[0] = (byte)'/';
            name.CopyTo(jsonPointer.AsSpan(1));
            return jsonPointer;
        }
        else
        {
            return name;
        }
    }

    // Internal Get methods that throw exceptions directly
    private ReadOnlySpan<byte> GetPropertyValue(ReadOnlySpan<byte> name)
    {
        return _model.Get(name);
    }
    
    // get spillover (or real?) property or array value
    public bool TryGet(string name, out ReadOnlySpan<byte> value)
        => TryGet(Encoding.UTF8.GetBytes(name), out value);
    public bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value) 
        => _model.TryGet(name, out value);

    public ReadOnlySpan<byte> Get(string name)
        => Get(Encoding.UTF8.GetBytes(name));
    public ReadOnlySpan<byte> Get(ReadOnlySpan<byte> name) 
        => _model.Get(name);

    public T[] GetArray<T>(ReadOnlySpan<byte> name)
    {
        ReadOnlySpan<byte> value = _model.Get(name);
        if (value.Length == 0)
            throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found or has no value.");
        
        // Use System.Text.Json to parse the array
        var reader = new Utf8JsonReader(value);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(name)}' is not a JSON array.");
        
        var list = new List<T>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            
            // Deserialize each element
            if (typeof(T) == typeof(string))
            {
                list.Add((T)(object)reader.GetString()!);
            }
            else if (typeof(T) == typeof(double))
            {
                list.Add((T)(object)reader.GetDouble());
            }
            else if (typeof(T) == typeof(int))
            {
                list.Add((T)(object)reader.GetInt32());
            }
            else if (typeof(T) == typeof(float))
            {
                list.Add((T)(object)reader.GetSingle());
            }
            else if (typeof(T) == typeof(bool))
            {
                list.Add((T)(object)reader.GetBoolean());
            }
            else
            {
                // For complex types, get the raw JSON and deserialize
                var element = JsonDocument.ParseValue(ref reader).RootElement;
                list.Add(element.Deserialize<T>()!);
            }
        }
        return list.ToArray();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        => _model.WriteAdditionalProperties(writer, options);
}




