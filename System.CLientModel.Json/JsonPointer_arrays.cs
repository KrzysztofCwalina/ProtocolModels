// JSON Pointer: https://www.rfc-editor.org/rfc/rfc6901

using System.Text.Json;
using System.Text;
using System.Diagnostics;
using System.Buffers.Text;
using System.Buffers;
using System.Runtime.InteropServices;

public static partial class JsonPointer
{
    public static ReadOnlyMemory<byte>[]? GetUtf8Array(this BinaryData json, ReadOnlySpan<byte> pointer)
    {
        var memory = json.ToMemory();
        return GetUtf8ArrayCore(memory, pointer);
    }

    public static ReadOnlyMemory<byte>[]? GetUtf8Array(this ReadOnlySpan<byte> json, ReadOnlySpan<byte> pointer)
    {
        // Copy span to new array and use common implementation
        byte[] array = json.ToArray();
        var memory = new ReadOnlyMemory<byte>(array);
        return GetUtf8ArrayCore(memory, pointer);
    }

    public static ReadOnlyMemory<byte>[]? GetUtf8Array(this BinaryData json)
    {
        var memory = json.ToMemory();
        return GetUtf8ArrayCore(memory);
    }

    public static ReadOnlyMemory<byte>[]? GetUtf8Array(this ReadOnlySpan<byte> json)
    {
        // Copy span to new array and use common implementation
        byte[] array = json.ToArray();
        var memory = new ReadOnlyMemory<byte>(array);
        return GetUtf8ArrayCore(memory);
    }

    private static int CountArrayElements(ReadOnlyMemory<byte> memory, ReadOnlySpan<byte> pointer = default)
    {
        var reader = pointer.IsEmpty ? new Utf8JsonReader(memory.Span) : memory.Span.Find(pointer);
        
        if (pointer.IsEmpty)
        {
            bool success = reader.Read();
            Debug.Assert(success, "JSON must be valid UTF-8 and parseable as JSON");
        }
        
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new InvalidOperationException("JSON value is not an array");
        
        int count = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                count++;
            }
        }
        return count;
    }

    private static ReadOnlyMemory<byte>[]? GetUtf8ArrayCore(ReadOnlyMemory<byte> memory, ReadOnlySpan<byte> pointer = default)
    {
        var reader = pointer.IsEmpty ? new Utf8JsonReader(memory.Span) : memory.Span.Find(pointer);
        
        if (pointer.IsEmpty)
        {
            bool success = reader.Read();
            Debug.Assert(success, "JSON must be valid UTF-8 and parseable as JSON");
        }
        
        // Count array elements first
        int elementCount = CountArrayElements(memory, pointer);
        
        if (elementCount == 0)
            return Array.Empty<ReadOnlyMemory<byte>>();
        
        // Rent arrays from pool for offsets and lengths
        int[] offsets = ArrayPool<int>.Shared.Rent(elementCount);
        int[] lengths = ArrayPool<int>.Shared.Rent(elementCount);
        
        try
        {
            int index = 0;
            
            // Reset reader and collect segment information
            reader = pointer.IsEmpty ? new Utf8JsonReader(memory.Span) : memory.Span.Find(pointer);
            if (pointer.IsEmpty)
                reader.Read();
                
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    // Use TokenStartIndex + 1 to skip opening quote, ValueSpan.Length for unquoted content
                    offsets[index] = (int)reader.TokenStartIndex + 1;
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
                var segments = new ReadOnlyMemory<byte>[elementCount];
                var fallbackReader = pointer.IsEmpty ? new Utf8JsonReader(memory.Span) : memory.Span.Find(pointer);
                
                if (pointer.IsEmpty)
                    fallbackReader.Read();
                
                // Estimate total size using TokenStartIndex
                int estimatedTotalSize = 0;
                var sizingReader = pointer.IsEmpty ? new Utf8JsonReader(memory.Span) : memory.Span.Find(pointer);
                if (pointer.IsEmpty)
                    sizingReader.Read();
                    
                while (sizingReader.Read() && sizingReader.TokenType != JsonTokenType.EndArray)
                {
                    if (sizingReader.TokenType == JsonTokenType.String)
                    {
                        estimatedTotalSize += sizingReader.ValueSpan.Length;
                    }
                }
                
                // Create single buffer
                byte[] buffer = new byte[estimatedTotalSize];
                int currentOffset = 0;
                int segmentIndex = 0;
                
                while (fallbackReader.Read() && fallbackReader.TokenType != JsonTokenType.EndArray)
                {
                    if (fallbackReader.TokenType == JsonTokenType.String)
                    {
                        var valueSpan = fallbackReader.ValueSpan;
                        valueSpan.CopyTo(buffer.AsSpan(currentOffset));
                        segments[segmentIndex] = new ReadOnlyMemory<byte>(buffer, currentOffset, valueSpan.Length);
                        currentOffset += valueSpan.Length;
                        segmentIndex++;
                    }
                }
                
                return segments;
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(offsets);
            ArrayPool<int>.Shared.Return(lengths);
        }
    }

}