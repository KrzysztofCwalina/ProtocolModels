// JSON Pointer: https://www.rfc-editor.org/rfc/rfc6901

using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Buffers.Text;

public static class JsonPointer
{
    public static string AsString(this ReadOnlySpan<byte> json)
    {
        Utf8JsonReader reader = new(json);
        bool success = reader.Read();
        Debug.Assert(success, "JSON must be valid UTF-8 and parseable as JSON");
        return reader.GetString()!;
    }
    public static double AsDouble(this ReadOnlySpan<byte> json)
    {
        Utf8JsonReader reader = new(json);
        bool success = reader.Read();
        Debug.Assert(success, "JSON must be valid UTF-8 and parseable as JSON");
        return reader.GetDouble();
    }
    public static string? GetString(this BinaryData json, ReadOnlySpan<byte> jsonPointer)
        => json.Find(jsonPointer).GetString();
    public static ReadOnlySpan<byte> ReadUtf8(this BinaryData json, ReadOnlySpan<byte> jsonPointer)
        => json.Find(jsonPointer).ValueSpan;
    public static int GetInt32(this BinaryData json, ReadOnlySpan<byte> jsonPointer)
        => json.Find(jsonPointer).GetInt32();
    public static bool ReadBoolean(this BinaryData json, ReadOnlySpan<byte> jsonPointer)
        => json.Find(jsonPointer).GetBoolean();

    public static double GetDouble(this BinaryData json, ReadOnlySpan<byte> jsonPointer)
    => json.Find(jsonPointer).GetDouble();

    public static double GetDouble(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> jsonPointer)
        => json.Find(jsonPointer).GetDouble();

    // TODO (pri 0): implement arrays, e.g. "/addresses/0/street"u8
    // TODO (pri 1): implement object graphs, e.g "/address/street"u8
    // TODO (pri 1): support null values for all types
    // TODO (pri 1): make sure JSON escaping works
    // TODO (pri 3): make sure JSON Pointer escaping works, e.g. "/a~/b"u8 finds property "a/b"
    public static Utf8JsonReader Find(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> jsonPointer)
    {
        if (json.Length == 0) throw new ArgumentException("JSON document cannot be empty", nameof(json));

        var reader = new Utf8JsonReader(json);
        bool success = reader.Read();
        Debug.Assert(success);

        if (jsonPointer.Length == 0)
        { // return the whole document
            return reader;
        }

        return Find(reader, jsonPointer);
    }

    private static Utf8JsonReader Find(Utf8JsonReader reader, ReadOnlySpan<byte> jsonPointer)
    {
        string propertyName = Encoding.UTF8.GetString(jsonPointer);
        if (jsonPointer.Length == 0) return reader;
        if (jsonPointer[0] != (byte)'/') throw new ArgumentException("JSON Pointer must start with '/'", nameof(jsonPointer));
        if (jsonPointer.IndexOf((byte)'~') != -1) throw new NotImplementedException("JSON Pointer escaping not implemented yet");

        jsonPointer = jsonPointer.Slice(1); // slice off the leading '/'
        int slashIndex = jsonPointer.IndexOf((byte)'/');
        ReadOnlySpan<byte> nextPointerSegment = slashIndex == -1 ? jsonPointer : jsonPointer.Slice(0, slashIndex);  
        string nextSegment = Encoding.UTF8.GetString(nextPointerSegment);
        ReadOnlySpan<byte> remainingPointer = slashIndex == -1 ? ReadOnlySpan<byte>.Empty : jsonPointer.Slice(slashIndex);
        string remainingPointerString = Encoding.UTF8.GetString(remainingPointer);

        JsonTokenType jsonType = reader.TokenType;
        if (jsonType == JsonTokenType.StartObject)
        {
            reader = FindPropertyValue(reader, nextPointerSegment);
        }
        else if (jsonType == JsonTokenType.StartArray)
        {
            reader = FindArrayItem(reader, nextPointerSegment);
        }
        return Find(reader, remainingPointer);
    }

    private static Utf8JsonReader FindPropertyValue(Utf8JsonReader reader, ReadOnlySpan<byte> propertyName)
    {
        string pn = Encoding.UTF8.GetString(propertyName);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals(propertyName))
            {
                bool success = reader.Read();
                Debug.Assert(success);
                return reader;
            }
        }

        throw new KeyNotFoundException($"{Encoding.UTF8.GetString(propertyName)} not found in JSON document");
    }

    private static Utf8JsonReader FindArrayItem(Utf8JsonReader reader, ReadOnlySpan<byte> jsonPointer)
    {
        string indexString = Encoding.UTF8.GetString(jsonPointer);
        if (!Utf8Parser.TryParse(jsonPointer, out int index, out _))
        {
            throw new ArgumentException($"Invalid JSON Pointer index: {Encoding.UTF8.GetString(jsonPointer)}");
        }

        int current = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                throw new IndexOutOfRangeException();
            }
            if (current == index)
            {
                return reader;
            }
            current++;
        }
        throw new KeyNotFoundException($"{Encoding.UTF8.GetString(jsonPointer)} not found in JSON document");
    }

    public static Utf8JsonReader Find(this BinaryData json, ReadOnlySpan<byte> jsonPointer)
        => json.ToMemory().Span.Find(jsonPointer);
}
