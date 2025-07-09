using System.Buffers.Binary;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

public partial struct RecordStore
{
    public bool Contains(ReadOnlySpan<byte> name)
        => IndexOf(name) >= 0;

    // TODO: can set support json pointer?
    // System.String
    public void Set(ReadOnlySpan<byte> name, string value)
    {  
        PropertyRecord entry = new(name, value);
        Set(entry);
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
            PropertyRecord baseValue = Get(baseName);
            if (baseValue.Kind != ValueKind.Json) ThrowPropertyNotFoundException(jsonPointer);
            
            // Use JsonPointer to navigate to the specific element
            return JsonPointer.GetString(baseValue.Value.Span, pointer) ?? string.Empty;
        }
        
        // Direct property access
        PropertyRecord value = Get(jsonPointer);
        if (value.Kind != ValueKind.Utf8String) ThrowPropertyNotFoundException(jsonPointer); 
        return Encoding.UTF8.GetString(value.Value.Span);   
    }
    public ReadOnlyMemory<byte> GetStringUtf8(ReadOnlySpan<byte> jsonPointer)
    {
        PropertyRecord value = Get(jsonPointer);
        if (value.Kind != ValueKind.Utf8String) ThrowPropertyNotFoundException(jsonPointer); 
        return value.Value;
    }

    // Int32
    public void Set(ReadOnlySpan<byte> name, int value)
    {
        PropertyRecord entry = new(name, value);
        Set(entry);
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
            PropertyRecord baseValue = Get(baseName);
            if (baseValue.Kind != ValueKind.Json) ThrowPropertyNotFoundException(jsonPointer);
            
            // Use JsonPointer to navigate to the specific element
            return JsonPointer.GetInt32(baseValue.Value.Span, pointer);
        }
        
        // Direct property access
        PropertyRecord value = Get(jsonPointer);
        if (value.Kind != ValueKind.Int32) ThrowPropertyNotFoundException(jsonPointer);
        return BinaryPrimitives.ReadInt32LittleEndian(value.Value.Span);
    }

    // TODO: can set support json pointer?
    // JSON Object
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
    {
        PropertyRecord entry = new(name, json);
        Set(entry);
    }

    public BinaryData GetJson(ReadOnlySpan<byte> jsonPointer)
    {
        PropertyRecord value = Get(jsonPointer);
        if (value.Kind != ValueKind.Json) ThrowPropertyNotFoundException(jsonPointer); 
        return BinaryData.FromBytes(value.Value);
    }

    public void Set(ReadOnlySpan<byte> name, bool value)
    {
        PropertyRecord entry = new(name, value);
        Set(entry);
    }

    // Special (remove, set null, etc)
    public void Remove(ReadOnlySpan<byte> jsonPointer)
    {
        PropertyRecord removedEntry = PropertyRecord.CreateRemoved(jsonPointer);
        Set(removedEntry);
    }

    public void SetNull(ReadOnlySpan<byte> jsonPointer)
    {
        PropertyRecord nullEntry = PropertyRecord.CreateNull(jsonPointer);
        Set(nullEntry);
    }
}

