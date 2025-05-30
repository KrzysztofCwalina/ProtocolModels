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
            SetArrayItem(name.Slice(0, slashIndex), name.Slice(slashIndex + 1), value);
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
            SetArrayItem(name.Slice(0, slashIndex), name.Slice(slashIndex + 1), value);
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
            SetArrayItem(name.Slice(0, slashIndex), name.Slice(slashIndex + 1), value);
            return;
        }

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

    private void SetArrayItem<T>(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> indexSpan, T value)
    {
        if (!Utf8Parser.TryParse(indexSpan, out int index, out _))
        {
            throw new ArgumentException($"Invalid array index: {Encoding.UTF8.GetString(indexSpan)}");
        }

        // Check if this is a real property with a specific type
        Type? propertyType = _model.GetPropertyType(arrayProperty);
        
        // Get current array - must exist
        if (!_model.TryGet(arrayProperty, out ReadOnlySpan<byte> currentJson))
        {
            throw new InvalidOperationException($"Array property '{Encoding.UTF8.GetString(arrayProperty)}' does not exist");
        }

        // Use typed array handling based on property type
        Type? elementType = propertyType?.IsArray == true ? propertyType.GetElementType() : null;
        
        if (elementType == typeof(int))
        {
            ArrayModifiers.ModifyIntArray(_model, arrayProperty, currentJson, index, (int)(object)value!);
        }
        else if (elementType == typeof(float))  
        {
            ArrayModifiers.ModifyFloatArray(_model, arrayProperty, currentJson, index, (float)(object)value!);
        }
        else if (elementType == typeof(string))
        {
            ArrayModifiers.ModifyStringArray(_model, arrayProperty, currentJson, index, (string)(object)value!);
        }
        else if (elementType == typeof(double))
        {
            ArrayModifiers.ModifyDoubleArray(_model, arrayProperty, currentJson, index, (double)(object)value!);
        }
        else if (elementType == null)
        {
            // For JSON-only arrays without explicit type, use the value type to determine the array type
            if (typeof(T) == typeof(int))
            {
                ArrayModifiers.ModifyIntArray(_model, arrayProperty, currentJson, index, (int)(object)value!);
            }
            else if (typeof(T) == typeof(float))  
            {
                ArrayModifiers.ModifyFloatArray(_model, arrayProperty, currentJson, index, (float)(object)value!);
            }
            else if (typeof(T) == typeof(string))
            {
                ArrayModifiers.ModifyStringArray(_model, arrayProperty, currentJson, index, (string)(object)value!);
            }
            else if (typeof(T) == typeof(double))
            {
                ArrayModifiers.ModifyDoubleArray(_model, arrayProperty, currentJson, index, (double)(object)value!);
            }
            else
            {
                throw new NotSupportedException($"Value type '{typeof(T).Name}' is not supported for array item modification");
            }
        }
        else
        {
            throw new NotSupportedException($"Array element type '{elementType.Name}' is not supported for array item modification");
        }
    }

    public JsonView this[string name]
    {
        get => new JsonView(_model, Encoding.UTF8.GetBytes(name));
    }

    public string GetString(ReadOnlySpan<byte> name)
    {
        // Use the same path resolution logic as GetDouble
        ReadOnlySpan<byte> json = GetValueForPath(name);
        return json.AsString();
    }

    public double GetDouble(ReadOnlySpan<byte> name)
    {
        // Use GetValueForPath to handle nested path resolution
        ReadOnlySpan<byte> json = GetValueForPath(name);
        return json.AsDouble();
    }
    
    // Helper method to handle nested path navigation
    private ReadOnlySpan<byte> GetValueForPath(ReadOnlySpan<byte> path)
    {
        // If this JsonView already has a path, we need to handle it
        if (_path.Length > 0)
        {
            if (!_model.TryGet(_path, out var nestedValue))
                throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(_path)}' not found");
            
            // Create a JSON pointer with the path
            var pathBytes = new byte[path.Length + 1];
            pathBytes[0] = (byte)'/';
            path.CopyTo(pathBytes.AsSpan(1));
            var jsonPointer = pathBytes;
            
            var reader = nestedValue.Find(jsonPointer);
            return reader.ValueSpan;
        }
        
        // If no slashes, it's a direct property access
        int slashIndex = path.IndexOf((byte)'/');
        if (slashIndex < 0)
        {
            if (!_model.TryGet(path, out var value))
                throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(path)}' not found");
            return value;
        }
        
        // We have a path with at least one slash
        var firstSegmentBytes = new byte[slashIndex];
        path.Slice(0, slashIndex).CopyTo(firstSegmentBytes);
        var firstSegment = firstSegmentBytes.AsSpan();
        
        var remainingPathBytes = new byte[path.Length - slashIndex - 1];
        path.Slice(slashIndex + 1).CopyTo(remainingPathBytes);
        var remainingPath = remainingPathBytes.AsSpan();
        
        // Get the value for the first segment
        if (_model.TryGet(firstSegment, out var segmentValue))
        {
            // Convert the remaining path to a JSON pointer (starts with /)
            var jsonPointerBytes = new byte[remainingPath.Length + 1];
            jsonPointerBytes[0] = (byte)'/';
            remainingPath.CopyTo(jsonPointerBytes.AsSpan(1));
            var jsonPointer = jsonPointerBytes;
            
            try
            {
                var reader = segmentValue.Find(jsonPointer);
                return reader.ValueSpan;
            }
            catch (Exception ex)
            {
                throw new KeyNotFoundException($"Error accessing path '{Encoding.UTF8.GetString(path)}': {ex.Message}", ex);
            }
        }
        
        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(firstSegment)}' not found");
    }

    private bool IsArrayIndex(ReadOnlySpan<byte> indexSpan, out int index)
    {
        return Utf8Parser.TryParse(indexSpan, out index, out _);
    }

    private T GetArrayItem<T>(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> indexSpan)
    {
        if (!IsArrayIndex(indexSpan, out int index))
        {
            throw new ArgumentException($"Invalid array index: {Encoding.UTF8.GetString(indexSpan)}");
        }

        // Get current array
        if (!_model.TryGet(arrayProperty, out ReadOnlySpan<byte> currentJson))
        {
            throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' not found");
        }

        // Parse existing array
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }

        int currentIndex = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                throw new IndexOutOfRangeException($"Array index {index} is out of range");
            }
            
            if (currentIndex == index)
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)reader.GetString()!;
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)reader.GetDouble();
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)reader.GetInt32();
                }
                else
                {
                    throw new NotSupportedException($"Type {typeof(T)} is not supported for array item access");
                }
            }
            currentIndex++;
        }
        
        throw new IndexOutOfRangeException($"Array index {index} is out of range");
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




