using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;

public class InputModel : JsonModel<InputModel>
{
    public string Category { get; set; }
    public string[] Names { get; set; } = Array.Empty<string>();
    public double[] Numbers { get; set; } = Array.Empty<double>();

    public static InputModel operator+(InputModel model, ReadOnlySpan<byte> json)
    {
        var sum = ModelReaderWriter.Read<InputModel>(BinaryData.FromBytes(json.ToArray()))!;
        if (sum.TryGetProperty("category"u8, out _))
        {
            sum.Category = model.Category; // TODO: don't we need a copy?
        }
        if (sum.TryGetProperty("names"u8, out _))
        {
            sum.Names = model.Names;
        }
        if (sum.TryGetProperty("numbers"u8, out _))
        {
            sum.Numbers = model.Numbers;
        }
        return sum;
    }    
    
    protected override InputModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("category"))
            {
                Category = property.Value.GetString() ?? string.Empty;
            }
            else if (property.NameEquals("names"))
            {
                List<string> namesList = new List<string>();
                foreach (var item in property.Value.EnumerateArray())
                {
                    namesList.Add(item.GetString() ?? string.Empty);
                }
                Names = namesList.ToArray();
            }
            else if (property.NameEquals("numbers"))
            {
                List<double> numbersList = new List<double>();
                foreach (var item in property.Value.EnumerateArray())
                {
                    numbersList.Add(item.GetDouble());
                }
                Numbers = numbersList.ToArray();
            }
            else
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(property.Name);
                byte[] valueBytes = Encoding.UTF8.GetBytes(property.Value.GetRawText());
                Json.Set(nameBytes, valueBytes);
            }
        }
        return this;
    }

    protected override Type GetPropertyType(ReadOnlySpan<byte> name)
    {
        if (name.SequenceEqual("category"u8)) return typeof(string);
        if (name.SequenceEqual("names"u8)) return typeof(string[]);
        if (name.SequenceEqual("numbers"u8)) return typeof(double[]);
        return null;
    }

    protected override bool TryGetProperty(ReadOnlySpan<byte> name, out object value)
    {
        if(name.SequenceEqual("category"u8))
        {
            value = Category;
            return true;
        }
        if(name.SequenceEqual("names"u8))
        {
            value = Names;
            return true;
        }
        if(name.SequenceEqual("numbers"u8))
        {
            value = Numbers;
            return true;
        }
        value = default;
        return false;
    }

    protected override bool TrySetProperty(ReadOnlySpan<byte> name, object value)
    {
        if(name.SequenceEqual("category"u8) && value is string category)
        {
            Category = category;
            return true;
        }
        if(name.SequenceEqual("names"u8) && value is string[] names)
        {
            Names = names;
            return true;
        }
        if(name.SequenceEqual("numbers"u8) && value is double[] numbers)
        {
            Numbers = numbers;
            return true;
        }
        throw new NotImplementedException();
    }

    protected override void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("category", Category);
        writer.WritePropertyName("names");
        writer.WriteStartArray();
        foreach (var name in Names)
        {
            writer.WriteStringValue(name);
        }
        writer.WriteEndArray();
        writer.WritePropertyName("numbers");
        writer.WriteStartArray();
        foreach (var number in Numbers)
        {
            writer.WriteNumberValue(number);
        }
        writer.WriteEndArray();
        Json.Write(writer, options); 
        writer.WriteEndObject();    
    }
}

public class InputModelJson : IJsonModel<InputModelJson>, IJsonModel
{
    private ReadOnlyMemory<byte> _json;

    public InputModelJson()
    {
        // Initialize with empty JSON object
        _json = "{}"u8.ToArray();
    }

    public JsonView Json => new JsonView(this);

    public string Category
    {
        get
        {
            if (_json.Length <= 2) return string.Empty; // Empty JSON object
            return _json.Span.GetString("/category"u8);
        }
        set
        {
            SetProperty("/category"u8, writer => writer.WriteString("category", value));
        }
    }

