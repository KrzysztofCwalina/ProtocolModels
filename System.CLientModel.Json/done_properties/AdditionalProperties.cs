using System.Buffers.Binary;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

public partial struct DictionaryStore
{
    // Marker classes for special values
    internal sealed class RemovedValue
    {
        public static readonly RemovedValue Instance = new();
        private RemovedValue() { }
        public override string ToString() => "<removed>";
    }

    internal sealed class NullValue
    {
        public static readonly NullValue Instance = new();
        private NullValue() { }
        public override string ToString() => "null";
    }

    // Helper method to write objects as JSON
    private static void WriteObjectAsJson(Utf8JsonWriter writer, string propertyName, object value)
    {
        // Skip removed properties during serialization
        if (value is RemovedValue) return;
        
        ReadOnlySpan<byte> nameBytes = System.Text.Encoding.UTF8.GetBytes(propertyName);
        
        switch (value)
        {
            case string s:
                writer.WriteString(nameBytes, s);
                break;
            case int i:
                writer.WriteNumber(nameBytes, i);
                break;
            case bool b:
                writer.WriteBoolean(nameBytes, b);
                break;
            case byte[] jsonBytes:
                writer.WritePropertyName(nameBytes);
                writer.WriteRawValue(jsonBytes);
                break;
            case NullValue:
                writer.WriteNull(nameBytes);
                break;
            default:
                throw new NotSupportedException($"Unsupported value type: {value?.GetType()}");
        }
    }

    public bool Contains(ReadOnlySpan<byte> name)
    {
        if (_properties == null) return false;
        byte[] nameBytes = name.ToArray();
        return _properties.ContainsKey(nameBytes);
    }

    public bool Contains(byte[] name)
    {
        if (_properties == null) return false;
        return _properties.ContainsKey(name);
    }

    // TODO: can set support json pointer?
    // System.String
    public void Set(ReadOnlySpan<byte> name, string value)
    {  
        Set(name, (object)value);
    }

    // TODO: all these methods should take JSON pointer, not just property names
    public string GetString(ReadOnlySpan<byte> jsonPointer)
    {
        // Check if this is a JSON pointer (contains '/')
        int slashIndex = jsonPointer.IndexOf((byte)'/');
        if (slashIndex >= 0)
        {
            // This is a JSON pointer - extract the base property name
            ReadOnlySpan<byte> baseName = jsonPointer.Slice(0, slashIndex);
            ReadOnlySpan<byte> pointer = jsonPointer.Slice(slashIndex);
            
            // Get the encoded value for the base property
            byte[] baseEncodedValue = GetEncodedValue(baseName);
            if (baseEncodedValue.Length == 0 || (ValueKind)baseEncodedValue[0] != ValueKind.Json) 
                ThrowPropertyNotFoundException(jsonPointer);
            
            // Extract JSON bytes (skip the first byte which is the kind)
            byte[] jsonBytes = baseEncodedValue.AsSpan(1).ToArray();
            
            // Use JsonPointer to navigate to the specific element
            return JsonPointer.GetString(jsonBytes, pointer) ?? string.Empty;
        }
        
        // Direct property access
        byte[] encodedValue = GetEncodedValue(jsonPointer);
        if (encodedValue.Length == 0 || (ValueKind)encodedValue[0] != ValueKind.Utf8String) 
            ThrowPropertyNotFoundException(jsonPointer);
        
        // Extract string bytes (skip the first byte which is the kind)
        return Encoding.UTF8.GetString(encodedValue.AsSpan(1));
    }

    // Helper method to get raw encoded value bytes
    private byte[] GetEncodedValue(ReadOnlySpan<byte> name)
    {
        if (_properties == null) return Array.Empty<byte>();
        
        byte[] nameBytes = name.ToArray();
        if (!_properties.TryGetValue(nameBytes, out byte[]? encodedValue))
        {
            return Array.Empty<byte>();
        }
        
        return encodedValue!;
    }
    
    public ReadOnlyMemory<byte> GetStringUtf8(ReadOnlySpan<byte> jsonPointer)
    {
        byte[] encodedValue = GetEncodedValue(jsonPointer);
        if (encodedValue.Length == 0 || (ValueKind)encodedValue[0] != ValueKind.Utf8String) 
            ThrowPropertyNotFoundException(jsonPointer);
        
        // Return the UTF8 bytes (skip the first byte which is the kind)
        return encodedValue.AsMemory(1);
    }

    // Int32
    public void Set(ReadOnlySpan<byte> name, int value)
    {
        Set(name, (object)value);
    }

    public int GetInt32(ReadOnlySpan<byte> jsonPointer)
    {
        // Check if this is a JSON pointer (contains '/')
        int slashIndex = jsonPointer.IndexOf((byte)'/');
        if (slashIndex >= 0)
        {
            // This is a JSON pointer - extract the base property name
            ReadOnlySpan<byte> baseName = jsonPointer.Slice(0, slashIndex);
            ReadOnlySpan<byte> pointer = jsonPointer.Slice(slashIndex);
            
            // Get the encoded value for the base property
            byte[] baseEncodedValue = GetEncodedValue(baseName);
            if (baseEncodedValue.Length == 0 || (ValueKind)baseEncodedValue[0] != ValueKind.Json) 
                ThrowPropertyNotFoundException(jsonPointer);
            
            // Extract JSON bytes (skip the first byte which is the kind)
            byte[] jsonBytes = baseEncodedValue.AsSpan(1).ToArray();
            
            // Use JsonPointer to navigate to the specific element
            return JsonPointer.GetInt32(jsonBytes, pointer);
        }
        
        // Direct property access
        byte[] encodedValue = GetEncodedValue(jsonPointer);
        if (encodedValue.Length == 0 || (ValueKind)encodedValue[0] != ValueKind.Int32) 
            ThrowPropertyNotFoundException(jsonPointer);
        
        // Extract int32 bytes (skip the first byte which is the kind)
        ReadOnlySpan<byte> valueBytes = encodedValue.AsSpan(1);
        return BinaryPrimitives.ReadInt32LittleEndian(valueBytes);
    }

    // TODO: can set support json pointer?
    // JSON Object
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
    {
        byte[] jsonBytes = json.ToArray();
        Set(name, (object)jsonBytes);
    }

    public BinaryData GetJson(ReadOnlySpan<byte> jsonPointer)
    {
        byte[] encodedValue = GetEncodedValue(jsonPointer);
        if (encodedValue.Length == 0 || (ValueKind)encodedValue[0] != ValueKind.Json) 
            ThrowPropertyNotFoundException(jsonPointer);
        
        // Extract JSON bytes (skip the first byte which is the kind)
        byte[] jsonBytes = encodedValue.AsSpan(1).ToArray();
        return BinaryData.FromBytes(jsonBytes);
    }

    public void Set(ReadOnlySpan<byte> name, bool value)
    {
        Set(name, (object)value);
    }

    // Special (remove, set null, etc)
    public void Remove(ReadOnlySpan<byte> jsonPointer)
    {
        Set(jsonPointer, RemovedValue.Instance);
    }

    public void SetNull(ReadOnlySpan<byte> jsonPointer)
    {
        Set(jsonPointer, NullValue.Instance);
    }
}