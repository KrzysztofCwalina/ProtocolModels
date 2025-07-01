using System.ClientModel.Primitives;
using System.Text.Json;
// TOOD: what do we do with struct models?

public abstract class JsonModel<T> : IJsonModel<T>, IPersistableModel<T>
{
    protected abstract void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options);
    protected abstract T CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options);

    #region MRW
    T IJsonModel<T>.Create(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
        => CreateCore(ref reader, options);

    T IPersistableModel<T>.Create(BinaryData data, ModelReaderWriterOptions options)
    {
        Utf8JsonReader reader = new Utf8JsonReader(data.ToMemory().Span);
        return CreateCore(ref reader, options);
    }

    string IPersistableModel<T>.GetFormatFromOptions(ModelReaderWriterOptions options) => "J";

    void IJsonModel<T>.Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        => WriteCore(writer, options);

    BinaryData IPersistableModel<T>.Write(ModelReaderWriterOptions options)
    {
        MemoryStream stream = new MemoryStream();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        WriteCore(writer, options);
        byte[] buffer = stream.GetBuffer();
        ReadOnlyMemory<byte> memory = buffer.AsMemory(0, (int)stream.Position);
        return new BinaryData(memory);
    }
    #endregion
}

