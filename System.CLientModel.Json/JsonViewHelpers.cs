using System.Buffers.Text;
using System.Text;
using System.Text.Json;

/// <summary>
/// Static helper methods for JsonView operations
/// </summary>
internal static class JsonViewHelpers
{
    /// <summary>
    /// Checks if the given span represents a valid array index
    /// </summary>
    public static bool IsValidArrayIndex(this ReadOnlySpan<byte> indexSpan, out int index)
    {
        return Utf8Parser.TryParse(indexSpan, out index, out _);
    }

    /// <summary>
    /// Gets a typed value from a JsonElement
    /// </summary>
    public static T GetTypedValue<T>(this JsonElement element)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)element.GetString()!;
        }
        else if (typeof(T) == typeof(double))
        {
            return (T)(object)element.GetDouble();
        }
        else if (typeof(T) == typeof(int))
        {
            return (T)(object)element.GetInt32();
        }
        else if (typeof(T) == typeof(bool))
        {
            return (T)(object)element.GetBoolean();
        }
        else
        {
            throw new NotSupportedException($"Type {typeof(T)} is not supported for path access");
        }
    }

    /// <summary>
    /// Navigates to a specific segment (property or array index) in a JsonElement
    /// </summary>
    public static JsonElement NavigateToSegment(this JsonElement current, string segment)
    {
        // Check if this segment is an array index
        if (int.TryParse(segment, out int index))
        {
            // This might be an array index, but we should check the current element type
            if (current.ValueKind == JsonValueKind.Array)
            {
                if (index >= current.GetArrayLength())
                    throw new IndexOutOfRangeException($"Array index {index} is out of range");
                return current.EnumerateArray().ElementAt(index);
            }
        }
        
        // Treat as object property
        if (!current.TryGetProperty(segment, out JsonElement next))
            throw new KeyNotFoundException($"Property '{segment}' not found in object");
        return next;
    }

    /// <summary>
    /// Asserts that a Utf8JsonReader read operation was successful
    /// </summary>
    public static void AssertRead(this Utf8JsonReader reader)
    {
        if (!reader.Read())
            throw new InvalidOperationException("Failed to parse JSON");
    }
    
    /// <summary>
    /// Asserts that the current token is StartObject
    /// </summary>
    public static void AssertStartObject(this Utf8JsonReader reader, ReadOnlySpan<byte> propertyName)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(propertyName)}' is not an object");
    }
    
    /// <summary>
    /// Throws an exception indicating that a property was not found in an object
    /// </summary>
    public static void AssertEndObject(this Utf8JsonReader reader, ReadOnlySpan<byte> propertyName, ReadOnlySpan<byte> subPropertyName)
    {
        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(subPropertyName)}' not found in object '{Encoding.UTF8.GetString(propertyName)}'");
    }
}