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

