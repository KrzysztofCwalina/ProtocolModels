using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;

public class SomeModel : JsonModel<SomeModel>
{
    public string Category { get; set; } = String.Empty;
    public string[] Names { get; set; } = Array.Empty<string>();
    public double[] Numbers { get; set; } = Array.Empty<double>();

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
}