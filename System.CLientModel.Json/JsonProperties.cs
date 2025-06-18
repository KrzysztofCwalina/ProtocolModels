using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;

internal struct JsonProperties
{
    private Dictionary<string, ReadOnlyMemory<byte>> _properties;
    public void Set(string name, ReadOnlySpan<byte> value)
    {
        if (_properties == null)
            _properties = new Dictionary<string, ReadOnlyMemory<byte>>();
        _properties[name] = value.ToArray();
    }
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        => Set(Encoding.UTF8.GetString(name), value);
    public bool TryGet(string name, out ReadOnlySpan<byte> value)
    {
        ReadOnlyMemory<byte> memory = default;
        if (_properties != null && _properties.TryGetValue(name, out memory))
        {
            value = memory.Span;
            return true;
        }
        value = default;
        return false;
    }
    public bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
    {
        string strName = Encoding.UTF8.GetString(name);
        return TryGet(strName, out value);
    }

    internal void Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        if (_properties != null)
        {
            foreach (var kvp in _properties)
            {
                writer.WritePropertyName(kvp.Key);
                writer.WriteRawValue(kvp.Value.Span, true); // true to escape the value
            }
        }
    }
}

