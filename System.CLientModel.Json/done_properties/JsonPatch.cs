using System.Buffers.Binary;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

public partial struct JsonPatch
{
    // System.String
    public void Set(ReadOnlySpan<byte> name, string value)
    {  
        JsonPatchEntry entry = new JsonPatchEntry(name, value);
        Set(entry);
    }

    public string GetString(ReadOnlySpan<byte> name)
    {
        // Check if this is a JSON pointer (contains '/')
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex >= 0)
        {
            // This is a JSON pointer - extract the base property name
            ReadOnlySpan<byte> baseName = name.Slice(0, slashIndex);
            ReadOnlySpan<byte> pointer = name.Slice(slashIndex);
            
            // Get the JSON value for the base property
            JsonPatchEntry baseValue = Get(baseName);
            if (baseValue.Kind != ValueKind.Json) ThrowPropertyNotFoundException(name);
            
            // Use JsonPointer to navigate to the specific element
            return System.Text.Json.JsonPointer.GetString(baseValue.Value.Span, pointer) ?? string.Empty;
        }
        
        // Direct property access
        JsonPatchEntry value = Get(name);
        if (value.Kind != ValueKind.Utf8String) ThrowPropertyNotFoundException(name); 
        return Encoding.UTF8.GetString(value.Value.Span);   
    }
    public ReadOnlyMemory<byte> GetStringUtf8(ReadOnlySpan<byte> name)
    {
        JsonPatchEntry value = Get(name);
        if (value.Kind != ValueKind.Utf8String) ThrowPropertyNotFoundException(name); 
        return value.Value;
    }

    // Int32
    public void Set(ReadOnlySpan<byte> name, int value)
    {
        JsonPatchEntry entry = new JsonPatchEntry(name, value);
        Set(entry);
    }

    public int GetInt32(ReadOnlySpan<byte> name)
    {
        // Check if this is a JSON pointer (contains '/')
        int slashIndex = name.IndexOf((byte)'/');
        if (slashIndex >= 0)
        {
            // This is a JSON pointer - extract the base property name
            ReadOnlySpan<byte> baseName = name.Slice(0, slashIndex);
            ReadOnlySpan<byte> pointer = name.Slice(slashIndex);
            
            // Get the JSON value for the base property
            JsonPatchEntry baseValue = Get(baseName);
            if (baseValue.Kind != ValueKind.Json) ThrowPropertyNotFoundException(name);
            
            // Use JsonPointer to navigate to the specific element
            return System.Text.Json.JsonPointer.GetInt32(baseValue.Value.Span, pointer);
        }
        
        // Direct property access
        JsonPatchEntry value = Get(name);
        if (value.Kind != ValueKind.Int32) ThrowPropertyNotFoundException(name);
        return BinaryPrimitives.ReadInt32LittleEndian(value.Value.Span);
    }

    // JSON Object
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
    {
        JsonPatchEntry entry = new JsonPatchEntry(name, json);
        Set(entry);
    }

    public BinaryData GetJson(ReadOnlySpan<byte> name)
    {
        JsonPatchEntry value = Get(name);
        if (value.Kind != ValueKind.Json) ThrowPropertyNotFoundException(name); 
        return BinaryData.FromBytes(value.Value);
    }
}

