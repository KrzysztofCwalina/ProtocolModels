using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

// this is option #2. 
// this model does not try to synchronize JSON and CLR properties; the JSON properties are for MRW only/mainly
// I think it's too complex
public class SimpleModel: JsonModel<SimpleModel>
{
    private JsonPatch _extensions = new();
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ref JsonPatch Extensions => ref _extensions;

    public string? Category { get; set; }
    public int Id { get; set; } = 0;
    public short[] Numbers { get; set; } = Array.Empty<short>();
    public SubModel SubModel { get; set; } = new();

    protected override SimpleModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        SimpleModel model = new();
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.NameEquals("category"u8))
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    model.Category = property.Value.GetString();
                }
                else
                {
                    Extensions.Set("category"u8, property.Value.GetRawText());
                }
            }
            if (property.NameEquals("id"u8))
            {
                if (property.Value.ValueKind == JsonValueKind.Number &&
                    property.Value.TryGetInt32(out int id))
                {
                    model.Id = id;
                }
                else
                {
                    Extensions.Set("id"u8, property.Value.GetRawText());
                }
            }
            else if (property.NameEquals("numbers"u8))
            {
                if (property.Value.ValueKind != JsonValueKind.Array)
                {
                    try
                    {
                        model.Numbers = property.Value.Deserialize<short[]>(); // is there a Try way to do it?
                        Extensions.Set("names"u8, property.Value.GetRawText());
                        continue;
                    }
                    catch (JsonException)
                    {
                        Extensions.Set("numbers"u8, property.Value.GetRawText());
                        continue;
                    }
                }
                else
                {
                    Extensions.Set("numbers"u8, property.Value.GetRawText());
                    continue;
                }
            }
            else
            {
                byte[] name = Encoding.UTF8.GetBytes(property.Name);
                Extensions.Set(name, property.Value.GetRawText());
            }
        }
        return model; // change this to return the created model instead of 'this'
    }

    protected override void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        writer.WriteStartObject();
        if (!Extensions.Contains("category"u8)){
            writer.WriteString("category"u8, Category);
        }
        if (!Extensions.Contains("id"u8)){
            writer.WriteNumber("id"u8, Id);
        }
        if (!Extensions.Contains("names"u8)){
            writer.WritePropertyName("names"u8);
            writer.WriteStartArray();
            foreach (short name in Numbers)
            {
                writer.WriteNumberValue(name);
            }
            writer.WriteEndArray();
        }
        if (!Extensions.Contains("numbers"u8))
        {
            writer.WritePropertyName("numbers"u8);
            writer.WriteStartArray();
            foreach (var number in Numbers)
            {
                writer.WriteNumberValue(number);
            }
            writer.WriteEndArray();
        }

        Extensions.Write(writer);

        writer.WriteEndObject();    
    }
}

public class SubModel
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public JsonPatch __extensions { get; } = new();

    public int Foo { get; set; } = 0;
    public string Bar { get; set; } = String.Empty;
}