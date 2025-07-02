
namespace System.Text.Json;

// TODO: maybe instead of this whole file we can use JsonSerializer or ModelReaderWriter (after additions)
internal static class SerializationHelpers
{
    public static ReadOnlyMemory<byte> ToJson(this object? objValue)
    {
        if (objValue == null) return "null"u8.ToArray();

        var stream = new MemoryStream(24);
        Utf8JsonWriter writer = new Utf8JsonWriter(stream);

        Type type = objValue.GetType();
        if (type == typeof(double))
        {
            writer.WriteNumberValue((double)objValue);
        }
        else if (type == typeof(int))
        {
            writer.WriteNumberValue((int)objValue);
        }
        else if (type == typeof(bool))
        {
            writer.WriteBooleanValue((bool)objValue);
        }
        else if (type == typeof(long))
        {
            writer.WriteNumberValue((long)objValue);
        }
        else if (type == typeof(short))
        {
            writer.WriteNumberValue((short)objValue);
        }
        else if (type == typeof(byte))
        {
            writer.WriteNumberValue((byte)objValue);
        }
        else if (type == typeof(string))
        {
            writer.WriteStringValue((string)objValue);
        }
        else if (type == typeof(float))
        {
            writer.WriteNumberValue((Single)objValue);
        }
        else if (type == typeof(double[]))
        {
            writer.WriteStartArray();
            foreach (var d in (double[])objValue)
                writer.WriteNumberValue(d);
            writer.WriteEndArray();
        }
        else if (type == typeof(string[]))
        {
            writer.WriteStartArray();
            foreach (var s in (string[])objValue)
                writer.WriteStringValue(s);
            writer.WriteEndArray();
        }
        else
        {
            throw new NotImplementedException($"Unsupported property type: {type}");
        }
        writer.Flush();
        ReadOnlyMemory<byte> memory = stream.GetBuffer().AsMemory(0, (int)stream.Position);
        return memory;
    }

    public static bool TryJsonToClrValue(ReadOnlySpan<byte> value, Type? ptype, out object? clrValue)
    {
        Utf8JsonReader reader = new Utf8JsonReader(value);
        if (!reader.Read())
        {
            clrValue = null;
            return false; // Invalid JSON
        }
        if (reader.TokenType == JsonTokenType.Null)
        {
            clrValue = null;
            return true; // Null value
        }

        // Try to convert JSON to the CLR property type
        object convertedValue;
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                if (ptype == typeof(string))
                {
                    clrValue = reader.GetString();
                    return true;
                }
                break;
            case JsonTokenType.Number:
                if (ptype == typeof(int))
                {
                    clrValue = reader.GetInt32();
                    return true;
                }
                else if (ptype == typeof(double))
                {
                    clrValue = reader.GetDouble();
                    return true;
                }
                break;
            case JsonTokenType.True:
            case JsonTokenType.False:
                if (ptype == typeof(bool))
                {
                    clrValue = reader.GetBoolean();
                    return true;
                }
                break;
            case JsonTokenType.StartArray:
                if (ptype.IsArray)
                {
                    if (TryJsonToClrArray(value, ptype.GetElementType()!, out object clrArray))
                    {
                        clrValue = clrArray;
                        return true;
                    }
                }
                break;
        }

        clrValue = null;
        return false;
    }

    private static bool TryJsonToClrArray(ReadOnlySpan<byte> json, Type itemType, out object? clrArray)
    {
        JsonDocument jsonDocument = JsonDocument.Parse(json.ToArray());
        JsonElement root = jsonDocument.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
        {
            clrArray = null;
            return false;
        }
        if (itemType == typeof(double))
        {
            var list = new List<double>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetDouble());
            clrArray = list.ToArray();
            return true;
        }
        else if (itemType == typeof(string))
        {
            var list = new List<string>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetString() ?? string.Empty);
            clrArray = list.ToArray();
            return true;
        }
        else if (itemType == typeof(int))
        {
            var list = new List<int>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetInt32());
            clrArray = list.ToArray();
            return true;
        }
        else if (itemType == typeof(float))
        {
            var list = new List<float>();
            foreach (var element in root.EnumerateArray())
                list.Add(element.GetSingle());
            clrArray = list.ToArray();
            return true;
        }
        throw new NotSupportedException($"Unsupported array type: {itemType}");
    }
}

