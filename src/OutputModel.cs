﻿using System.ClientModel.Primitives;
using System.Text;
using System.Text.Json;

public class OutputModel : JsonModel<OutputModel>
{
    public float Confidence { get; internal set; }
    protected override OutputModel CreateCore(ref Utf8JsonReader reader, ModelReaderWriterOptions options)
    {
        JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        foreach (var property in root.EnumerateObject())
        {
            if (property.NameEquals("confidence"))
            {
                Confidence = property.Value.GetSingle();
            }
            else 
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(property.Name);
                byte[] valueBytes = Encoding.UTF8.GetBytes(property.Value.GetRawText());
                Json.Set(nameBytes, valueBytes);
            }
        }
        return this;
    }

    protected override Type GetPropertyType(ReadOnlySpan<byte> name)
    {
        if (name.SequenceEqual("confidence"u8)) return typeof(float);
        return null;
    }


    protected override bool TryGetProperty(ReadOnlySpan<byte> name, out object value)
    {
        if(name.SequenceEqual("confidence"u8))
        {
            value = Confidence;
            return true;
        }
        value = default;
        return false;
    }

    protected override bool TrySetProperty(ReadOnlySpan<byte> name, object value)
    {
        throw new NotImplementedException();
    }

    protected override void WriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        throw new NotImplementedException();
    }

    public static implicit operator OutputModel(string json)
    {
        BinaryData data = BinaryData.FromString(json);
        return ModelReaderWriter.Read<OutputModel>(data)!;
    
    }
    public static implicit operator OutputModel(ReadOnlySpan<byte> json)
    {
        BinaryData data = BinaryData.FromBytes(json.ToArray());
        return ModelReaderWriter.Read<OutputModel>(data)!;
    }
}

