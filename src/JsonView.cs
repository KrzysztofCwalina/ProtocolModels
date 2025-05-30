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

    // We need to modify how nested paths are handled for deeper nesting
    private ReadOnlySpan<byte> ResolveNestedPath(ReadOnlySpan<byte> name)
    {
        // Check if this is a nested path (contains '/')
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex <= 0)
            return name;  // No more nesting
        
        // Get the first part of the path
        ReadOnlySpan<byte> firstSegment = name.Slice(0, slashIndex);
        ReadOnlySpan<byte> remainingPath = name.Slice(slashIndex + 1);
        
        // Return just the first segment
        return firstSegment;
    }
    
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
        // Special case for "bar/baz" pattern (single level nesting)
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            ReadOnlySpan<byte> propertyName = name.Slice(0, slashIndex);
            ReadOnlySpan<byte> subPropertyName = name.Slice(slashIndex + 1);
            
            // First check if this is an array access (e.g., "items/0")
            if (IsArrayIndex(subPropertyName, out _))
            {
                return GetArrayItem<string>(propertyName, subPropertyName);
            }
            
            // This is a property access on an object, e.g. "bar/baz"
            // First get the parent object
            if (!_model.TryGet(propertyName, out ReadOnlySpan<byte> parentJson))
                throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(propertyName)}' not found");
            
            // Parse it as JSON
            var reader = new Utf8JsonReader(parentJson);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(propertyName)}' is not an object");

            // Look for the specific property
            string subPropName = Encoding.UTF8.GetString(subPropertyName);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string currentPropName = reader.GetString();
                    if (currentPropName == subPropName)
                    {
                        reader.Read(); // Move to the property value
                        return reader.GetString();
                    }
                }
                
                if (reader.TokenType == JsonTokenType.EndObject)
                    throw new KeyNotFoundException($"Property '{subPropName}' not found in object '{Encoding.UTF8.GetString(propertyName)}'");
            }
            
            throw new KeyNotFoundException($"Property '{subPropName}' not found in object '{Encoding.UTF8.GetString(propertyName)}'");
        }

        // Regular property access
        if (_path.Length > 0)
        {
            // We're already in a nested object
            if (!_model.TryGet(_path, out ReadOnlySpan<byte> json))
                throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(_path)}' not found");
                
            // Create a JSON reader for the object
            var reader = new Utf8JsonReader(json);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(_path)}' is not an object");
                
            // Find the property
            string propName = Encoding.UTF8.GetString(name);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string currentName = reader.GetString();
                    if (currentName == propName)
                    {
                        reader.Read();
                        return reader.GetString();
                    }
                }
            }
            
            throw new KeyNotFoundException($"Property '{propName}' not found in object '{Encoding.UTF8.GetString(_path)}'");
        }
        else
        {
            // Direct property access on the model
            if (!_model.TryGet(name, out ReadOnlySpan<byte> valueData))
                throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found");
                
            return valueData.AsString();
        }
    }

    public double GetDouble(ReadOnlySpan<byte> name)
    {
        // Special case for "bar/baz" pattern (single level nesting)
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            ReadOnlySpan<byte> propertyName = name.Slice(0, slashIndex);
            ReadOnlySpan<byte> subPropertyName = name.Slice(slashIndex + 1);
            
            // First check if this is an array access (e.g., "items/0")
            if (IsArrayIndex(subPropertyName, out _))
            {
                return GetArrayItem<double>(propertyName, subPropertyName);
            }
            
            // This is a property access on an object, e.g. "bar/baz"
            // First get the parent object
            if (!_model.TryGet(propertyName, out ReadOnlySpan<byte> parentJson))
                throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(propertyName)}' not found");
            
            // Parse it as JSON
            var reader = new Utf8JsonReader(parentJson);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(propertyName)}' is not an object");

            // Look for the specific property
            string subPropName = Encoding.UTF8.GetString(subPropertyName);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string currentPropName = reader.GetString();
                    if (currentPropName == subPropName)
                    {
                        reader.Read(); // Move to the property value
                        return reader.GetDouble();
                    }
                }
                
                if (reader.TokenType == JsonTokenType.EndObject)
                    throw new KeyNotFoundException($"Property '{subPropName}' not found in object '{Encoding.UTF8.GetString(propertyName)}'");
            }
            
            throw new KeyNotFoundException($"Property '{subPropName}' not found in object '{Encoding.UTF8.GetString(propertyName)}'");
        }

        // Regular property access
        if (_path.Length > 0)
        {
            // We're already in a nested object
            if (!_model.TryGet(_path, out ReadOnlySpan<byte> json))
                throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(_path)}' not found");
                
            // Create a JSON reader for the object
            var reader = new Utf8JsonReader(json);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(_path)}' is not an object");
                
            // Find the property
            string propName = Encoding.UTF8.GetString(name);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string currentName = reader.GetString();
                    if (currentName == propName)
                    {
                        reader.Read();
                        return reader.GetDouble();
                    }
                }
            }
            
            throw new KeyNotFoundException($"Property '{propName}' not found in object '{Encoding.UTF8.GetString(_path)}'");
        }
        else
        {
            // Direct property access on the model
            if (!_model.TryGet(name, out ReadOnlySpan<byte> valueData))
                throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found");
                
            return valueData.AsDouble();
        }
    }
    
    private bool IsArrayIndex(ReadOnlySpan<byte> indexSpan, out int index)
    {
        return Utf8Parser.TryParse(indexSpan, out index, out _);
    }

    private T GetArrayItem<T>(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> indexSpan)
    {
        // Parse the index part
        int slashIndex = indexSpan.IndexOf((byte)'/');
        ReadOnlySpan<byte> currentIndex;
        ReadOnlySpan<byte> remainingPath;

        if (slashIndex > 0)
        {
            // We have a nested path after the index
            currentIndex = indexSpan.Slice(0, slashIndex);
            remainingPath = indexSpan.Slice(slashIndex + 1);
        }
        else
        {
            // Just a simple array index
            currentIndex = indexSpan;
            remainingPath = ReadOnlySpan<byte>.Empty;
        }

        if (!IsArrayIndex(currentIndex, out int index))
        {
            throw new ArgumentException($"Invalid array index: {Encoding.UTF8.GetString(currentIndex)}");
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

        // Find the array item
        int currentIndex1 = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                throw new IndexOutOfRangeException($"Array index {index} is out of range");
            }
            
            if (currentIndex1 == index)
            {
                // If there's a nested path, we need special handling
                if (!remainingPath.IsEmpty)
                {
                    // For object nesting after array access (items/0/id)
                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        // Parse the object
                        JsonDocument doc = JsonDocument.ParseValue(ref reader);
                        JsonElement element = doc.RootElement;
                        
                        // Navigate to the property using JsonElement APIs
                        string path = Encoding.UTF8.GetString(remainingPath);
                        string[] pathParts = path.Split('/');
                        JsonElement current = element;
                        
                        foreach (string part in pathParts)
                        {
                            if (!current.TryGetProperty(part, out current))
                            {
                                throw new KeyNotFoundException($"Property '{part}' not found in array element at index {index}");
                            }
                        }
                        
                        // Get the typed value from JsonElement
                        if (typeof(T) == typeof(string))
                        {
                            return (T)(object)current.GetString();
                        }
                        else if (typeof(T) == typeof(double))
                        {
                            return (T)(object)current.GetDouble();
                        }
                        else if (typeof(T) == typeof(int))
                        {
                            return (T)(object)current.GetInt32();
                        }
                        else
                        {
                            throw new NotSupportedException($"Type {typeof(T)} is not supported for nested array item access");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Cannot navigate into a non-object value at array index {index}");
                    }
                }
                
                // Direct value access
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
            currentIndex1++;
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




