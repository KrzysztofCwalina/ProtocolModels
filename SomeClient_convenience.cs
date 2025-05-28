using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text.Json;

public partial class SomeClient
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual InputModel CreateProtocolInputModel(ConvenienceInputModel options)
    {
        List<string> names = new List<string>();
        List<double> numbers = new List<double>();
        foreach (var namedNumber in options.Numbers)
        {
            names.Add(namedNumber.Name);
            numbers.Add(namedNumber.Value);
        }
        var protocolModel = new InputModel
        {
            Names = names.ToArray(),
            Numbers = numbers.ToArray()
        };
        return protocolModel;
    }

    // convenience method
    public ClientResult<ConvenienceOutputModel> DoSomething(ConvenienceInputModel options, CancellationToken ct = default)
    {
        InputModel protocolModel = CreateProtocolInputModel(options);
        ClientResult<OutputModel> po = DoSomething(options.Id, protocolModel, ct);

        var convenienceModel = new ConvenienceOutputModel
        {
            // Map the properties from ProtocolOutputModel to ConvenienceOutputModel as needed
        };

        return ClientResult.FromValue(convenienceModel, po.GetRawResponse());
    }
}

public class ConvenienceInputModel : JsonModel<ConvenienceInputModel>
{
    public string Id { get; set; } = string.Empty;
    public IList<NamedNumber> Numbers { get; set; } = Array.Empty<NamedNumber>();

    protected override bool TryGetProperty(ReadOnlySpan<byte> name, out object value)
    {
        throw new NotImplementedException();
    }

    protected override bool HasProperty(ReadOnlySpan<byte> name)
    {
        throw new NotImplementedException();
    }

    protected override bool TrySetProperty(ReadOnlySpan<byte> name, object value)
    {
        throw new NotImplementedException();
    }

    protected override void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    protected override ConvenienceInputModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }
}

public class ConvenienceOutputModel : JsonModel<ConvenienceOutputModel>
{
    protected override ConvenienceOutputModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    protected override bool HasProperty(ReadOnlySpan<byte> name)
    {
        throw new NotImplementedException();
    }

    protected override bool TryGetProperty(ReadOnlySpan<byte> name, out object value)
    {
        throw new NotImplementedException();
    }

    protected override bool TrySetProperty(ReadOnlySpan<byte> name, object value)
    {
        throw new NotImplementedException();
    }

    protected override void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }
}

