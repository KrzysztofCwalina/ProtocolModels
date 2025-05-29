using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

public readonly struct JsonView
{
    private readonly IJsonModel _model;
    private readonly byte[] _path;

    internal JsonView(IJsonModel model) : this(model, Array.Empty<byte>())
    {}

    private JsonView(IJsonModel model, byte[] path)
    {
        _model = model;
        _path = path;
    }

    public void Set(ReadOnlySpan<byte> name, string value)
    {
        MemoryStream stream = new(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStringValue(value);
        writer.Flush();
        ReadOnlySpan<byte> json = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        Set(name, json);
    }

    public void Set(ReadOnlySpan<byte> name, double value)
    {
        MemoryStream stream = new(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteNumberValue(value);
        writer.Flush();
        ReadOnlySpan<byte> json = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        Set(name, json);
    }

    // add or change value
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
        => _model.Set(name, json);
    public void Set(string name, ReadOnlySpan<byte> json)
        => Set(Encoding.UTF8.GetBytes(name), json);

    // TODO: add Set overloads for other types (int, bool, etc.)

    public JsonView this[string name]
    {
        get => new JsonView(_model, Encoding.UTF8.GetBytes(name));
    }
    public string GetString(ReadOnlySpan<byte> name)
    {
        if (_model.TryGet(name, out ReadOnlySpan<byte> value) && value.Length > 0)
            return value.AsString();
        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found or has no value.");
    }
    public double GetDouble(ReadOnlySpan<byte> name)
    {
        //Span<byte> fullPath = stackalloc byte[_path.Length + name.Length + 1];
        //_path.AsSpan().CopyTo(fullPath);
        //fullPath[_path.Length] = (byte)'/';
        //name.CopyTo(fullPath.Slice(_path.Length + 1));
        ReadOnlySpan<byte> value;
        if (_path.Length > 0)
        {
            if (!_model.TryGet(_path, out value)) throw new KeyNotFoundException();
            return value.GetDouble("/value"u8); // TODO: do not hardcode
        }
        else
        {
            if (!_model.TryGet(name, out value)) throw new KeyNotFoundException();
            return value.AsDouble();
        }
    }
    // get spillover (or real?) property or array value
    public bool TryGet(string name, out ReadOnlySpan<byte> value)
        => TryGet(Encoding.UTF8.GetBytes(name), out value);
    public bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value) 
        => _model.TryGet(name, out value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        => _model.WriteAdditionalProperties(writer, options);
}