    public string[] Names
    {
        get
        {
            if (_json.Length <= 2) return Array.Empty<string>(); // Empty JSON object
            return _json.Span.GetStringArray("/names"u8);
        }
        set
        {
            SetProperty("/names"u8, writer =>
            {
                writer.WritePropertyName("names");
                writer.WriteStartArray();
                foreach (var name in value)
                {
                    writer.WriteStringValue(name);
                }
                writer.WriteEndArray();
            });
        }
    }

    public double[] Numbers
    {
        get
        {
            if (_json.Length <= 2) return Array.Empty<double>(); // Empty JSON object
            return _json.Span.GetDoubleArray("/numbers"u8);
        }
        set
        {
            SetProperty("/numbers"u8, writer =>
            {
                writer.WritePropertyName("numbers");
                writer.WriteStartArray();
                foreach (var number in value)
                {
                    writer.WriteNumberValue(number);
                }
                writer.WriteEndArray();
            });
        }
    }

    private void SetProperty(ReadOnlySpan<byte> jsonPointer, Action<Utf8JsonWriter> writeProperty)
    {
        // Parse existing JSON
        JsonDocument doc = JsonDocument.Parse(_json);
        
        // Create new JSON with the updated property
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartObject();
        
        // Write the new/updated property first
        writeProperty(writer);
        
        // Copy all other properties from existing JSON
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            var propertyName = property.Name;
            // Skip the property we're updating
            if (jsonPointer.Length > 1 && property.NameEquals(Encoding.UTF8.GetString(jsonPointer.Slice(1))))
            {
                continue;
            }
            
            writer.WritePropertyName(propertyName);
            property.Value.WriteTo(writer);
        }
        
        writer.WriteEndObject();
        writer.Flush();
        
