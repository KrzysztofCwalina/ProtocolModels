using NUnit.Framework;
using System;
using System.ClientModel.Primitives;

namespace AdditionalProperties;

public class SimpleModelTests
{
    [Test]
    public void Basics()
    {
        SimpleModel model = new();
        model.Category = "number facts";
        model.Extensions.Set("category"u8, 42);
        model.Extensions.Set("json_only"u8, "true");
        AssertSerializesTo(model, """{"id":0,"names":[],"numbers":[],"category":42,"json_only":"true"}""");
    }

    [Test]
    public void ChangeTypeOfClrProperty()
    {
        SimpleModel model = new();
        model.Extensions.Set("category"u8, 42); // public string? Category { get; set; }

        int category = model.Extensions.GetInt32("category"u8);

        AssertSerializesTo(model, """{"id":0,"names":[],"numbers":[],"category":42}""");
    }

    [Test]
    public void RemoveProperties()
    {
        SimpleModel model = new();
        model.Extensions.Set("json_only"u8, "true");
        model.Extensions.Remove("category"u8);
        model.Extensions.Remove("id"u8);
        model.Extensions.Remove("names"u8);
        model.Extensions.Remove("json_only"u8);

        AssertSerializesTo(model, """{"numbers":[]}""");
    }

    [Test]
    public void Arrays()
    {
        SimpleModel model = new();
        model.Extensions.Set("numbers"u8, "[1, 2, 3.0]"u8);

        AssertSerializesTo(model, """{"category":null,"id":0,"names":[],"numbers":[1, 2, 3.0]}""");

        // Json Pointer Syntax
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
            "array": [1, 2, 3],
            "names" : ["one", "two", "three"]
        }
        """u8);
        int nestedNumber = model.Extensions.GetInt32("properties/nested/a"u8);
        Assert.That(nestedNumber, Is.EqualTo(1));

        int arrayNumber = model.Extensions.GetInt32("properties/array/2"u8);
        Assert.That(arrayNumber, Is.EqualTo(3));

        string arrayString = model.Extensions.GetString("properties/names/2"u8);
        Assert.That(arrayString, Is.EqualTo("three"));
    }

    private static void AssertSerializesTo(SimpleModel model, string json)
    {
        BinaryData serialized = ModelReaderWriter.Write(model);
        Assert.That(serialized.ToString(), Is.EqualTo(json));
    }
}
