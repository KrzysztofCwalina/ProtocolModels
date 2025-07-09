using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

// this is a datastructure for efficiently storing JSON properties using Dictionary
public partial struct AdditionalProperties
{
    // Dictionary-based storage instead of PropertyRecord[]
    private Dictionary<string, object>? _properties;

    private void Set(ReadOnlySpan<byte> name, object value)
    {
        if (_properties == null)
        {
            _properties = new Dictionary<string, object>();
        }
        
        string nameStr = Encoding.UTF8.GetString(name);
        _properties[nameStr] = value;
    }

    private object Get(ReadOnlySpan<byte> name)
    {
        if (_properties == null) ThrowPropertyNotFoundException(name);
        
        string nameStr = Encoding.UTF8.GetString(name);
        if (!_properties.TryGetValue(nameStr, out object? value))
        {
            ThrowPropertyNotFoundException(name);
        }
        
        return value!;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Write(Utf8JsonWriter writer)
    {
        if (_properties == null) return;
        
        foreach (var kvp in _properties)
        {
            WriteObjectAsJson(writer, kvp.Key, kvp.Value);
        }
    }

    public override string ToString()
    {
        if (_properties == null || _properties.Count == 0)
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        bool first = true;
        foreach (var kvp in _properties)
        {
            if (!first) sb.AppendLine(",");
            first = false;
            sb.Append(kvp.Key);
            sb.Append(": ");
            sb.Append(kvp.Value.ToString());
        }
        if (_properties.Count > 0)
            sb.AppendLine();
        return sb.ToString();
    }

    [Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private void ThrowPropertyNotFoundException(ReadOnlySpan<byte> name)
    {
        throw new KeyNotFoundException(Encoding.UTF8.GetString(name.ToArray()));
    }
}