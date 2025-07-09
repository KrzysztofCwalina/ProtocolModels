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
    // Dictionary-based storage using UTF8 byte arrays as keys
    private Dictionary<byte[], object>? _properties;

    // Custom equality comparer for byte arrays to enable content-based comparison
    private sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public static readonly ByteArrayEqualityComparer Instance = new();

        public bool Equals(byte[]? x, byte[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.AsSpan().SequenceEqual(y.AsSpan());
        }

        public int GetHashCode(byte[] obj)
        {
            if (obj is null) return 0;
            
            // Simple hash code implementation for byte arrays
            var hash = new HashCode();
            hash.AddBytes(obj);
            return hash.ToHashCode();
        }
    }

    private void Set(ReadOnlySpan<byte> name, object value)
    {
        if (_properties == null)
        {
            _properties = new Dictionary<byte[], object>(ByteArrayEqualityComparer.Instance);
        }
        
        byte[] nameBytes = name.ToArray();
        _properties[nameBytes] = value;
    }

    private object Get(ReadOnlySpan<byte> name)
    {
        if (_properties == null) ThrowPropertyNotFoundException(name);
        
        byte[] nameBytes = name.ToArray();
        if (!_properties.TryGetValue(nameBytes, out object? value))
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
            string propertyName = Encoding.UTF8.GetString(kvp.Key);
            WriteObjectAsJson(writer, propertyName, kvp.Value);
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
            string propertyName = Encoding.UTF8.GetString(kvp.Key);
            sb.Append(propertyName);
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