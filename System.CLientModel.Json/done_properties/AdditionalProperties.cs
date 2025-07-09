using System.Buffers.Binary;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

public partial struct AdditionalProperties
{
    public bool Contains(ReadOnlySpan<byte> name)
    {
        if (_properties == null) return false;
        string nameStr = System.Text.Encoding.UTF8.GetString(name);
        return _properties.ContainsKey(nameStr);
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