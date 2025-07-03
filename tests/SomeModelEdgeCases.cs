using NUnit.Framework;
using System;

namespace AdditionalProperties;

public class SomeModelEdgeCases
{
    [Ignore("we need to decide what we want")]
    [Test]
    public void ChangeClrPropertyType()
    {
        SomeModel model = new();
        model.Category = "number facts";
        model["category"] = 42;

        double value = model.Json.GetDouble("category"u8);

        Assert.That(value, Is.EqualTo(42));

        Assert.Throws<Exception>(() => {
            string category = model.Category; // this does not throw today. the reason is that it just reads the property
        });
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
