using System.Text;
using System.Text.Json;

internal static class ArrayModifiers
{
    public static void ModifyInt32Array(IJsonModel model, ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, int newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<int>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.Number ? reader.GetInt32() : 0);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteNumberValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        model.Set(arrayProperty, arrayJson);
    }
    
    public static void ModifyFloatArray(IJsonModel model, ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, float newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<float>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.Number ? reader.GetSingle() : 0f);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteNumberValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        model.Set(arrayProperty, arrayJson);
    }
    
    public static void ModifyStringArray(IJsonModel model, ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, string newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.String ? reader.GetString()! : string.Empty);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteStringValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        model.Set(arrayProperty, arrayJson);
    }
    
    public static void ModifyDoubleArray(IJsonModel model, ReadOnlySpan<byte> arrayProperty, ReadOnlySpan<byte> currentJson, int index, double newValue)
    {
        var reader = new Utf8JsonReader(currentJson);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new InvalidOperationException($"Property '{Encoding.UTF8.GetString(arrayProperty)}' is not an array");
        }
        
        var arrayElements = new List<double>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;
            arrayElements.Add(reader.TokenType == JsonTokenType.Number ? reader.GetDouble() : 0.0);
        }
        
        // Check if index exists in the array
        if (index >= arrayElements.Count)
        {
            throw new IndexOutOfRangeException($"Array index {index} is out of range for array with {arrayElements.Count} elements");
        }
        
        // Set the value at the specified index
        arrayElements[index] = newValue;
        
        // Rebuild the array JSON
        MemoryStream stream = new();
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);
        writer.WriteStartArray();
        
        foreach (var element in arrayElements)
        {
            writer.WriteNumberValue(element);
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        ReadOnlySpan<byte> arrayJson = stream.GetBuffer().AsSpan(0, (int)stream.Position);
        model.Set(arrayProperty, arrayJson);
    }
}