using NUnit.Framework;
using System;
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
    }

    [Test]
    public void Serialization()
    {
        RawModel model = new();
        model.Category = "number facts";
        model.Extensions.Set("category"u8, JsonSerializer.Serialize(42));

        Assert.That(model.Extensions.Contains("category"u8));
        BinaryData json = model.Extensions.GetJson("category"u8);
    }

    [Test]
    public void ArraysCanBeSetThroughJson()
    {
        SomeModel model = new();

        model.Json.Set("numbers"u8, "[1.0, 2.0, 3.0]"u8);
        model.Json.Set("numbers/1"u8, 99.9);

        double[] numbersArray = model.Numbers;
        Assert.That(numbersArray.Length, Is.EqualTo(3));
        Assert.That(numbersArray[0], Is.EqualTo(1.0));    // Unchanged
        Assert.That(numbersArray[1], Is.EqualTo(99.9));   // Modified
        Assert.That(numbersArray[2], Is.EqualTo(3.0));    // Unchanged
    }
}
