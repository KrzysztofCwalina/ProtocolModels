using System.ClientModel.Primitives;

public class Tests
{
    [Test]
    public void ArrayItemSetter()
    {
        InputModel input = new();
        
        // First create arrays using the regular Set method
        input.Json.Set("numbers"u8, "[1.0, 2.0, 3.0]"u8);
        input.Json.Set("values"u8, "[10.5, 20.5, 30.5, 40.5]"u8);
        
        // Test updating existing arrays at existing indices
        input.Json.Set("numbers/1"u8, 99.9);
        input.Json.Set("values/2"u8, 300.7);
        
        // Verify the arrays were modified correctly
        double[] numbersArray = input.Json.GetArray<double>("numbers"u8);
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
        
        // Test that attempting to set non-existent array fails
        var ex1 = Assert.Throws<InvalidOperationException>(() => input.Json.Set("nonexistent/0"u8, 42.0));
        Assert.That(ex1.Message, Does.Contain("does not exist"));
        
        // Test that attempting to set out-of-range index fails
        var ex2 = Assert.Throws<IndexOutOfRangeException>(() => input.Json.Set("numbers/5"u8, 42.0));
        Assert.That(ex2.Message, Does.Contain("out of range"));
        
        // Test alternative syntax with the indexer
        input.Json.Set("test"u8, "[100.0, 200.0]"u8);
        var element = input.Json["test/1"u8];
        element.Set(999.0);
        Assert.That(input.Json.GetDouble("test/1"u8), Is.EqualTo(999.0));
        
        // Test that indexer also fails for non-existent arrays  
        var element2 = input.Json["missing/0"u8];
        try
        {
            element2.Set(123.0);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Test]
    public void ArrayItemSetterWithTypedProperties()
    {
        InputModel input = new();
        
        // Set up typed arrays using the properties directly
        input.Numbers = new double[] { 1.0, 2.0, 3.0 };
        
        // Test updating existing array at existing indices using array syntax
        input.Json.Set("numbers/1"u8, 99.9);
        
        // Verify the real property was updated
        Assert.That(input.Numbers.Length, Is.EqualTo(3));
        Assert.That(input.Numbers[0], Is.EqualTo(1.0));
        Assert.That(input.Numbers[1], Is.EqualTo(99.9)); // Modified
        Assert.That(input.Numbers[2], Is.EqualTo(3.0));
        
        // Verify we can retrieve the modified value using array syntax
        Assert.That(input.Json.GetDouble("numbers/1"u8), Is.EqualTo(99.9));
        
        // Test that the property type checking is working
        Assert.That(input.Json.GetDouble("numbers/0"u8), Is.EqualTo(1.0));
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
