using NUnit.Framework;
using System.IO;
using System.Text.Json;

namespace AdditionalProperties;

public class SimpleModelTests
{
    [Test]
    public void Smoke()
    {
        SimpleModel model = new();
        model.Extensions.Set("category"u8, 42);
        int category = model.Extensions.GetInt32("category"u8);

        AssertSerializesTo(model, """{"category":42}""");
    }

    [Test]
    public void Serialization()
    {
        SimpleModel model = new();
        model.Category = "number facts";
        model.Extensions.Set("category"u8, 42);
        AssertSerializesTo(model, """{"category":42}""");
    }

    [Test]
    public void Arrays()
    {
        SimpleModel model = new();
        model.Extensions.Set("numbers"u8, "[1, 2, 3.0]"u8);
        AssertSerializesTo(model, """{"numbers":[1, 2, 3.0]}""");
        int number = model.Extensions.GetInt32("numbers/1"u8);
        Assert.That(number, Is.EqualTo(2));
    }

    [Test]
    public void Objects()
    {
        SimpleModel model = new();
        model.Extensions.Set("properties"u8,
        """
        {
            "foo": "bar",
            "nested": {
                "a": 1,
                "b": 2.0
            },
            "array": [1, 2, 3]
        }
        """u8);
        int nestedNumber = model.Extensions.GetInt32("nested/a"u8);
        Assert.That(nestedNumber, Is.EqualTo(1));

        int arrayNumber = model.Extensions.GetInt32("array/2"u8);
        Assert.That(arrayNumber, Is.EqualTo(2));
    }

    private static void AssertSerializesTo(SimpleModel model, string json)
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
