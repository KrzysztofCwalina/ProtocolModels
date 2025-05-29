using System.ClientModel.Primitives;

public class Tests
{
    [Test]
    public void ArrayItemSetter()
    {
        InputModel input = new();
        
        // Test setting array items using the slash notation
        input.Json.Set("foo/3"u8, 10.5);
        input.Json.Set("bar/0"u8, "test");
        input.Json.Set("baz/1"u8, 42);
        
        // Test that arrays were created with proper padding
        double[] fooArray = input.Json.GetArray<double>("foo"u8);
        Assert.That(fooArray.Length, Is.EqualTo(4));
        Assert.That(fooArray[3], Is.EqualTo(10.5));
        Assert.That(fooArray[0], Is.EqualTo(0.0)); // Should be padded with defaults
        
        string[] barArray = input.Json.GetArray<string>("bar"u8);
        Assert.That(barArray.Length, Is.EqualTo(1));
        Assert.That(barArray[0], Is.EqualTo("test"));
        
        // Verify the values can be retrieved using array access syntax
        Assert.That(input.Json.GetDouble("foo/3"u8), Is.EqualTo(10.5));
        Assert.That(input.Json.GetString("bar/0"u8), Is.EqualTo("test"));
        
        // Test updating existing arrays
        input.Json.Set("numbers"u8, "[1.0, 2.0, 3.0]"u8);
        input.Json.Set("numbers/1"u8, 99.9);
        
        double[] numbersArray = input.Json.GetArray<double>("numbers"u8);
        Assert.That(numbersArray.Length, Is.EqualTo(3));
        Assert.That(numbersArray[0], Is.EqualTo(1.0));
        Assert.That(numbersArray[1], Is.EqualTo(99.9)); // Modified
        Assert.That(numbersArray[2], Is.EqualTo(3.0));
        
        // Verify we can retrieve the modified value
        Assert.That(input.Json.GetDouble("numbers/1"u8), Is.EqualTo(99.9));
    }

    [Test] 
    public void Models()
    {
        InputModel input = new();
        input.Category = "number facts";
        input.Numbers = [42, 3.14];
        input.Names = ["my first building", "PI"];
        input += """
        {
            "foo": 0.95,
            "bar": { 
                "baz" : 1 
            }
        }
        """u8;

        input.Json.Set("temperature"u8, 90d);
        input.Json.Set("category"u8, "facts");
        input.Json.Set("numbers"u8, "[3.14, 7]"u8);
        input.Json.Set("complex"u8, "{ \"name\": \"foo\", \"value\": 100 }"u8);

        JsonView bar = input.Json["bar"];
        Assert.That(bar.GetDouble("baz"u8), Is.EqualTo(1));

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
}
