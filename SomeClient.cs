using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

public partial class SomeClient
{
    readonly ClientPipeline _pipeline;

    public SomeClient(SomeClientOptions options = default)
    {
        _pipeline = ClientPipeline.Create();
    }

    // untyped protocol method
    public ClientResult DoSomething(string pathParameter, BinaryContent content, RequestOptions options = default)
    {
        using PipelineMessage message = CreateDoSomethingRequest(pathParameter, content, options);
        //_pipeline.Send(message);

        PipelineResponseImpl response = new(200, BinaryData.FromString("{\"confidence\": 0.95, \"text\":\"some text\"}"));

        return ClientResult.FromResponse(response);
    }

    // typed protocol method (allows renames, but not restructuring)
    public ClientResult<OutputModel> DoSomething(string name, InputModel model, CancellationToken ct = default)
    {
        BinaryContent content = CreateDoSomethingContent(model);
        ClientResult result = DoSomething(name, content, new RequestOptions { CancellationToken = ct });
        PipelineResponse response = result.GetRawResponse();
        OutputModel value = CreateDoSomethingResult(response);
        return ClientResult.FromValue(value, response);
    }

    // virtual so that the message can be intercepted
    protected virtual PipelineMessage CreateDoSomethingRequest(string pathParameter, BinaryContent content, RequestOptions options)
    {
        PipelineMessage message = _pipeline.CreateMessage();
        // TODO: set up the message with the request details
        return message;
    }

    protected virtual BinaryContent CreateDoSomethingContent(InputModel content)
    {
        ModelReaderWriterOptions options = new("W");
        BinaryData bytes = ModelReaderWriter.Write(content, options);
        return BinaryContent.Create(bytes);
    }

    protected virtual OutputModel CreateDoSomethingResult(PipelineResponse response)
    {
        return ModelReaderWriter.Read<OutputModel>(response.Content)!;
    }
}
public class  SomeClientOptions : ClientPipelineOptions
{
    public Uri Endpoint { get; set; }
    public ModelProxy[] ModelReaderWriterProxies { get; set; } = Array.Empty<ModelProxy>();
}
public class OutputModel : JsonModel<OutputModel>
{
    public float Confidence { get; internal set; }
    protected override OutputModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("confidence"))
            {
                Confidence = property.Value.GetSingle();
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

    protected override bool HasProperty(ReadOnlySpan<byte> name)
    {
        if(name.SequenceEqual("confidence"u8)) return true;
        return false;
    }

    protected override bool TryGetProperty(ReadOnlySpan<byte> name, out object value)
    {
        if(name.SequenceEqual("confidence"u8))
        {
            value = Confidence;
            return true;
        }
        value = default;
        return false;
    }

    protected override bool TrySetProperty(ReadOnlySpan<byte> name, object value)
    {
        throw new NotImplementedException();
    }

    protected override void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    public static implicit operator OutputModel(string json)
    {
        BinaryData data = BinaryData.FromString(json);
        return ModelReaderWriter.Read<OutputModel>(data)!;
    
    }
    public static implicit operator OutputModel(ReadOnlySpan<byte> json)
    {
        BinaryData data = BinaryData.FromBytes(json.ToArray());
        return ModelReaderWriter.Read<OutputModel>(data)!;

    }
}
public class InputModel : JsonModel<InputModel>
{
    public string Category { get; set; }
    public string[] Names { get; set; } = Array.Empty<string>();
    public double[] Numbers { get; set; } = Array.Empty<double>();

    protected override InputModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    protected override bool HasProperty(ReadOnlySpan<byte> name)
    {
        if(name.SequenceEqual("category"u8)) return true;
        if(name.SequenceEqual("names"u8)) return true;
        if(name.SequenceEqual("numbers"u8)) return true;
        return false;
    }

    protected override bool TryGetProperty(ReadOnlySpan<byte> name, out object value)
    {
        throw new NotImplementedException();
    }

    protected override bool TrySetProperty(ReadOnlySpan<byte> name, object value)
    {
        if(name.SequenceEqual("category"u8) && value is string category)
        {
            Category = category;
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

