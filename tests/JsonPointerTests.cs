using NUnit.Framework;
using System;
using System.Text;
using System.Collections.Generic;

public class JsonPointerTests
{
    const string json = """
    {
        "foo": "bar",
        "numbers": [1, 2, 3],
        "empty": {},
        "a/b": "slash in key",
        "c%d": "percent in key",
        "e^f": "caret in key",
        "g|h": "pipe in key",
        "i\\j": "backslash in key",
        "k\"l": "quotes in key",
        "m~n": "tilde in key",
        "nested": {
            "child": {
                "value": 123,
                "name": "test"
            },
            "array": [4, 5, 6]
        },
        "boolValue": true,
        "nullValue": null
    }
    """;

    [TestCase("/foo", typeof(string), ExpectedResult = "bar")]
    [TestCase("/numbers/0", typeof(int), ExpectedResult = 1)]
    [TestCase("/numbers/1", typeof(int), ExpectedResult = 2)]
    [TestCase("/numbers/2", typeof(int), ExpectedResult = 3)]
    [TestCase("/c%d", typeof(string), ExpectedResult = "percent in key")]
    [TestCase("/e^f", typeof(string), ExpectedResult = "caret in key")]
    [TestCase("/g|h", typeof(string), ExpectedResult = "pipe in key")]
    [TestCase("/i\\j", typeof(string), ExpectedResult = "backslash in key")]
    [TestCase("/k\"l", typeof(string), ExpectedResult = "quotes in key")]
    [TestCase("/nested/child/value", typeof(int), ExpectedResult = 123)]
    [TestCase("/nested/child/name", typeof(string), ExpectedResult = "test")]
    [TestCase("/nested/array/1", typeof(int), ExpectedResult = 5)]
    [TestCase("/boolValue", typeof(bool), ExpectedResult = true)]
    //[TestCase("", typeof(ReadOnlySpan<byte>), ExpectedResult = json)]
    //[TestCase("/nullValue", typeof(object), ExpectedResult = null)]
    public object SmokeTests(string pointer, Type valueType)
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        byte[] pointerBytes = Encoding.UTF8.GetBytes(pointer);

        if (valueType == typeof(string)) {
            return JsonPointer.GetString(jsonBytes, pointerBytes);       
        }
        else if (valueType == typeof(bool))
        {
            return JsonPointer.GetBoolean(jsonBytes, pointerBytes);
        }
        else if (valueType == typeof(int))
        {
            return JsonPointer.GetInt32(jsonBytes, pointerBytes);
        }
        else if (valueType == typeof(double))
        {
            return JsonPointer.GetDouble(jsonBytes, pointerBytes);
        }
        else if (valueType == typeof(ReadOnlySpan<byte>))
        {
            ReadOnlySpan<byte> bytes = JsonPointer.GetUtf8(jsonBytes, pointerBytes); 
            return Encoding.UTF8.GetString(bytes);
        }
        else if (valueType == typeof(int[]))
        {
            return JsonPointer.GetInt32Array(jsonBytes, pointerBytes);
        }
        else
        {
            throw new ArgumentException($"Unsupported value type: {valueType}", nameof(valueType));
        }
    }
        
    [TestCase("foo", typeof(string), typeof(ArgumentException))]
    [TestCase("/nonexistent", typeof(string), typeof(KeyNotFoundException))]
    [TestCase("/nonexistent", typeof(bool), typeof(KeyNotFoundException))]
    [TestCase("/nested/nonexistent", typeof(string), typeof(KeyNotFoundException))]
    [TestCase("/numbers/999", typeof(int), typeof(IndexOutOfRangeException))]
    [TestCase("/numbers/999", typeof(string), typeof(IndexOutOfRangeException))]
    [TestCase("/nested/array/99", typeof(string), typeof(IndexOutOfRangeException))]
    [TestCase("/foo", typeof(bool), typeof(InvalidOperationException))]
    [TestCase("/boolValue", typeof(int), typeof(InvalidOperationException))]
    [TestCase("/nested/child/value", typeof(string), typeof(InvalidOperationException))]
    [TestCase("~", typeof(string), typeof(ArgumentException))]
    public void InvalidPointersShouldThrow(string pointer, Type accessorType, Type expectedExceptionType)
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        byte[] pointerBytes = Encoding.UTF8.GetBytes(pointer);
        string errorMessage = $"Expected {expectedExceptionType.Name} for pointer '{pointer}' with {accessorType.Name} accessor";
        
        if (accessorType == typeof(string))
        {
            Assert.Throws(expectedExceptionType, () => JsonPointer.GetString(jsonBytes, pointerBytes), errorMessage);
        }
        else if (accessorType == typeof(bool))
        {
            Assert.Throws(expectedExceptionType, () => JsonPointer.GetBoolean(jsonBytes, pointerBytes), errorMessage);
        }
        else if (accessorType == typeof(int))
        {
            Assert.Throws(expectedExceptionType, () => JsonPointer.GetInt32(jsonBytes, pointerBytes), errorMessage);
        }
        else if (accessorType == typeof(double))
        {
            Assert.Throws(expectedExceptionType, () => JsonPointer.GetDouble(jsonBytes, pointerBytes), errorMessage);
        }
        else if (accessorType == typeof(double[]))
        {
            Assert.Throws(expectedExceptionType, () => JsonPointer.GetDoubleArray(jsonBytes, pointerBytes), errorMessage);
        }
        else
        {
            throw new ArgumentException($"Unsupported accessor type: {accessorType}", nameof(accessorType));
        }
    }
}
