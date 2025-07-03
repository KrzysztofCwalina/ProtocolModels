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

