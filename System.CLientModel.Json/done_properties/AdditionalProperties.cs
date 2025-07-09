using System.Buffers.Binary;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

public partial struct AdditionalProperties
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
            
            // Get the JSON value for the base property
            object baseValue = Get(baseName);
            if (baseValue is not byte[]) ThrowPropertyNotFoundException(jsonPointer);
            byte[] jsonBytes = (byte[])baseValue;
            
            // Use JsonPointer to navigate to the specific element
            return JsonPointer.GetString(jsonBytes, pointer) ?? string.Empty;
        }
        
        // Direct property access
        object value = Get(jsonPointer);
        if (value is not string) ThrowPropertyNotFoundException(jsonPointer);
        string stringValue = (string)value; 
        return stringValue;   
    }
    
    public ReadOnlyMemory<byte> GetStringUtf8(ReadOnlySpan<byte> jsonPointer)
    {
        object value = Get(jsonPointer);
        if (value is not string) ThrowPropertyNotFoundException(jsonPointer);
        string stringValue = (string)value; 
        return System.Text.Encoding.UTF8.GetBytes(stringValue);
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
            
            // Get the JSON value for the base property
            object baseValue = Get(baseName);
            if (baseValue is not byte[]) ThrowPropertyNotFoundException(jsonPointer);
            byte[] jsonBytes = (byte[])baseValue;
            
            // Use JsonPointer to navigate to the specific element
            return JsonPointer.GetInt32(jsonBytes, pointer);
        }
        
        // Direct property access
        object value = Get(jsonPointer);
        if (value is not int) ThrowPropertyNotFoundException(jsonPointer);
        int intValue = (int)value;
        return intValue;
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
        object value = Get(jsonPointer);
        if (value is not byte[]) ThrowPropertyNotFoundException(jsonPointer);
        byte[] jsonBytes = (byte[])value; 
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