        // Update the JSON field
        _json = stream.ToArray();
    }

    #region IJsonModel implementation

    bool IJsonModel.TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
    {
        if (name.SequenceEqual("category"u8))
        {
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            writer.WriteStringValue(Category);
            writer.Flush();
            value = stream.GetBuffer().AsSpan(0, (int)stream.Position);
            return true;
        }
        if (name.SequenceEqual("names"u8))
        {
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            writer.WriteStartArray();
            foreach (var name2 in Names)
            {
                writer.WriteStringValue(name2);
            }
            writer.WriteEndArray();
            writer.Flush();
            value = stream.GetBuffer().AsSpan(0, (int)stream.Position);
            return true;
        }
        if (name.SequenceEqual("numbers"u8))
        {
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            writer.WriteStartArray();
            foreach (var number in Numbers)
            {
                writer.WriteNumberValue(number);
            }
            writer.WriteEndArray();
            writer.Flush();
            value = stream.GetBuffer().AsSpan(0, (int)stream.Position);
            return true;
        }
        
        // Try to get from JSON for additional properties
        string propertyName = Encoding.UTF8.GetString(name);
        ReadOnlySpan<byte> jsonPointer = Encoding.UTF8.GetBytes($"/{propertyName}");
        
        if (_json.Span.TryGetElement(jsonPointer, out JsonElement element))
        {
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            element.WriteTo(writer);
            writer.Flush();
            value = stream.GetBuffer().AsSpan(0, (int)stream.Position);
            return true;
        }
        
        value = default;
        return false;
    }

    void IJsonModel.Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        if (name.SequenceEqual("category"u8))
        {
            var reader = new Utf8JsonReader(value);
            reader.Read();
            Category = reader.GetString()!;
            return;
        }
        if (name.SequenceEqual("names"u8))
        {
            var doc = JsonDocument.Parse(value.ToArray());
            var list = new List<string>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                list.Add(element.GetString() ?? string.Empty);
            }
            Names = list.ToArray();
            return;
        }
        if (name.SequenceEqual("numbers"u8))
        {
            var doc = JsonDocument.Parse(value.ToArray());
            var list = new List<double>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                list.Add(element.GetDouble());
            }
            Numbers = list.ToArray();
            return;
        }
        
        // For additional properties, add them to the JSON
        string propertyName = Encoding.UTF8.GetString(name);
        SetPropertyFromJson(propertyName, value);
    }

    private void SetPropertyFromJson(string propertyName, ReadOnlySpan<byte> value)
    {
        // Parse existing JSON
        JsonDocument doc = JsonDocument.Parse(_json);
        
        // Create new JSON with the updated property
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartObject();
        
        // Write the new/updated property first
        writer.WritePropertyName(propertyName);
        writer.WriteRawValue(value, skipInputValidation: true);
        
        // Copy all other properties from existing JSON
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Name != propertyName)
            {
                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }
        }
        
        writer.WriteEndObject();
        writer.Flush();
        
        // Update the JSON field
        _json = stream.ToArray();
    }

    Type? IJsonModel.GetPropertyType(ReadOnlySpan<byte> name)
    {
        if (name.SequenceEqual("category"u8)) return typeof(string);
        if (name.SequenceEqual("names"u8)) return typeof(string[]);
        if (name.SequenceEqual("numbers"u8)) return typeof(double[]);
        return null;
    }

    void IJsonModel.WriteAdditionalProperties(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        // Parse the existing JSON and write out all additional properties (non-core properties)
        JsonDocument doc = JsonDocument.Parse(_json);
        
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            // Skip core properties
            if (property.Name != "category" && property.Name != "names" && property.Name != "numbers")
            {
                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }
        }
    }

    #endregion

    #region IJsonModel<T> and IPersistableModel<T> implementation

    InputModelJson IJsonModel<InputModelJson>.Create(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        // Read the entire JSON document and store it
        JsonDocument doc = JsonDocument.ParseValue(ref reader);
        
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        doc.WriteTo(writer);
        writer.Flush();
        
        var newModel = new InputModelJson();
        newModel._json = stream.ToArray();
        return newModel;
    }

    InputModelJson IPersistableModel<InputModelJson>.Create(BinaryData data, ModelReaderWriterOptions options)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data.ToMemory().Span);
        return ((IJsonModel<InputModelJson>)this).Create(ref reader, options);
    }

    string IPersistableModel<InputModelJson>.GetFormatFromOptions(ModelReaderWriterOptions options) => "J";

    void IJsonModel<InputModelJson>.Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        // Parse the existing JSON and write it out
        JsonDocument doc = JsonDocument.Parse(_json);
        
        writer.WriteStartObject();
        
        // Write all properties from our JSON storage
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            writer.WritePropertyName(property.Name);
            property.Value.WriteTo(writer);
        }
        
        writer.WriteEndObject();
    }

    BinaryData IPersistableModel<InputModelJson>.Write(ModelReaderWriterOptions options)
    {
        MemoryStream stream = new MemoryStream();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        ((IJsonModel<InputModelJson>)this).Write(writer, options);
        writer.Flush();
        byte[] buffer = stream.GetBuffer();
        ReadOnlyMemory<byte> memory = buffer.AsMemory(0, (int)stream.Position);
        return new BinaryData(memory);
    }

    #endregion

    public static InputModelJson operator+(InputModelJson model, ReadOnlySpan<byte> json)
    {
        // Parse the new JSON to add
        JsonDocument newDoc = JsonDocument.Parse(json.ToArray());
        
        // Parse the existing JSON
        JsonDocument existingDoc = JsonDocument.Parse(model._json);
        
        // Create merged JSON
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartObject();
        
        // First write all existing properties
        foreach (var property in existingDoc.RootElement.EnumerateObject())
        {
            writer.WritePropertyName(property.Name);
            property.Value.WriteTo(writer);
        }
        
        // Then write all new properties (this will overwrite existing ones with same name)
        foreach (var property in newDoc.RootElement.EnumerateObject())
        {
            // Only add if it doesn't already exist (to avoid duplicates)
            bool exists = false;
            foreach (var existingProperty in existingDoc.RootElement.EnumerateObject())
            {
                if (existingProperty.Name == property.Name)
                {
                    exists = true;
                    break;
                }
            }
            
            if (!exists)
            {
                writer.WritePropertyName(property.Name);
                property.Value.WriteTo(writer);
            }
        }
        
        writer.WriteEndObject();
        writer.Flush();
        
        // Create new model with merged JSON
        var result = new InputModelJson();
        result._json = stream.ToArray();
        return result;
    }
}

public class NamedNumber
{
    public NamedNumber(string name, int value)
    {
        Name = name;
        Value = value;
    }
    public string Name { get; }
    public int Value { get; }
}

