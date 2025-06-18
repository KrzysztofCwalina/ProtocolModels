using NUnit.Framework;
using System;
using System.ClientModel.Primitives;

public class ModelAdditionalPropertiesTests
{
    [Test]
    public void SmokeTests()
    {
        InputModel input = new();
        input.Category = "number facts";
        input.Numbers = [42, 3.14];
        input.Names = ["my first building", "PI"];

        // Cute syntax adding JSON-only properties to the input model
        input += """
        {
            "foo": 0.95,
            "bar": { 
                "baz" : 1 
            }
        }
        """u8;

        input["category"] = "facts"; // changes CLR property
        input["numbers"] = new double[] { 3.14, 7 }; // changes CLR array property
        input["temperature"] = 90d; // adds JSON-only property
        input["complex"u8] = """{ "name": "foo", "value": 100 }"""u8; // adds JSON-only property

        Assert.That(input.Category, Is.EqualTo("facts"));
        Assert.That(input.Numbers, Is.EqualTo(new double[] { 3.14, 7d }));
        Assert.That(input.Names, Is.EqualTo(new string[] { "my first building", "PI" }));

        Assert.That(input.Json.GetDouble("bar/baz"u8), Is.EqualTo(1));
        Assert.That(input.Json.GetDouble("complex/value"u8), Is.EqualTo(100));
        Assert.That(input.Json.GetDouble("temperature"u8), Is.EqualTo(90d));
        Assert.That(input.Json.GetString("category"u8), Is.EqualTo("facts"));
        Assert.That(input.Json.GetArray<double>("numbers"u8), Is.EqualTo(new double[] { 3.14, 7d }));
        Assert.That(input.Json.GetArray<string>("names"u8), Is.EqualTo(new string[] { "my first building", "PI" }));
    }

    [Test]
    public void ArrayTests()
    {
        InputModel input = new();
        
        // First create arrays using the regular Set method
        input.Json.Set("numbers"u8, "[1.0, 2.0, 3.0]"u8);
        input.Json.Set("values"u8, "[10.5, 20.5, 30.5, 40.5]"u8);
        
        // Test updating existing arrays at existing indices
        input.Json.Set("numbers/1"u8, 99.9);
        input.Json.Set("values/2"u8, 300.7);

        // Verify the arrays were modified correctly
        double[] numbersArray = input.Numbers;
        Assert.That(numbersArray.Length, Is.EqualTo(3));
        Assert.That(numbersArray[0], Is.EqualTo(1.0));    // Unchanged
        Assert.That(numbersArray[1], Is.EqualTo(99.9));   // Modified
        Assert.That(numbersArray[2], Is.EqualTo(3.0));    // Unchanged
        
        double[] valuesArray = input.Json.GetArray<double>("values"u8);
        Assert.That(valuesArray.Length, Is.EqualTo(4));
        Assert.That(valuesArray[0], Is.EqualTo(10.5));    // Unchanged
        Assert.That(valuesArray[1], Is.EqualTo(20.5));    // Unchanged
        Assert.That(valuesArray[2], Is.EqualTo(300.7));   // Modified
        Assert.That(valuesArray[3], Is.EqualTo(40.5));    // Unchanged
        
        // Verify we can retrieve the modified values using array syntax
        Assert.That(input.Json.GetDouble("numbers/1"u8), Is.EqualTo(99.9));
        Assert.That(input.Json.GetDouble("values/2"u8), Is.EqualTo(300.7));
        
        // Test direct syntax
        input.Json.Set("test"u8, "[100.0, 200.0]"u8);
        input.Json.Set("test/1"u8, 999.0);
        Assert.That(input.Json.GetDouble("test/1"u8), Is.EqualTo(999.0));
    }
    
    [Test]
    public void NestedObjectTests()
    {
        InputModel input = new();
        input.Category = "nested objects";

        // Add a more complex nested structure with bar/baz pattern
        input += """
        {
            "bar": { 
                "baz" : 1,
                "name" : "test value"
            },
            "nested": { 
                "level1": {
                    "level2": {
                        "value": 42,
                        "name": "deep nested"
                    }
                },
                "simple": 100
            },
            "items": [
                { "id": 1, "name": "item1" },
                { "id": 2, "name": "item2" }
            ]
        }
        """u8;

        // Test that the original issue is fixed
        Assert.That(input.Json.GetDouble("bar/baz"u8), Is.EqualTo(1));
        
        // Test string access
        Assert.That(input.Json.GetString("bar/name"u8), Is.EqualTo("test value"));
        
        // Test deeply nested paths
        Assert.That(input.Json.GetDouble("nested/simple"u8), Is.EqualTo(100));
        Assert.That(input.Json.GetDouble("nested/level1/level2/value"u8), Is.EqualTo(42));
        Assert.That(input.Json.GetString("nested/level1/level2/name"u8), Is.EqualTo("deep nested"));
        
        // Test array access with paths
        Assert.That(input.Json.GetDouble("items/0/id"u8), Is.EqualTo(1));
        Assert.That(input.Json.GetString("items/1/name"u8), Is.EqualTo("item2"));
    }

    [Test]
    public void SerializationTests() {
        InputModel input = new();
        input.Names = ["my first building", "PI"];

        // Adds JSON-only properties to the input model
        input += """
        {
            "foo": 0.95,
            "bar": { 
                "baz" : 1 
            }
        }
        """u8;

        input["category"] = "facts";
        input["numbers"] = new double[] { 3.14, 7 }; // changes CLR property
        input["temperature"] = 90d; // adds JSON-only property
        input["complex"u8] = "{ \"name\": \"foo\", \"value\": 100 }"u8;

        // serialize
        BinaryData json = ModelReaderWriter.Write(input);

        Assert.That(json.GetString("/category"u8), Is.EqualTo(input.Category));
        Assert.That(json.GetDouble("/numbers/0"u8), Is.EqualTo(input.Numbers[0]));
        Assert.That(json.GetDouble("/numbers/1"u8), Is.EqualTo(input.Numbers[1]));
        Assert.That(json.GetString("/names/0"u8), Is.EqualTo(input.Names[0]));
        Assert.That(json.GetString("/names/1"u8), Is.EqualTo(input.Names[1]));
        Assert.That(json.GetDouble("/temperature"u8), Is.EqualTo(90d));
    }
}
