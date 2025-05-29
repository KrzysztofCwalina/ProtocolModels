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

    public JsonView this[string name]
    {
        get => new JsonView(_model, Encoding.UTF8.GetBytes(name));
    }
    
    // Special Set method that handles array indexing syntax
    public void SetArrayItem(ReadOnlySpan<byte> nameWithIndex, object value)
    {
        // Parse the name to check if it contains array indexing syntax
        if (ParseArrayIndexFromName(nameWithIndex, out ReadOnlySpan<byte> propertyName, out int arrayIndex))
        {
            // Handle array item setting
            SetArrayItemInternal(propertyName, arrayIndex, value);
        }
        else
        {
            // Handle regular property setting
            if (value is double d)
                Set(nameWithIndex, d);
            else if (value is string s)
                Set(nameWithIndex, s);
            else if (value is int i)
                Set(nameWithIndex, (double)i); // Convert int to double
            else
                throw new NotSupportedException($"Unsupported value type: {value?.GetType()}");
        }
    }
    
    private bool ParseArrayIndexFromName(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> propertyName, out int arrayIndex)
    {
        // Look for the array index separator (backslash in this case)
        int separatorIndex = -1;
        for (int i = 0; i < name.Length; i++)
        {
            if (name[i] == (byte)'\\')
            {
                separatorIndex = i;
                break;
            }
        }
        
        if (separatorIndex >= 0 && separatorIndex < name.Length - 1)
        {
            propertyName = name.Slice(0, separatorIndex);
            ReadOnlySpan<byte> indexSpan = name.Slice(separatorIndex + 1);
            
            // Try to parse the index
            string indexString = Encoding.UTF8.GetString(indexSpan);
            if (int.TryParse(indexString, out arrayIndex))
            {
                return true;
            }
        }
        
        propertyName = default;
        arrayIndex = -1;
        return false;
    }
    
    private void SetArrayItemInternal(ReadOnlySpan<byte> propertyName, int arrayIndex, object value)
    {
        // Get the current array from the model
        if (!_model.TryGet(propertyName, out ReadOnlySpan<byte> currentValue) || currentValue.Length == 0)
        {
            throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(propertyName)}' not found or has no value.");
        }
        
        // Parse the current array
        var reader = new Utf8JsonReader(currentValue);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(propertyName)}' is not a JSON array.");
        }
        
        // Read the array into a list for modification
        var list = new List<object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
                
            // Read each element as an object
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    list.Add(reader.GetDouble());
                    break;
                case JsonTokenType.String:
                    list.Add(reader.GetString()!);
                    break;
                case JsonTokenType.True:
                case JsonTokenType.False:
                    list.Add(reader.GetBoolean());
                    break;
                default:
                    throw new NotSupportedException($"Unsupported JSON token type in array: {reader.TokenType}");
            }
        }
        
        // Ensure the array is large enough
        while (list.Count <= arrayIndex)
        {
            list.Add(0.0); // Default value for expansion
        }
        
        // Set the new value
        if (value is int i)
            list[arrayIndex] = (double)i; // Convert int to double for consistency
        else
            list[arrayIndex] = value;
        
        // Serialize the modified array back to JSON
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartArray();
        foreach (var item in list)
        {
            if (item is double d)
                writer.WriteNumberValue(d);
            else if (item is string s)
                writer.WriteStringValue(s);
            else if (item is bool b)
                writer.WriteBooleanValue(b);
            else
                throw new NotSupportedException($"Unsupported array item type: {item?.GetType()}");
        }
        writer.WriteEndArray();
        writer.Flush();
        
        // Set the updated array back to the model
        ReadOnlySpan<byte> newJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(propertyName, newJson);
    }
    public string GetString(ReadOnlySpan<byte> name)
    {
        if (_model.TryGet(name, out ReadOnlySpan<byte> value) && value.Length > 0)
            return value.AsString();
        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found or has no value.");
    }
    public double GetDouble(ReadOnlySpan<byte> name)
    {
        //Span<byte> fullPath = stackalloc byte[_path.Length + name.Length + 1];
        //_path.AsSpan().CopyTo(fullPath);
        //fullPath[_path.Length] = (byte)'/';
        //name.CopyTo(fullPath.Slice(_path.Length + 1));
        ReadOnlySpan<byte> value;
        if (_path.Length > 0)
        {
            if (!_model.TryGet(_path, out value)) throw new KeyNotFoundException();
            
            Span<byte> path = stackalloc byte[name.Length+ 1];
            path[0] = (byte)'/';
            name.CopyTo(path.Slice(1));
            return value.GetDouble(path);
        }
        else
        {
            if (!_model.TryGet(name, out value)) throw new KeyNotFoundException();
            return value.AsDouble();
        }
    }
    // get spillover (or real?) property or array value
    public bool TryGet(string name, out ReadOnlySpan<byte> value)
        => TryGet(Encoding.UTF8.GetBytes(name), out value);
    public bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value) 
        => _model.TryGet(name, out value);

    public T[] GetArray<T>(ReadOnlySpan<byte> name)
    {
        if (!_model.TryGet(name, out ReadOnlySpan<byte> value) || value.Length == 0)
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


