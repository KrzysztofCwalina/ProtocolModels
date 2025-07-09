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
        => IndexOf(name) >= 0;

    // TODO: can set support json pointer?
    // System.String
    public void Set(ReadOnlySpan<byte> name, string value)
    {  
        PropertyValue entry = new(value);
        Set(name, entry);
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
            PropertyValue baseValue = Get(baseName);
            if (baseValue.Kind != ValueKind.Json) ThrowPropertyNotFoundException(jsonPointer);
            
            // Use JsonPointer to navigate to the specific element
            return JsonPointer.GetString(baseValue.JsonBytes.Span, pointer) ?? string.Empty;
        }
        
        // Direct property access
        PropertyValue value = Get(jsonPointer);
        if (value.Kind != ValueKind.Utf8String) ThrowPropertyNotFoundException(jsonPointer); 
        return value.StringValue;   
    }
    
    public ReadOnlyMemory<byte> GetStringUtf8(ReadOnlySpan<byte> jsonPointer)
    {
        PropertyValue value = Get(jsonPointer);
        if (value.Kind != ValueKind.Utf8String) ThrowPropertyNotFoundException(jsonPointer); 
        return value.Utf8Bytes;
    }

    // Int32
    public void Set(ReadOnlySpan<byte> name, int value)
    {
        PropertyValue entry = new(value);
        Set(name, entry);
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
            PropertyValue baseValue = Get(baseName);
            if (baseValue.Kind != ValueKind.Json) ThrowPropertyNotFoundException(jsonPointer);
            
            // Use JsonPointer to navigate to the specific element
            return JsonPointer.GetInt32(baseValue.JsonBytes.Span, pointer);
        }
        
        // Direct property access
        PropertyValue value = Get(jsonPointer);
        if (value.Kind != ValueKind.Int32) ThrowPropertyNotFoundException(jsonPointer);
        return value.Int32Value;
    }

    // TODO: can set support json pointer?
    // JSON Object
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
    {
        PropertyValue entry = new(json);
        Set(name, entry);
    }

    public BinaryData GetJson(ReadOnlySpan<byte> jsonPointer)
    {
        PropertyValue value = Get(jsonPointer);
        if (value.Kind != ValueKind.Json) ThrowPropertyNotFoundException(jsonPointer); 
        return BinaryData.FromBytes(value.JsonBytes);
    }

    public void Set(ReadOnlySpan<byte> name, bool value)
    {
        PropertyValue entry = new(value);
        Set(name, entry);
    }

    // Special (remove, set null, etc)
    public void Remove(ReadOnlySpan<byte> jsonPointer)
    {
        PropertyValue removedEntry = PropertyValue.CreateRemoved();
        Set(jsonPointer, removedEntry);
    }

    public void SetNull(ReadOnlySpan<byte> jsonPointer)
    {
        PropertyValue nullEntry = PropertyValue.CreateNull();
        Set(jsonPointer, nullEntry);
    }
}