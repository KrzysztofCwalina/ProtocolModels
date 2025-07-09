using System.Buffers.Binary;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

// this is a datastructure for efficiently storing JSON properties using Dictionary
public partial struct DictionaryStore
{
    // Value kinds for encoding type information in byte arrays
    private enum ValueKind : byte
    {
        Json = 1,
        Int32 = 2,
        Utf8String = 3,
        Removed = 4,
        Null = 5,
        BooleanTrue = 6,
        BooleanFalse = 7,
    }

    // Dictionary-based storage using UTF8 byte arrays as keys and encoded byte arrays as values
    private Dictionary<byte[], byte[]>? _properties;

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

    // Helper methods to encode objects to byte arrays (similar to PropertyRecord format)
    private static byte[] EncodeValue(object value)
    {
        switch (value)
        {
            case string s:
                // Store as JSON string representation
                byte[] jsonStringBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(s));
                byte[] stringResult = new byte[1 + jsonStringBytes.Length];
                stringResult[0] = (byte)ValueKind.Utf8String;
                jsonStringBytes.CopyTo(stringResult, 1);
                return stringResult;

            case int i:
                // Store as JSON number representation
                byte[] jsonIntBytes = Encoding.UTF8.GetBytes(i.ToString());
                byte[] intResult = new byte[1 + jsonIntBytes.Length];
                intResult[0] = (byte)ValueKind.Int32;
                jsonIntBytes.CopyTo(intResult, 1);
                return intResult;

            case bool b:
                // Store as JSON boolean representation
                byte[] jsonBoolBytes = Encoding.UTF8.GetBytes(b ? "true" : "false");
                byte[] boolResult = new byte[1 + jsonBoolBytes.Length];
                boolResult[0] = (byte)(b ? ValueKind.BooleanTrue : ValueKind.BooleanFalse);
                jsonBoolBytes.CopyTo(boolResult, 1);
                return boolResult;

            case byte[] jsonBytes:
                byte[] jsonResult = new byte[1 + jsonBytes.Length];
                jsonResult[0] = (byte)ValueKind.Json;
                jsonBytes.CopyTo(jsonResult, 1);
                return jsonResult;

            case RemovedValue:
                return new byte[] { (byte)ValueKind.Removed };

            case NullValue:
                // Store as JSON null representation
                byte[] jsonNullBytes = Encoding.UTF8.GetBytes("null");
                byte[] nullResult = new byte[1 + jsonNullBytes.Length];
                nullResult[0] = (byte)ValueKind.Null;
                jsonNullBytes.CopyTo(nullResult, 1);
                return nullResult;

            default:
                throw new NotSupportedException($"Unsupported value type: {value?.GetType()}");
        }
    }

    // Helper method to decode byte arrays back to objects (for backward compatibility)
    private static object DecodeValue(ReadOnlySpan<byte> encodedValue)
    {
        if (encodedValue.Length == 0)
            throw new ArgumentException("Empty encoded value");

        ValueKind kind = (ValueKind)encodedValue[0];
        ReadOnlySpan<byte> valueBytes = encodedValue.Slice(1);

        switch (kind)
        {
            case ValueKind.Utf8String:
                // Parse JSON string representation
                string jsonString = Encoding.UTF8.GetString(valueBytes);
                return JsonSerializer.Deserialize<string>(jsonString) ?? string.Empty;

            case ValueKind.Int32:
                // Parse JSON number representation
                string jsonInt = Encoding.UTF8.GetString(valueBytes);
                return int.Parse(jsonInt);

            case ValueKind.BooleanTrue:
            case ValueKind.BooleanFalse:
                // Parse JSON boolean representation
                string jsonBool = Encoding.UTF8.GetString(valueBytes);
                return bool.Parse(jsonBool);

            case ValueKind.Json:
                return valueBytes.ToArray();

            case ValueKind.Removed:
                return RemovedValue.Instance;

            case ValueKind.Null:
                return NullValue.Instance;

            default:
                throw new ArgumentException($"Unknown value kind: {kind}");
        }
    }

    private void Set(ReadOnlySpan<byte> name, object value)
    {
        if (_properties == null)
        {
            _properties = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Instance);
        }
        
        byte[] nameBytes = name.ToArray();
        byte[] encodedValue = EncodeValue(value);
        _properties[nameBytes] = encodedValue;
    }

    private object Get(ReadOnlySpan<byte> name)
    {
        if (_properties == null) ThrowPropertyNotFoundException(name);
        
        byte[] nameBytes = name.ToArray();
        if (!_properties.TryGetValue(nameBytes, out byte[]? encodedValue))
        {
            ThrowPropertyNotFoundException(name);
        }
        
        return DecodeValue(encodedValue!);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Write(Utf8JsonWriter writer)
    {
        if (_properties == null) return;
        
        foreach (var kvp in _properties)
        {
            string propertyName = Encoding.UTF8.GetString(kvp.Key);
            WriteEncodedValueAsJson(writer, propertyName, kvp.Value);
        }
    }

    // Helper method to write encoded byte values as JSON
    private static void WriteEncodedValueAsJson(Utf8JsonWriter writer, string propertyName, byte[] encodedValue)
    {
        if (encodedValue.Length == 0)
            throw new ArgumentException("Empty encoded value");

        ValueKind kind = (ValueKind)encodedValue[0];
        ReadOnlySpan<byte> valueBytes = encodedValue.AsSpan(1);
        ReadOnlySpan<byte> nameBytes = Encoding.UTF8.GetBytes(propertyName);

        switch (kind)
        {
            case ValueKind.Utf8String:
                // valueBytes contains JSON string representation, parse and write properly
                string jsonStringRepr = Encoding.UTF8.GetString(valueBytes);
                string actualString = JsonSerializer.Deserialize<string>(jsonStringRepr) ?? string.Empty;
                writer.WriteString(nameBytes, actualString);
                break;
            case ValueKind.Int32:
                // valueBytes contains JSON number representation
                string jsonIntRepr = Encoding.UTF8.GetString(valueBytes);
                int intValue = int.Parse(jsonIntRepr);
                writer.WriteNumber(nameBytes, intValue);
                break;
            case ValueKind.BooleanTrue:
            case ValueKind.BooleanFalse:
                // valueBytes contains JSON boolean representation
                string jsonBoolRepr = Encoding.UTF8.GetString(valueBytes);
                bool boolValue = bool.Parse(jsonBoolRepr);
                writer.WriteBoolean(nameBytes, boolValue);
                break;
            case ValueKind.Json:
                writer.WritePropertyName(nameBytes);
                writer.WriteRawValue(valueBytes);
                break;
            case ValueKind.Null:
                writer.WriteNull(nameBytes);
                break;
            case ValueKind.Removed:
                // Skip removed properties during serialization
                break;
            default:
                throw new NotSupportedException($"Unsupported value kind: {kind}");
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
            
            // Decode the encoded value for display
            object decodedValue = DecodeValue(kvp.Value);
            sb.Append(decodedValue.ToString());
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