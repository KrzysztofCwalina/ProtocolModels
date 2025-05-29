using System.ClientModel.Primitives;

public class Tests
{
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
        
        // TODO: implement support for setting array elements
        // TOOD: implement += <bytes> operator
        // TODO: type hierarchy
        // TOOD: what if there is JSON and CLR property returning complex object?

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
