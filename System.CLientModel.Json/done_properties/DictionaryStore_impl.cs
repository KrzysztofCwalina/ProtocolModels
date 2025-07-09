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

    // Singleton arrays for common values
    private static readonly byte[] s_removedValueArray = new byte[] { (byte)ValueKind.Removed };
    private static readonly byte[] s_nullValueArray;
    private static readonly byte[] s_trueBooleanArray;
    private static readonly byte[] s_falseBooleanArray;

    // Static constructor to initialize singleton arrays
    static DictionaryStore()
    {
        // Initialize null value array
        using (var stream = new MemoryStream(6))
        {
            stream.WriteByte((byte)ValueKind.Null);
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteNullValue();
                writer.Flush();
            }
            s_nullValueArray = stream.ToArray();
        }

        // Initialize true boolean array
        using (var stream = new MemoryStream(10))
        {
            stream.WriteByte((byte)ValueKind.BooleanTrue);
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteBooleanValue(true);
                writer.Flush();
            }
            s_trueBooleanArray = stream.ToArray();
        }

        // Initialize false boolean array
        using (var stream = new MemoryStream(10))
        {
            stream.WriteByte((byte)ValueKind.BooleanFalse);
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteBooleanValue(false);
                writer.Flush();
            }
            s_falseBooleanArray = stream.ToArray();
        }
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
                // Estimate the buffer size for the string and use Utf8JsonWriter with a pre-allocated buffer
                int estimatedSize = Encoding.UTF8.GetByteCount(s) + 10; // Add extra space for JSON formatting and value kind
                using (var stream = new MemoryStream(estimatedSize)) 
                {
                    // Write the ValueKind byte and advance the position
                    stream.WriteByte((byte)ValueKind.Utf8String);
                    
                    // Now write the JSON starting at position 1
                    using (var writer = new Utf8JsonWriter(stream))
                    {
                        writer.WriteStringValue(s);
                        writer.Flush();
                    }
                    return stream.ToArray();
                }

            case int i:
                // Estimate the buffer size for the integer and use Utf8JsonWriter with a pre-allocated buffer
                const int intEstimatedSize = 20; // Enough for any 32-bit integer
                using (var stream = new MemoryStream(intEstimatedSize)) 
                {
                    // Write the ValueKind byte and advance the position
                    stream.WriteByte((byte)ValueKind.Int32);
                    
                    // Now write the JSON starting at position 1
                    using (var writer = new Utf8JsonWriter(stream))
                    {
                        writer.WriteNumberValue(i);
                        writer.Flush();
                    }
                    return stream.ToArray();
                }

            case bool b:
                // Use singleton arrays for boolean values
                return b ? s_trueBooleanArray : s_falseBooleanArray;

            case byte[] jsonBytes:
                byte[] jsonResult = new byte[1 + jsonBytes.Length];
                jsonResult[0] = (byte)ValueKind.Json;
                jsonBytes.CopyTo(jsonResult, 1);
                return jsonResult;

            case RemovedValue:
                // Use singleton array for removed value
                return s_removedValueArray;

            case NullValue:
                // Use singleton array for null value
                return s_nullValueArray;

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
                // Parse JSON string representation using Utf8JsonReader
                Utf8JsonReader reader = new Utf8JsonReader(valueBytes);
                if (reader.Read() && reader.TokenType == JsonTokenType.String)
                {
                    return reader.GetString() ?? string.Empty;
                }
                throw new FormatException("Invalid JSON string format.");

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

        writer.WritePropertyName(propertyName);

        switch (kind)
        {
            case ValueKind.Utf8String:
                // valueBytes contains JSON string representation, parse and write properly
                writer.WriteStringValue(valueBytes);
                break;

            case ValueKind.Int32:
                // valueBytes contains JSON number representation
                if (int.TryParse(Encoding.UTF8.GetString(valueBytes), out int intValue))
                {
                    writer.WriteNumberValue(intValue);
                }
                else
                {
                    throw new FormatException("Invalid integer format in encoded value.");
                }
                break;

            case ValueKind.BooleanTrue:
            case ValueKind.BooleanFalse:
                // valueBytes contains JSON boolean representation
                writer.WriteBooleanValue(kind == ValueKind.BooleanTrue);
                break;

            case ValueKind.Json:
                // Write raw JSON value
                writer.WriteRawValue(valueBytes, skipInputValidation: true);
                break;

            case ValueKind.Null:
                writer.WriteNullValue();
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