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
        string nameString = Encoding.UTF8.GetString(name);
        int slashIndex = nameString.IndexOf('/');
        if (slashIndex > 0)
        {
            SetArrayItem(nameString, value);
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
        string nameString = Encoding.UTF8.GetString(name);
        int slashIndex = nameString.IndexOf('/');
        if (slashIndex > 0)
        {
            SetArrayItem(nameString, value);
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
        string nameString = Encoding.UTF8.GetString(name);
        int slashIndex = nameString.IndexOf('/');
        if (slashIndex > 0)
        {
            SetArrayItem(nameString, value);
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

    private void SetArrayItem<T>(string path, T value)
    {
        int slashIndex = path.IndexOf('/');
        string propertyName = path.Substring(0, slashIndex);
        string indexString = path.Substring(slashIndex + 1);
        
        if (!int.TryParse(indexString, out int arrayIndex))
        {
            throw new ArgumentException($"Invalid array index: {indexString}");
        }

        byte[] propertyNameBytes = Encoding.UTF8.GetBytes(propertyName);
        
        // Get current array
        if (!_model.TryGet(propertyNameBytes, out ReadOnlySpan<byte> currentJson))
        {
            // Property doesn't exist, create new array
            CreateArrayWithItem(propertyNameBytes, arrayIndex, value);
            return;
        }

        // Parse existing array
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{propertyName}' is not an array");
        }

        // Read all array elements
        var arrayElements = new List<object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
                
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    arrayElements.Add(reader.GetString()!);
                    break;
                case JsonTokenType.Number:
                    arrayElements.Add(reader.GetDouble());
                    break;
                default:
                    // For other types, store raw JSON
                    var doc = JsonDocument.ParseValue(ref reader);
                    arrayElements.Add(doc.RootElement.GetRawText());
                    break;
            }
        }

        // Extend array if necessary
        while (arrayElements.Count <= arrayIndex)
        {
            arrayElements.Add(GetDefaultValue(arrayElements));
        }

        // Set the value at the specified index
        arrayElements[arrayIndex] = value;

        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            if (element is string stringValue)
            {
                writer.WriteStringValue(stringValue);
            }
            else if (element is double doubleValue)
            {
                writer.WriteNumberValue(doubleValue);
            }
            else if (element is int intValue)
            {
                writer.WriteNumberValue(intValue);
            }
            else if (element is string rawJson)
            {
                writer.WriteRawValue(rawJson);
            }
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(propertyNameBytes, arrayJson);
    }

    private void CreateArrayWithItem<T>(byte[] propertyNameBytes, int index, T value)
    {
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        // Fill with default values up to the index
        for (int i = 0; i < index; i++)
        {
            if (value is string)
                writer.WriteStringValue("");
            else if (value is double || value is int)
                writer.WriteNumberValue(0);
        }
        
        // Write the actual value
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
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        _model.Set(propertyNameBytes, arrayJson);
    }

    private static object GetDefaultValue(List<object> existingElements)
    {
        if (existingElements.Count == 0)
            return 0.0; // Default to double
            
        var firstElement = existingElements[0];
        if (firstElement is string)
            return "";
        if (firstElement is double)
            return 0.0;
        if (firstElement is int)
            return 0;
            
        return 0.0; // Default fallback
    }

    public JsonView this[string name]
    {
        get => new JsonView(_model, Encoding.UTF8.GetBytes(name));
    }


    public string GetString(ReadOnlySpan<byte> name)
    {
        // Check if this is an array index operation (contains '/')
        string nameString = Encoding.UTF8.GetString(name);
        int slashIndex = nameString.IndexOf('/');
        if (slashIndex > 0)
        {
            return GetArrayItem<string>(nameString);
        }

        if (_model.TryGet(name, out ReadOnlySpan<byte> value) && value.Length > 0)
            return value.AsString();
        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found or has no value.");
    }

    public double GetDouble(ReadOnlySpan<byte> name)
    {
        // Check if this is an array index operation (contains '/')
        string nameString = Encoding.UTF8.GetString(name);
        int slashIndex = nameString.IndexOf('/');
        if (slashIndex > 0)
        {
            return GetArrayItem<double>(nameString);
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

    private T GetArrayItem<T>(string path)
    {
        int slashIndex = path.IndexOf('/');
        string propertyName = path.Substring(0, slashIndex);
        string indexString = path.Substring(slashIndex + 1);
        
        if (!int.TryParse(indexString, out int arrayIndex))
        {
            throw new ArgumentException($"Invalid array index: {indexString}");
        }

        byte[] propertyNameBytes = Encoding.UTF8.GetBytes(propertyName);
        
        // Get current array
        if (!_model.TryGet(propertyNameBytes, out ReadOnlySpan<byte> currentJson))
        {
            throw new KeyNotFoundException($"Property '{propertyName}' not found");
        }

        // Parse existing array
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{propertyName}' is not an array");
        }

        int currentIndex = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                throw new IndexOutOfRangeException($"Array index {arrayIndex} is out of range");
            }
            
            if (currentIndex == arrayIndex)
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
        
        throw new IndexOutOfRangeException($"Array index {arrayIndex} is out of range");
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




