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
            ModifyIntArray(arrayProperty, currentJson, index, (int)(object)value!);
        }
        else if (elementType == typeof(float))  
        {
            ModifyFloatArray(arrayProperty, currentJson, index, (float)(object)value!);
        }
        else if (elementType == typeof(string))
        {
            ModifyStringArray(arrayProperty, currentJson, index, (string)(object)value!);
        }
        else
        {
            // Default to double for mixed or unknown types
            ModifyDoubleArray(arrayProperty, currentJson, index, (double)(object)value!);
        }
    }
    
    private void ModifyIntArray(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, int newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<int>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.Number ? reader.GetInt32() : 0);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteNumberValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(arrayProperty, arrayJson);
    }
    
    private void ModifyFloatArray(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, float newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<float>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.Number ? reader.GetSingle() : 0f);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteNumberValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(arrayProperty, arrayJson);
    }
    
    private void ModifyStringArray(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, string newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.String ? reader.GetString()! : string.Empty);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteStringValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(arrayProperty, arrayJson);
    }
    
    private void ModifyDoubleArray(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, double newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<double>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.Number ? reader.GetDouble() : 0.0);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteNumberValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(arrayProperty, arrayJson);
    }

    public JsonView this[string name]
    {
        get => new JsonView(_model, Encoding.UTF8.GetBytes(name));
    }

    public JsonArrayElement this[ReadOnlySpan<byte> path]
    {
        get => new JsonArrayElement(_model, path.ToArray());
    }


    public string GetString(ReadOnlySpan<byte> name)
    {
        // Check if this is an array index operation (contains '/')
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            return GetArrayItem<string>(name.Slice(0, slashIndex), name.Slice(slashIndex + 1));
        }

        if (_model.TryGet(name, out ReadOnlySpan<byte> value) && value.Length > 0)
            return value.AsString();
        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found or has no value.");
    }

    public double GetDouble(ReadOnlySpan<byte> name)
    {
        // Check if this is an array index operation (contains '/')
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            return GetArrayItem<double>(name.Slice(0, slashIndex), name.Slice(slashIndex + 1));
        }

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

    private T GetArrayItem<T>(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> indexSpan)
    {
        if (!Utf8Parser.TryParse(indexSpan, out int index, out _))
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

public ref struct JsonArrayElement
{
    private readonly IJsonModel _model;
    private readonly byte[] _path;

    internal JsonArrayElement(IJsonModel model, byte[] path)
    {
        _model = model;
        _path = path;
    }

    public void Set<T>(T value)
    {
        SetValue(value);
    }

    private void SetValue<T>(T value)
    {
        ReadOnlySpan<byte> pathSpan = _path.AsSpan();
        
        // Check if this is an array index operation (contains '/')
        int slashIndex = pathSpan.IndexOf((byte)'/');
        if (slashIndex > 0)
        {
            // Parse array property name and index
            ReadOnlySpan<byte> propertyName = pathSpan.Slice(0, slashIndex);
            ReadOnlySpan<byte> indexSpan = pathSpan.Slice(slashIndex + 1);
            
            if (Utf8Parser.TryParse(indexSpan, out int arrayIndex, out _))
            {
                SetArrayItem(propertyName, arrayIndex, value);
                return;
            }
        }
        
        // Regular property set
        SetProperty(pathSpan, value);
    }



    private void SetProperty<T>(ReadOnlySpan<byte> name, T value)
    {
        // Serialize the value to JSON
        MemoryStream stream = new(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        
        if (value is string stringValue)
        {
            writer.WriteStringValue(stringValue);
        }
        else if (value is double doubleValue)
        {
            writer.WriteNumberValue(doubleValue);
        }
        else if (value is int intValue)
        {
            writer.WriteNumberValue(intValue);
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(T)} is not supported");
        }
        
        writer.Flush();
        ReadOnlySpan<byte> json = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(name, json);
    }

    private void SetArrayItem<T>(ReadOnlySpan<byte> arrayProperty, int index, T value)
    {
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
            ModifyIntArrayElement(arrayProperty, currentJson, index, (int)(object)value!);
        }
        else if (elementType == typeof(float))  
        {
            ModifyFloatArrayElement(arrayProperty, currentJson, index, (float)(object)value!);
        }
        else if (elementType == typeof(string))
        {
            ModifyStringArrayElement(arrayProperty, currentJson, index, (string)(object)value!);
        }
        else
        {
            // Default to double for mixed or unknown types
            ModifyDoubleArrayElement(arrayProperty, currentJson, index, (double)(object)value!);
        }
    }
    
    private void ModifyIntArrayElement(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, int newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<int>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.Number ? reader.GetInt32() : 0);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteNumberValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(arrayProperty, arrayJson);
    }
    
    private void ModifyFloatArrayElement(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, float newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<float>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.Number ? reader.GetSingle() : 0f);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteNumberValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(arrayProperty, arrayJson);
    }
    
    private void ModifyStringArrayElement(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, string newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.String ? reader.GetString()! : string.Empty);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteStringValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(arrayProperty, arrayJson);
    }
    
    private void ModifyDoubleArrayElement(ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, double newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<double>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.Number ? reader.GetDouble() : 0.0);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteNumberValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(arrayProperty, arrayJson);
    }
}




