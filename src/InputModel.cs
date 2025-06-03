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

public class InputModelJson : JsonModel<InputModelJson>
{
    private ReadOnlyMemory<byte> _json;

    public InputModelJson()
    {
        // Initialize with empty JSON object
        _json = "{}"u8.ToArray();
    }

    public string Category
    {
        get
        {
            if (_json.Length <= 2) return string.Empty; // Empty JSON object
            try
            {
                return _json.Span.GetString("/category"u8);
            }
            catch
            {
                return string.Empty;
            }
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
            try
            {
                // Use JsonDocument to parse the array
                var reader = new Utf8JsonReader(_json.Span);
                reader.Read(); // Read start object
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("names"u8))
                    {
                        reader.Read(); // Read start array
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            var names = new List<string>();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (reader.TokenType == JsonTokenType.String)
                                {
                                    names.Add(reader.GetString() ?? string.Empty);
                                }
                            }
                            return names.ToArray();
                        }
                        break;
                    }
                }
                return Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
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
            try
            {
                // Use JsonDocument to parse the array
                var reader = new Utf8JsonReader(_json.Span);
                reader.Read(); // Read start object
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("numbers"u8))
                    {
                        reader.Read(); // Read start array
                        if (reader.TokenType == JsonTokenType.StartArray)
                        {
                            var numbers = new List<double>();
                            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                            {
                                if (reader.TokenType == JsonTokenType.Number)
                                {
                                    numbers.Add(reader.GetDouble());
                                }
                            }
                            return numbers.ToArray();
                        }
                        break;
                    }
                }
                return Array.Empty<double>();
            }
            catch
            {
                return Array.Empty<double>();
            }
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

    protected override bool TryGetProperty(ReadOnlySpan<byte> name, out object value)
    {
        if (name.SequenceEqual("category"u8))
        {
            value = Category;
            return true;
        }
        if (name.SequenceEqual("names"u8))
        {
            value = Names;
            return true;
        }
        if (name.SequenceEqual("numbers"u8))
        {
            value = Numbers;
            return true;
        }
        value = default!;
        return false;
    }

    protected override Type GetPropertyType(ReadOnlySpan<byte> name)
    {
        if (name.SequenceEqual("category"u8)) return typeof(string);
        if (name.SequenceEqual("names"u8)) return typeof(string[]);
        if (name.SequenceEqual("numbers"u8)) return typeof(double[]);
        return null!;
    }

    protected override bool TrySetProperty(ReadOnlySpan<byte> name, object value)
    {
        if (name.SequenceEqual("category"u8) && value is string category)
        {
            Category = category;
            return true;
        }
        if (name.SequenceEqual("names"u8) && value is string[] names)
        {
            Names = names;
            return true;
        }
        if (name.SequenceEqual("numbers"u8) && value is double[] numbers)
        {
            Numbers = numbers;
            return true;
        }
        return false;
    }

    protected override void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        // Parse the existing JSON to merge with additional properties
        JsonDocument doc = JsonDocument.Parse(_json);
        
        writer.WriteStartObject();
        
        // Write all properties from our JSON storage
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            writer.WritePropertyName(property.Name);
            property.Value.WriteTo(writer);
        }
        
        // Write any additional properties from the JsonView
        Json.Write(writer, options);
        
        writer.WriteEndObject();
    }

    protected override InputModelJson CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
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

    public static InputModelJson operator+(InputModelJson model, ReadOnlySpan<byte> json)
    {
        var sum = ModelReaderWriter.Read<InputModelJson>(BinaryData.FromBytes(json.ToArray()))!;
        if (sum.TryGetProperty("category"u8, out _))
        {
            sum.Category = model.Category;
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

