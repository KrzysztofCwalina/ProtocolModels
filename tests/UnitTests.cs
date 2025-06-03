using System.ClientModel.Primitives;

public class Tests
{
    [Test]
    public void SmokeTests()
    {
        InputModel input = new();
        input.Category = "number facts";
        input.Numbers = [42, 3.14];
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

        input.Json.Set("temperature"u8, 90d); // adds JSON-only property
        input.Json.Set("category"u8, "facts"); // changes CLR property
        input.Json.Set("numbers"u8, "[3.14, 7]"u8); // changes CLR property
        input.Json.Set("complex"u8, "{ \"name\": \"foo\", \"value\": 100 }"u8); // adds complex JSON-only property

        JsonView bar = input.Json["bar"]; // Accessing a JSON-only property returning complex object.
        Assert.That(bar.GetDouble("baz"u8), Is.EqualTo(1));
        
        // Add the failing test case mentioned in the issue
        Assert.That(input.Json.GetDouble("bar/baz"u8), Is.EqualTo(1));

        JsonView complex = input.Json["complex"];
        Assert.That(complex.GetDouble("value"u8), Is.EqualTo(100));

        Assert.That(input.Category, Is.EqualTo("facts"));
        Assert.That(input.Numbers, Is.EqualTo([3.14, 7d]));
        Assert.That(input.Names, Is.EqualTo(["my first building", "PI"]));
        Assert.That(input.Json.GetDouble("temperature"u8), Is.EqualTo(90d));
        Assert.That(input.Json.GetString("category"u8), Is.EqualTo("facts"));

        // Test GetArray<T> for numbers and names
        double[] numbers = input.Json.GetArray<double>("numbers"u8);
        Assert.That(numbers, Is.EqualTo(new double[] { 3.14, 7d }));
        string[] names = input.Json.GetArray<string>("names"u8);
        Assert.That(names, Is.EqualTo(new string[] { "my first building", "PI" }));

        // serialize
        BinaryData json = ModelReaderWriter.Write(input);

        Assert.That(json.GetString("/category"u8), Is.EqualTo(input.Category));
        Assert.That(json.GetDouble("/numbers/0"u8), Is.EqualTo(input.Numbers[0]));
        Assert.That(json.GetDouble("/numbers/1"u8), Is.EqualTo(input.Numbers[1]));
        Assert.That(json.GetString("/names/0"u8), Is.EqualTo(input.Names[0]));
        Assert.That(json.GetString("/names/1"u8), Is.EqualTo(input.Names[1]));
        Assert.That(json.GetDouble("/temperature"u8), Is.EqualTo(90d));


        OutputModel output = """
        {
            "confidence": 0.95,
            "text": "some text"
        }
        """u8;

        Assert.That(output.Confidence, Is.EqualTo(0.95f));
        Assert.That(output.Json.GetDouble("confidence"u8), Is.EqualTo(0.95));
        Assert.That(output.Json.GetString("text"u8), Is.EqualTo("some text"));
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
    public void InputModelOriginalTest()
    {
        InputModel input = new();
        input.Category = "number facts";
        input.Numbers = [42, 3.14];
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

        JsonView bar = input.Json["bar"]; // Accessing a JSON-only property returning complex object.
        Assert.That(bar.GetDouble("baz"u8), Is.EqualTo(1));
        
        // Add the failing test case mentioned in the issue
        Assert.That(input.Json.GetDouble("bar/baz"u8), Is.EqualTo(1));
    }
    
    [Test]
    public void InputModelJsonAdditionOperatorTest()
    {
        var model = new InputModelJson();
        model.Category = "test";
        
        model += """{"foo": 42}"""u8;
        
        // Debug: Print the JSON to see what it contains
        BinaryData json = ModelReaderWriter.Write(model);
        Console.WriteLine($"JSON after addition: {json}");
        
        // Try to access foo property
        try
        {
            double fooValue = model.Json.GetDouble("foo"u8);
            Assert.That(fooValue, Is.EqualTo(42));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing foo: {ex}");
            throw;
        }
    }
    
    // TODO: This test requires infrastructure changes to support additional properties in JSON-only storage
    /*
    [Test]
    public void InputModelJsonComprehensiveTest()
    {
        var model = new InputModelJson();
        model.Category = "number facts";
        model.Numbers = new[] { 42.0, 3.14 };
        model.Names = new[] { "my first building", "PI" };

        // Test adding JSON-only properties through addition operator
        model += """
        {
            "foo": 0.95,
            "bar": { 
                "baz" : 1 
            }
        }
        """u8;

        // Debug: Print the JSON to see what it contains
        BinaryData json = ModelReaderWriter.Write(model);
        Console.WriteLine($"JSON after addition: {json}");

        // Test JsonView access for complex objects
        try
        {
            JsonView bar = model.Json["bar"];
            Assert.That(bar.GetDouble("baz"u8), Is.EqualTo(1));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing bar: {ex}");
            throw;
        }
        
        // Test nested path access
        Assert.That(model.Json.GetDouble("bar/baz"u8), Is.EqualTo(1));

        // Verify basic properties
        Assert.That(model.Category, Is.EqualTo("number facts"));
        Assert.That(model.Numbers, Is.EqualTo(new[] { 42.0, 3.14 }));
        Assert.That(model.Names, Is.EqualTo(new[] { "my first building", "PI" }));

        // Test serialization
        Assert.That(json.GetString("/category"u8), Is.EqualTo(model.Category));
        Assert.That(json.GetDouble("/numbers/0"u8), Is.EqualTo(model.Numbers[0]));
        Assert.That(json.GetDouble("/numbers/1"u8), Is.EqualTo(model.Numbers[1]));
        Assert.That(json.GetString("/names/0"u8), Is.EqualTo(model.Names[0]));
        Assert.That(json.GetString("/names/1"u8), Is.EqualTo(model.Names[1]));
        
        // Test deserialization
        var deserializedModel = ModelReaderWriter.Read<InputModelJson>(json);
        Assert.That(deserializedModel.Category, Is.EqualTo("number facts"));
        Assert.That(deserializedModel.Numbers, Is.EqualTo(new[] { 42.0, 3.14 }));
        Assert.That(deserializedModel.Names, Is.EqualTo(new[] { "my first building", "PI" }));
    }
    */
    
    [Test]
    public void InputModelJsonBasicTest()
    {
        var model = new InputModelJson();
        
        // Test basic property setting and getting
        model.Category = "test category";
        model.Names = new[] { "name1", "name2" };
        model.Numbers = new[] { 1.0, 2.0, 3.0 };
        
        Assert.That(model.Category, Is.EqualTo("test category"));
        Assert.That(model.Names, Is.EqualTo(new[] { "name1", "name2" }));
        Assert.That(model.Numbers, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
        
        // Test serialization
        BinaryData json = ModelReaderWriter.Write(model);
        string jsonString = json.ToString();
        
        // Verify the JSON contains our data
        Assert.That(jsonString, Does.Contain("test category"));
        Assert.That(jsonString, Does.Contain("name1"));
        Assert.That(jsonString, Does.Contain("name2"));
        
        // Test deserialization
        var deserializedModel = ModelReaderWriter.Read<InputModelJson>(json);
        Assert.That(deserializedModel.Category, Is.EqualTo("test category"));
        Assert.That(deserializedModel.Names, Is.EqualTo(new[] { "name1", "name2" }));
        Assert.That(deserializedModel.Numbers, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
    }
    
    [Test]
    public void NestedObjectAccessTests()
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
}
