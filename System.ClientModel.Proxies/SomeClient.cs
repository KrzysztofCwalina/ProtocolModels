using System.ClientModel;
using System.ClientModel.Primitives;

// scenarios:
// 1. Add a property to output (serialization only)
// 2. Ignore a property from input (serialization)
// 3. Change property type in output (serialization only)
// 4. Ignore a property from output (deserialization)
// 5. Read spillover property (deserialization)

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
    public ClientResult<SynchronizingModel> DoSomething(string name, SynchronizingModel model, CancellationToken ct = default)
    {
        BinaryContent content = CreateDoSomethingContent(model);
        ClientResult result = DoSomething(name, content, new RequestOptions { CancellationToken = ct });
        PipelineResponse response = result.GetRawResponse();
        SynchronizingModel value = CreateDoSomethingResult(response);
        return ClientResult.FromValue(value, response);
    }

    // virtual so that the message can be intercepted
    protected virtual PipelineMessage CreateDoSomethingRequest(string pathParameter, BinaryContent content, RequestOptions options)
    {
        PipelineMessage message = _pipeline.CreateMessage();
        // TODO: set up the message with the request details
        return message;
    }

    protected virtual BinaryContent CreateDoSomethingContent(SynchronizingModel content)
    {
        ModelReaderWriterOptions options = new("W");
        BinaryData bytes = ModelReaderWriter.Write(content, options);
        return BinaryContent.Create(bytes);
    }

    protected virtual SynchronizingModel CreateDoSomethingResult(PipelineResponse response)
    {
        return ModelReaderWriter.Read<SynchronizingModel>(response.Content)!;
    }
}
public class  SomeClientOptions : ClientPipelineOptions
{
    public Uri Endpoint { get; set; }
    public ModelProxy[] ModelReaderWriterProxies { get; set; } = Array.Empty<ModelProxy>();
}


