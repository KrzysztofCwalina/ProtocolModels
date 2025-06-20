// JSON Pointer: https://www.rfc-editor.org/rfc/rfc6901

using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Buffers.Text;
using System.Buffers;
using System.Runtime.InteropServices;

// TODO (pri 0): implement arrays, e.g. "/addresses/0/street"u8
// TODO (pri 1): implement object graphs, e.g "/address/street"u8
// TODO (pri 1): support null values for all types
// TODO (pri 1): make sure JSON escaping works
// TODO (pri 3): make sure JSON Pointer escaping works, e.g. "/a~/b"u8 finds property "a/b"
public static class JsonPointer
{
    public static ReadOnlySpan<byte> GetUtf8(this BinaryData json, ReadOnlySpan<byte> pointer)
    => json.Find(pointer).ValueSpan;
    public static ReadOnlySpan<byte> GetUtf8(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
        => json.Find(pointer).ValueSpan;
    public static ReadOnlySpan<byte> GetUtf8(this BinaryData json)
        => json.ToMemory().Span.GetUtf8();
    public static ReadOnlySpan<byte> GetUtf8(this ReadOnlySpan<byte> json)
    {
        Utf8JsonReader reader = new(json);
        bool success = reader.Read();
        return reader.ValueSpan;
    }

    public static string? GetString(this BinaryData json, ReadOnlySpan<byte> pointer)
        => json.Find(pointer).GetString();
    public static string? GetString(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
        => json.Find(pointer).GetString();
    public static string? GetString(this BinaryData json)
        => json.ToMemory().Span.GetString();
    public static string? GetString(this ReadOnlySpan<byte> json)
    {
        Utf8JsonReader reader = new(json);
        bool success = reader.Read();
        return reader.GetString();
    }

    public static int GetInt32(this BinaryData json, ReadOnlySpan<byte> pointer)
        => json.Find(pointer).GetInt32();
    public static int GetInt32(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
        => json.Find(pointer).GetInt32();
    public static int GetInt32(this BinaryData json)
        => json.ToMemory().Span.GetInt32();
    public static int GetInt32(this ReadOnlySpan<byte> json)
    {
        Utf8JsonReader reader = new(json);
        bool success = reader.Read();
        return reader.GetInt32();
    }

    public static double GetDouble(this BinaryData json, ReadOnlySpan<byte> pointer)
    => json.Find(pointer).GetDouble();
    public static double GetDouble(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
        => json.Find(pointer).GetDouble();
    public static double GetDouble(this BinaryData json)
        => json.ToMemory().Span.GetDouble();
    public static double GetDouble(this ReadOnlySpan<byte> json)
    {
        Utf8JsonReader reader = new(json);
        bool success = reader.Read();
        return reader.GetDouble();
    }

    public static bool GetBoolean(this BinaryData json, ReadOnlySpan<byte> pointer)
        => json.Find(pointer).GetBoolean();
    public static bool GetBoolean(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
        => json.Find(pointer).GetBoolean();
    public static bool GetBoolean(this BinaryData json)
        => json.ToMemory().Span.GetBoolean();
    public static bool GetBoolean(this ReadOnlySpan<byte> json)
    {
        Utf8JsonReader reader = new(json);
        bool success = reader.Read();
        return reader.GetBoolean();
    }

    public static string[]? GetStringArray(this BinaryData json, ReadOnlySpan<byte> pointer)
        => json.ToMemory().Span.GetStringArray(pointer);
    public static string[]? GetStringArray(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
    {
        var reader = json.Find(pointer);
        if (reader.TokenType != JsonTokenType.StartArray)
            return Array.Empty<string>();
        
        var strings = new List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                strings.Add(reader.GetString() ?? string.Empty);
            }
        }
        return strings.ToArray();
    }
    public static string[]? GetStringArray(this BinaryData json)
        => json.ToMemory().Span.GetStringArray();
    public static string[]? GetStringArray(this ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
        bool success = reader.Read();
        Debug.Assert(success, "JSON must be valid UTF-8 and parseable as JSON");
        
        if (reader.TokenType != JsonTokenType.StartArray)
            return Array.Empty<string>();
        
        var strings = new List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                strings.Add(reader.GetString() ?? string.Empty);
            }
        }
        return strings.ToArray();
    }

    public static double[]? GetDoubleArray(this BinaryData json, ReadOnlySpan<byte> pointer)
        => json.ToMemory().Span.GetDoubleArray(pointer);
    public static double[]? GetDoubleArray(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
    {
        var reader = json.Find(pointer);
        if (reader.TokenType != JsonTokenType.StartArray)
            return Array.Empty<double>();
        
        var doubles = new List<double>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                doubles.Add(reader.GetDouble());
            }
        }
        return doubles.ToArray();
    }
    public static double[]? GetDoubleArray(this BinaryData json)
        => json.ToMemory().Span.GetDoubleArray();
    public static double[]? GetDoubleArray(this ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
        bool success = reader.Read();
        Debug.Assert(success, "JSON must be valid UTF-8 and parseable as JSON");
        
        if (reader.TokenType != JsonTokenType.StartArray)
            return Array.Empty<double>();
        
        var doubles = new List<double>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                doubles.Add(reader.GetDouble());
            }
        }
        return doubles.ToArray();
    }

    public static int[]? GetInt32Array(this BinaryData json, ReadOnlySpan<byte> pointer)
        => json.ToMemory().Span.GetInt32Array(pointer);
    public static int[]? GetInt32Array(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
    {
        var reader = json.Find(pointer);
        if (reader.TokenType != JsonTokenType.StartArray)
            return Array.Empty<int>();

        var ints = new List<int>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                ints.Add(reader.GetInt32());
            }
        }
        return ints.ToArray();
    }
    public static int[]? GetInt32Array(this BinaryData json)
        => json.ToMemory().Span.GetInt32Array();
    public static int[]? GetInt32Array(this ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
        bool success = reader.Read();
        Debug.Assert(success, "JSON must be valid UTF-8 and parseable as JSON");
        
        if (reader.TokenType != JsonTokenType.StartArray)
            return Array.Empty<int>();

        var ints = new List<int>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                ints.Add(reader.GetInt32());
            }
        }
        return ints.ToArray();
    }

    public static ReadOnlyMemory<byte>[]? GetUtf8Array(this BinaryData json, ReadOnlySpan<byte> pointer)
    {
        var memory = json.ToMemory();
        var reader = memory.Span.Find(pointer);
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new InvalidOperationException("JSON value is not an array");
        
        // Count array elements first
        int elementCount = 0;
        var countingReader = memory.Span.Find(pointer);
        while (countingReader.Read() && countingReader.TokenType != JsonTokenType.EndArray)
        {
            if (countingReader.TokenType == JsonTokenType.String)
            {
                elementCount++;
            }
        }
        
        if (elementCount == 0)
            return Array.Empty<ReadOnlyMemory<byte>>();
        
        // Rent arrays from pool for offsets and lengths
        int[] offsets = ArrayPool<int>.Shared.Rent(elementCount);
        int[] lengths = ArrayPool<int>.Shared.Rent(elementCount);
        
        try
        {
            int index = 0;
            
            // Reset reader and collect segment information
            reader = memory.Span.Find(pointer);
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    // Use TokenStartIndex to get offset
                    offsets[index] = (int)reader.TokenStartIndex;
                    lengths[index] = reader.ValueSpan.Length;
                    index++;
                }
            }
            
            // Try to create slices from the original memory if possible
            if (MemoryMarshal.TryGetArray(memory, out var arraySegment) && arraySegment.Array != null)
            {
                var result = new ReadOnlyMemory<byte>[elementCount];
                for (int i = 0; i < elementCount; i++)
                {
                    // Adjust offset to account for array segment offset
                    int actualOffset = offsets[i] + arraySegment.Offset;
                    result[i] = new ReadOnlyMemory<byte>(arraySegment.Array, actualOffset, lengths[i]);
                }
                return result;
            }
            else
            {
                // Fallback: create a new array and copy segments
                var segmentData = new List<byte[]>();
                var reader2 = memory.Span.Find(pointer);
                while (reader2.Read() && reader2.TokenType != JsonTokenType.EndArray)
                {
                    if (reader2.TokenType == JsonTokenType.String)
                    {
                        segmentData.Add(reader2.ValueSpan.ToArray());
                    }
                }
                return CreateMemoryArrayFromByteArrays(segmentData);
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(offsets);
            ArrayPool<int>.Shared.Return(lengths);
        }
    }
    public static ReadOnlyMemory<byte>[]? GetUtf8Array(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
    {
        var reader = json.Find(pointer);
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new InvalidOperationException("JSON value is not an array");
        
        // For span-based input, we need to create a single buffer and copy all segments
        var segmentData = new List<byte[]>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                segmentData.Add(reader.ValueSpan.ToArray());
            }
        }
        
        return CreateMemoryArrayFromByteArrays(segmentData);
    }
    public static ReadOnlyMemory<byte>[]? GetUtf8Array(this BinaryData json)
    {
        var memory = json.ToMemory();
        var reader = new Utf8JsonReader(memory.Span);
        bool success = reader.Read();
        Debug.Assert(success, "JSON must be valid UTF-8 and parseable as JSON");
        
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new InvalidOperationException("JSON value is not an array");
        
        // Count array elements first
        int elementCount = 0;
        var countingReader = new Utf8JsonReader(memory.Span);
        countingReader.Read();
        while (countingReader.Read() && countingReader.TokenType != JsonTokenType.EndArray)
        {
            if (countingReader.TokenType == JsonTokenType.String)
            {
                elementCount++;
            }
        }
        
        if (elementCount == 0)
            return Array.Empty<ReadOnlyMemory<byte>>();
        
        // Rent arrays from pool for offsets and lengths
        int[] offsets = ArrayPool<int>.Shared.Rent(elementCount);
        int[] lengths = ArrayPool<int>.Shared.Rent(elementCount);
        
        try
        {
            int index = 0;
            
            // Reset reader and collect segment information
            reader = new Utf8JsonReader(memory.Span);
            reader.Read();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    // Use TokenStartIndex to get offset
                    offsets[index] = (int)reader.TokenStartIndex;
                    lengths[index] = reader.ValueSpan.Length;
                    index++;
                }
            }
            
            // Try to create slices from the original memory if possible
            if (MemoryMarshal.TryGetArray(memory, out var arraySegment) && arraySegment.Array != null)
            {
                var result = new ReadOnlyMemory<byte>[elementCount];
                for (int i = 0; i < elementCount; i++)
                {
                    // Adjust offset to account for array segment offset
                    int actualOffset = offsets[i] + arraySegment.Offset;
                    result[i] = new ReadOnlyMemory<byte>(arraySegment.Array, actualOffset, lengths[i]);
                }
                return result;
            }
            else
            {
                // Fallback: create a new array and copy segments
                var segmentData = new List<byte[]>();
                var reader2 = new Utf8JsonReader(memory.Span);
                reader2.Read();
                while (reader2.Read() && reader2.TokenType != JsonTokenType.EndArray)
                {
                    if (reader2.TokenType == JsonTokenType.String)
                    {
                        segmentData.Add(reader2.ValueSpan.ToArray());
                    }
                }
                return CreateMemoryArrayFromByteArrays(segmentData);
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(offsets);
            ArrayPool<int>.Shared.Return(lengths);
        }
    }
    public static ReadOnlyMemory<byte>[]? GetUtf8Array(this ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
        bool success = reader.Read();
        Debug.Assert(success, "JSON must be valid UTF-8 and parseable as JSON");
        
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new InvalidOperationException("JSON value is not an array");
        
        // For span-based input, we need to create a single buffer and copy all segments
        var segmentData = new List<byte[]>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                segmentData.Add(reader.ValueSpan.ToArray());
            }
        }
        
        return CreateMemoryArrayFromByteArrays(segmentData);
    }

    private static ReadOnlyMemory<byte>[] CreateMemoryArrayFromByteArrays(List<byte[]> segments)
    {
        if (segments.Count == 0)
            return Array.Empty<ReadOnlyMemory<byte>>();
        
        // Calculate total size needed
        int totalSize = 0;
        foreach (var segment in segments)
        {
            totalSize += segment.Length;
        }
        
        // Allocate single array
        byte[] buffer = new byte[totalSize];
        var result = new ReadOnlyMemory<byte>[segments.Count];
        
        int currentOffset = 0;
        for (int i = 0; i < segments.Count; i++)
        {
            // Copy segment to buffer
            segments[i].CopyTo(buffer, currentOffset);
            
            // Create ReadOnlyMemory slice from the same buffer
            result[i] = new ReadOnlyMemory<byte>(buffer, currentOffset, segments[i].Length);
            
            currentOffset += segments[i].Length;
        }
        
        return result;
    }

    internal static Utf8JsonReader Find(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
    {
        if (json.Length == 0) throw new ArgumentException("JSON document cannot be empty", nameof(json));

        var reader = new Utf8JsonReader(json);
        bool success = reader.Read();
        Debug.Assert(success);

        if (pointer.Length == 0)
        { // return the whole document
            return reader;
        }

        return Find(reader, pointer);
    }

    internal static Utf8JsonReader Find(this BinaryData json, ReadOnlySpan<byte> pointer)
    => json.ToMemory().Span.Find(pointer);

    private static Utf8JsonReader Find(Utf8JsonReader reader, ReadOnlySpan<byte> pointer)
    {
        string propertyName = Encoding.UTF8.GetString(pointer);
        if (pointer.Length == 0) return reader;
        if (pointer[0] != (byte)'/') throw new ArgumentException("JSON Pointer must start with '/'", nameof(pointer));
        if (pointer.IndexOf((byte)'~') != -1) throw new NotImplementedException("JSON Pointer escaping not implemented yet");

        pointer = pointer.Slice(1); // slice off the leading '/'
        int slashIndex = pointer.IndexOf((byte)'/');
        ReadOnlySpan<byte> nextPointerSegment = slashIndex == -1 ? pointer : pointer.Slice(0, slashIndex);  
        string nextSegment = Encoding.UTF8.GetString(nextPointerSegment);
        ReadOnlySpan<byte> remainingPointer = slashIndex == -1 ? ReadOnlySpan<byte>.Empty : pointer.Slice(slashIndex);
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

    private static Utf8JsonReader FindArrayItem(Utf8JsonReader reader, ReadOnlySpan<byte> pointer)
    {
        string indexString = Encoding.UTF8.GetString(pointer);
        if (!Utf8Parser.TryParse(pointer, out int index, out _))
        {
            throw new ArgumentException($"Invalid JSON Pointer index: {Encoding.UTF8.GetString(pointer)}");
        }

        int current = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                throw new IndexOutOfRangeException();
            }
            
            // We found an array element (could be an object, array, or primitive value)
            if (current == index)
            {
                return reader;
            }
            current++;
            
            // Skip the entire value to move to the next array element
            // This handles objects, arrays, and primitive values correctly
            reader.Skip();
        }
        throw new KeyNotFoundException($"{Encoding.UTF8.GetString(pointer)} not found in JSON document");
    }
}
