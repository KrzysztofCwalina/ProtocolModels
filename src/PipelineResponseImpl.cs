using System.ClientModel.Primitives;

internal class PipelineResponseImpl : PipelineResponse
{
    int _status;
    BinaryData _content;
    public PipelineResponseImpl(int status, BinaryData content)
    {
        _status = status;
        _content = content;
    }
    public override int Status => _status;

    public override string ReasonPhrase => throw new NotImplementedException();

    public override Stream? ContentStream { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override BinaryData Content => _content;

    protected override PipelineResponseHeaders HeadersCore => throw new NotImplementedException();

    public override BinaryData BufferContent(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        throw new NotImplementedException();
    }
}
