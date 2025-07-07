using System.ClientModel.Primitives;
using System.Text.Json;


// this is option #1. 
// this model tries to synchronize JSON and CLR properties
// I think it's too complex
public class SynchronizingModel : ExtensibleModel<SynchronizingModel>
{
    public string Category { get; set; } = String.Empty;
    public int Id { get; set; } = 0;

    public string[] Names { get; set; } = Array.Empty<string>();
    public double[] Numbers { get; set; } = Array.Empty<double>();

    protected override SynchronizingModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        SynchronizingModel model = new();
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.NameEquals("category"u8))
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    model.Category = property.Value.GetString() ?? String.Empty;
                }
                else if (property.Value.ValueKind == JsonValueKind.Null)
                {
                    model.Category = null;
                }
                else
                {
                    Json.Set("category"u8, property.Value.GetRawText());
                }
            }
            if (property.NameEquals("id"u8))
            {
                if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    // TODO: this needs to handle number not being an int32
                    model.Id = property.Value.GetInt32();
                }
                else
                {
                    Json.Set("id"u8, property.Value.GetRawText());
                }
            }
            else if (property.NameEquals("category"u8))
            {
                model.Category = property.Value.GetString() ?? String.Empty;
            }
            else if (property.NameEquals("id"u8))
            {
                model.Id = property.Value.GetInt32();
            }
            else if (property.NameEquals("names"u8))
            {
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
        if (!ContainsAdditionalProperty("category"u8)){
            writer.WriteString("category"u8, Category);
        }
        if (!ContainsAdditionalProperty("id"u8)){
            writer.WriteNumber("id"u8, Id);
        }
        if (!ContainsAdditionalProperty("names"u8)){
            writer.WritePropertyName("names"u8);
            writer.WriteStartArray();
            foreach (var name in Names)
            {
                writer.WriteStringValue(name);
            }
            writer.WriteEndArray();
        }
        if (!ContainsAdditionalProperty("numbers"u8))
        {
            writer.WritePropertyName("numbers"u8);
            writer.WriteStartArray();
            foreach (var number in Numbers)
            {
                writer.WriteNumberValue(number);
            }
            writer.WriteEndArray();
        }

        WriteAdditionalProperties(writer, options);

        writer.WriteEndObject();    
    }
}