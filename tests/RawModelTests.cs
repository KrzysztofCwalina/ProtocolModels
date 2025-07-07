using NUnit.Framework;
using System.IO;
using System.Text.Json;

namespace AdditionalProperties;

public class RawModelTests
{
    [Test]
    public void Smoke()
    {
        RawModel model = new();
        model.Extensions.Set("category"u8, 42);
        int category = model.Extensions.GetInt32("category"u8);

        AssertSerializesTo(model, """{"category":42}""");
    }

    [Test]
    public void Serialization()
    {
        RawModel model = new();
        model.Category = "number facts";
        model.Extensions.Set("category"u8, 42);
        AssertSerializesTo(model, """{"category":42}""");
    }

    [Test]
    public void ArraysCanBeSetThroughJson()
    {
        RawModel model = new();
        model.Extensions.Set("numbers"u8, "[1.0, 2.0, 3.0]"u8);
        AssertSerializesTo(model, """{"numbers":[1.0, 2.0, 3.0]}""");
    }

    private static void AssertSerializesTo(RawModel model, string json)
    {
        MemoryStream stream = new();
        Utf8JsonWriter writer = new(stream);
        writer.WriteStartObject();
        model.Extensions.Write(writer);
        writer.WriteEndObject();
        writer.Flush();
        string written = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        Assert.That(written, Is.EqualTo(json));
    }
}
