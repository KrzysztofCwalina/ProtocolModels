using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;

public class SomeModel : JsonModel<SomeModel>
{
    public string Category { get; set; } = String.Empty;
    public string[] Names { get; set; } = Array.Empty<string>();
    public double[] Numbers { get; set; } = Array.Empty<double>();

    #region Additional Properties "Reflection" APIs.

    // do we need this method or reflection is fine?
    //protected override bool TryGetPropertyType(ReadOnlySpan<byte> name, out Type? type)
    //{
    //    type = null;
    //    if (name.SequenceEqual("category"u8)) type = typeof(string);
    //    else if (name.SequenceEqual("names"u8)) type = typeof(string[]);
    //    else if (name.SequenceEqual("numbers"u8)) type = typeof(double[]);
    //    return type != null;
    //}
    //protected override bool TryGetProperty(ReadOnlySpan<byte> name, out object? value)
    //{
    //    if(name.SequenceEqual("category"u8))
    //    {
    //        value = Category;
    //        return true;
    //    }
    //    if(name.SequenceEqual("names"u8))
    //    {
    //        value = Names;
    //        return true;
    //    }
    //    if(name.SequenceEqual("numbers"u8))
    //    {
    //        value = Numbers;
    //        return true;
    //    }
    //    value = default;
    //    return false;
    //}
    //protected override bool TrySetProperty(ReadOnlySpan<byte> name, object value)
    //{
    //    if(name.SequenceEqual("category"u8) && value is string category)
    //    {
    //        Category = category;
    //        return true;
    //    }
    //    if(name.SequenceEqual("names"u8) && value is string[] names)
    //    {
    //        Names = names;
    //        return true;
    //    }
    //    if(name.SequenceEqual("numbers"u8) && value is double[] numbers)
    //    {
    //        Numbers = numbers;
    //        return true;
    //    }

    //    Json.Set(name, value);

    //    return true;
    //}
    #endregion

    #region implementation of IJsonModel<T>
    protected override SomeModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        // TODO: dont use JsonDocument, use JsonReader directly
        JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        SomeModel model = new();
        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("category"))
            {
                model.Category = property.Value.GetString() ?? string.Empty;
            }
            else if (property.NameEquals("names"))
            {
                List<string> namesList = new List<string>();
                foreach (var item in property.Value.EnumerateArray())
                {
                    namesList.Add(item.GetString() ?? string.Empty);
                }
                model.Names = namesList.ToArray();
            }
            else if (property.NameEquals("numbers"))
            {
                List<double> numbersList = new List<double>();
                foreach (var item in property.Value.EnumerateArray())
                {
                    numbersList.Add(item.GetDouble());
                }
                model.Numbers = numbersList.ToArray();
            }
            else
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(property.Name);
                byte[] valueBytes = Encoding.UTF8.GetBytes(property.Value.GetRawText());
                model.Json.Set(nameBytes, (ReadOnlySpan<byte>)valueBytes);
            }
        }
        return model; // change this to return the created model instead of 'this'
    }

    // implementation of IJsonModel<T>
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

        WriteAdditionalProperties(writer, options); 

        writer.WriteEndObject();    
    }
    #endregion
}