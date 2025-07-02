using NUnit.Framework;
using System;
using System.ClientModel.Primitives;

namespace AdditionalProperties;

// TODO: what do we do with enums and other types we cannot implement JsonModel on?
public class ModelAdditionalPropertiesTests
{
    [Test]
    public void SmokeTests()
    {
        SomeModel model = new();
        model.Category = "number facts";
        model.Numbers = [42, 3.14];
        model.Names = ["my first building", "PI"];

        model["category"] = "facts"; // changes CLR property
        model["numbers"] = new double[] { 3.14, 7 }; // changes CLR array property
        model["temperature"] = 90d; // adds JSON-only property
        model["complex"u8] = """{ "name": "foo", "value": 100 }"""u8; // adds JSON-only property

        Assert.That(model.Category, Is.EqualTo("facts"));
        Assert.That(model.Numbers, Is.EqualTo(new double[] { 3.14, 7d }));
        Assert.That(model.Names, Is.EqualTo(new string[] { "my first building", "PI" }));

        Assert.That(model.Json.GetDouble("complex/value"u8), Is.EqualTo(100));
        Assert.That(model.Json.GetDouble("temperature"u8), Is.EqualTo(90d));
        Assert.That(model.Json.GetString("category"u8), Is.EqualTo("facts"));
        Assert.That(model.Json.GetArray<double>("numbers"u8), Is.EqualTo(new double[] { 3.14, 7d }));
        Assert.That(model.Json.GetArray<string>("names"u8), Is.EqualTo(new string[] { "my first building", "PI" }));
    }

    [Test]
    public void ArrayTests()
    {
        SomeModel model = new();
        
        // First create arrays using the regular Set method
        model.Json.Set("numbers"u8, "[1.0, 2.0, 3.0]"u8);
        model.Json.Set("values"u8, "[10.5, 20.5, 30.5, 40.5]"u8);
        
        // Test updating existing arrays at existing indices
        model.Json.Set("numbers/1"u8, 99.9);
        model.Json.Set("values/2"u8, 300.7);

        // Verify the arrays were modified correctly
        double[] numbersArray = model.Numbers;
        Assert.That(numbersArray.Length, Is.EqualTo(3));
        Assert.That(numbersArray[0], Is.EqualTo(1.0));    // Unchanged
        Assert.That(numbersArray[1], Is.EqualTo(99.9));   // Modified
        Assert.That(numbersArray[2], Is.EqualTo(3.0));    // Unchanged
        
        double[] valuesArray = model.Json.GetArray<double>("values"u8);
        Assert.That(valuesArray.Length, Is.EqualTo(4));
        Assert.That(valuesArray[0], Is.EqualTo(10.5));    // Unchanged
        Assert.That(valuesArray[1], Is.EqualTo(20.5));    // Unchanged
        Assert.That(valuesArray[2], Is.EqualTo(300.7));   // Modified
        Assert.That(valuesArray[3], Is.EqualTo(40.5));    // Unchanged
        
        // Verify we can retrieve the modified values using array syntax
        Assert.That(model.Json.GetDouble("numbers/1"u8), Is.EqualTo(99.9));
        Assert.That(model.Json.GetDouble("values/2"u8), Is.EqualTo(300.7));
        
        // Test direct syntax
        model.Json.Set("test"u8, "[100.0, 200.0]"u8);
        model.Json.Set("test/1"u8, 999.0);
        Assert.That(model.Json.GetDouble("test/1"u8), Is.EqualTo(999.0));
    }
    
    [Test]
    public void SerializationTests() {
        SomeModel original = new();
        original.Names = ["my first building", "PI"];

        original["category"] = "facts";
        original["numbers"] = new double[] { 3.14, 7 }; // changes CLR property
        original["temperature"] = 90d; // adds JSON-only property
        original["complex"u8] = "{ \"name\": \"foo\", \"value\": 100 }"u8;

        // serialize
        BinaryData json = ModelReaderWriter.Write(original);

        Assert.That(json.GetString("/category"u8), Is.EqualTo(original.Category));
        Assert.That(json.GetDouble("/numbers/0"u8), Is.EqualTo(original.Numbers[0]));
        Assert.That(json.GetDouble("/numbers/1"u8), Is.EqualTo(original.Numbers[1]));
        Assert.That(json.GetString("/names/0"u8), Is.EqualTo(original.Names[0]));
        Assert.That(json.GetString("/names/1"u8), Is.EqualTo(original.Names[1]));
        Assert.That(json.GetDouble("/temperature"u8), Is.EqualTo(90d));

        SomeModel deserialized = ModelReaderWriter.Read<SomeModel>(json);

        Assert.That(deserialized.Category, Is.EqualTo(original.Category));
        Assert.That(deserialized.Numbers, Is.EqualTo(original.Numbers));
        Assert.That(deserialized.Names, Is.EqualTo(original.Names));
        Assert.That(deserialized.Json.GetDouble("complex/value"u8), Is.EqualTo(100));
        Assert.That(deserialized.Json.GetDouble("temperature"u8), Is.EqualTo(90d));
        Assert.That(deserialized.Json.GetString("category"u8), Is.EqualTo("facts"));
        Assert.That(deserialized.Json.GetArray<double>("numbers"u8), Is.EqualTo(new double[] { 3.14, 7d }));
        Assert.That(deserialized.Json.GetArray<string>("names"u8), Is.EqualTo(new string[] { "my first building", "PI" }));
        Assert.That(deserialized.Json.GetString("complex/name"u8), Is.EqualTo("foo"));
    }

    [Test]
    public void PropertyTypeMismatch()
    {
        SomeModel model = new();
        string guidString = Guid.NewGuid().ToString();

        // 1. Setting a string value to an int property via the Json API should succeed
        model.Json.Set("id"u8, guidString);
        
        // 2. The CLR property should remain unchanged (default value)
        Assert.That(model.Id, Is.EqualTo(0), "CLR property should maintain its default value");
        
        // 3. But we should be able to access the value via the JSON API
        Assert.That(model.Json.GetString("id"u8), Is.EqualTo(guidString), "JSON property should contain the string value");
        
        // 4. Test that we can serialize and deserialize the model with the type mismatch
        BinaryData serialized = ModelReaderWriter.Write(model);
        SomeModel deserialized = ModelReaderWriter.Read<SomeModel>(serialized);
        
        // Check that the deserialized model preserved the type mismatch properly
        Assert.That(deserialized.Id, Is.EqualTo(0), "Deserialized CLR property should maintain its default value");
        Assert.That(deserialized.Json.GetString("id"u8), Is.EqualTo(guidString), "Deserialized JSON property should contain the string value");
    }
}
