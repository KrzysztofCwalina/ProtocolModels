using System.ClientModel.Primitives;
using System.Text.Json;

public class SomeModel : ExtensibleModel<SomeModel>
{
    public string Category { get; set; } = String.Empty;
    public int Id { get; set; } = 0;

    public string[] Names { get; set; } = Array.Empty<string>();
    public double[] Numbers { get; set; } = Array.Empty<double>();

    protected override SomeModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        SomeModel model = new();
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.NameEquals("category"u8)) model.Category = property.Value.GetString();
            if (property.NameEquals("id"u8)) model.Id = property.Value.GetInt32();
            else if (property.NameEquals("category"u8)) {
                model.Category = property.Value.GetString() ?? String.Empty;
            }
            else if (property.NameEquals("id"u8)) {
                model.Id = property.Value.GetInt32();
            }
            else if (property.NameEquals("names"u8)) {
                model.Names = property.Value.Deserialize<string[]>();
            }
            else if (property.NameEquals("numbers"u8))
            {
                model.Numbers = property.Value.Deserialize<double[]>();
            }
            else
            {
                ReadAdditionalProperty(model.Json, property);
            }
        }
        return model; // change this to return the created model instead of 'this'
    }

    protected override void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("category"u8, Category);
        writer.WriteNumber("id"u8, Id);
        writer.WritePropertyName("names"u8);
        writer.WriteStartArray();
        foreach (var name in Names)
        {
            writer.WriteStringValue(name);
        }
        writer.WriteEndArray();
        writer.WritePropertyName("numbers"u8);
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