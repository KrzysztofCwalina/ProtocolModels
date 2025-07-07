using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text.Json;

public partial class SomeClient
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual SynchronizingModel CreateProtocolInputModel(ConvenienceSomeModel options)
    {
        throw new NotImplementedException();
    }

    // convenience method
    public ClientResult<ConvenienceSomeModel> DoSomething(ConvenienceSomeModel options, CancellationToken ct = default)
    {
        SynchronizingModel protocolModel = CreateProtocolInputModel(options);
        ClientResult<SynchronizingModel> po = DoSomething(options.Id, protocolModel, ct);

        var convenienceModel = new ConvenienceSomeModel
        {
            // Map the properties from ProtocolOutputModel to ConvenienceOutputModel as needed
        };

        return ClientResult.FromValue(convenienceModel, po.GetRawResponse());
    }
}

public class ConvenienceSomeModel : ExtensibleModel<ConvenienceSomeModel>
{
    public string Id { get; set; } = string.Empty;

    protected override bool TryGetClrProperty(ReadOnlySpan<byte> name, out object value)
    {
        throw new NotImplementedException();
    }

    protected override bool TrySetClrProperty(ReadOnlySpan<byte> name, object value)
    {
        throw new NotImplementedException();
    }

    protected override void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    protected override ConvenienceSomeModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    protected override bool TryGetClrPropertyType(ReadOnlySpan<byte> name, out Type? type)
    {
        throw new NotImplementedException();
    }
}

