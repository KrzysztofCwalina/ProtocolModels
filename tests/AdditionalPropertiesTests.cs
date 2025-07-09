using NUnit.Framework;
using System;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using AdditionalPropertiesType = System.ClientModel.Primitives.DictionaryStore;

namespace AdditionalProperties.Tests;

[TestFixture]
public class AdditionalPropertiesTests
{
    [Test]
    public void BasicStringOperations()
    {
        AdditionalPropertiesType props = new();
        
        // Test string set/get
        props.Set("name"u8, "John");
        Assert.That(props.GetString("name"u8), Is.EqualTo("John"));
        
        // Test string UTF8 get
        ReadOnlyMemory<byte> utf8 = props.GetStringUtf8("name"u8);
        Assert.That(Encoding.UTF8.GetString(utf8.Span), Is.EqualTo("John"));
        
        // Test Contains
        Assert.That(props.Contains("name"u8), Is.True);
        Assert.That(props.Contains("nonexistent"u8), Is.False);
    }
    
    [Test]
    public void BasicIntOperations()
    {
        AdditionalPropertiesType props = new();
        
        // Test int set/get
        props.Set("age"u8, 25);
        Assert.That(props.GetInt32("age"u8), Is.EqualTo(25));
        
        // Test Contains
        Assert.That(props.Contains("age"u8), Is.True);
    }
    
    [Test]
    public void BasicBoolOperations()
    {
        AdditionalPropertiesType props = new();
        
        // Test bool set/get
        props.Set("active"u8, true);
        props.Set("inactive"u8, false);
        
        // Boolean values don't have a GetBoolean method in ExtensionProperties
        // They are accessed via JSON or other means
        Assert.That(props.Contains("active"u8), Is.True);
        Assert.That(props.Contains("inactive"u8), Is.True);
    }
    
    [Test]
    public void BasicJsonOperations()
    {
        AdditionalPropertiesType props = new();
        
        // Test JSON set/get
        string jsonString = """{"name": "John", "age": 30}""";
        ReadOnlySpan<byte> jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        props.Set("data"u8, jsonBytes);
        
        BinaryData result = props.GetJson("data"u8);
        Assert.That(Encoding.UTF8.GetString(result.ToArray()), Is.EqualTo(jsonString));
        
        // Test Contains
        Assert.That(props.Contains("data"u8), Is.True);
    }
    
    [Test]
    public void JsonPointerNavigation()
    {
        AdditionalPropertiesType props = new();
        
        // Set JSON object
        string jsonString = """{"user": {"name": "John", "age": 30}}""";
        ReadOnlySpan<byte> jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        props.Set("data"u8, jsonBytes);
        
        // Test JSON pointer navigation
        Assert.That(props.GetString("data/user/name"u8), Is.EqualTo("John"));
        Assert.That(props.GetInt32("data/user/age"u8), Is.EqualTo(30));
    }
    
    [Test]
    public void RemoveOperations()
    {
        AdditionalPropertiesType props = new();
        
        // Set a property
        props.Set("name"u8, "John");
        Assert.That(props.Contains("name"u8), Is.True);
        
        // Remove the property
        props.Remove("name"u8);
        
        // Property should still exist (marked as removed)
        Assert.That(props.Contains("name"u8), Is.True);
    }
    
    [Test]
    public void NullOperations()
    {
        AdditionalPropertiesType props = new();
        
        // Set null property
        props.SetNull("value"u8);
        
        // Property should exist
        Assert.That(props.Contains("value"u8), Is.True);
    }
    
    [Test]
    public void WriteJsonOutput()
    {
        AdditionalPropertiesType props = new();
        
        // Add various properties
        props.Set("name"u8, "John");
        props.Set("age"u8, 30);
        props.Set("active"u8, true);
        props.Set("inactive"u8, false);
        props.SetNull("nullValue"u8);
        
        // Write to JSON
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartObject();
        props.Write(writer);
        writer.WriteEndObject();
        
        writer.Flush();
        
        string json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.That(json, Does.Contain("\"name\":\"John\""));
        Assert.That(json, Does.Contain("\"age\":30"));
        Assert.That(json, Does.Contain("\"active\":true"));
        Assert.That(json, Does.Contain("\"inactive\":false"));
        Assert.That(json, Does.Contain("\"nullValue\":null"));
    }
    
    [Test]
    public void ToStringOutput()
    {
        AdditionalPropertiesType props = new();
        
        // Add properties
        props.Set("name"u8, "John");
        props.Set("age"u8, 30);
        
        string result = props.ToString();
        
        // Should contain both properties
        Assert.That(result, Does.Contain("name: John"));
        Assert.That(result, Does.Contain("age: 30"));
    }
    
    [Test]
    public void PropertyNotFoundExceptions()
    {
        AdditionalPropertiesType props = new();
        
        // Test exceptions for non-existent properties
        Assert.Throws<KeyNotFoundException>(() => props.GetString("nonexistent"u8));
        Assert.Throws<KeyNotFoundException>(() => props.GetInt32("nonexistent"u8));
        Assert.Throws<KeyNotFoundException>(() => props.GetJson("nonexistent"u8));
        Assert.Throws<KeyNotFoundException>(() => props.GetStringUtf8("nonexistent"u8));
    }
    
    [Test]
    public void TypeMismatchExceptions()
    {
        AdditionalPropertiesType props = new();
        
        // Set a string value
        props.Set("name"u8, "John");
        
        // Try to access as different types
        Assert.Throws<KeyNotFoundException>(() => props.GetInt32("name"u8));
        Assert.Throws<KeyNotFoundException>(() => props.GetJson("name"u8));
        
        // Set an int value
        props.Set("age"u8, 30);
        
        // Try to access as different types
        Assert.Throws<KeyNotFoundException>(() => props.GetString("age"u8));
        Assert.Throws<KeyNotFoundException>(() => props.GetJson("age"u8));
    }
    
    [Test]
    public void EmptyPropertiesOperations()
    {
        AdditionalPropertiesType props = new();
        
        // Test operations on empty properties
        Assert.That(props.Contains("anything"u8), Is.False);
        Assert.That(props.ToString(), Is.EqualTo(string.Empty));
        
        // Write empty properties
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartObject();
        props.Write(writer);
        writer.WriteEndObject();
        
        writer.Flush();
        
        string json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.That(json, Is.EqualTo("{}"));
    }
    
    [Test]
    public void ValuesStoredAsJsonRepresentation()
    {
        AdditionalPropertiesType props = new();
        
        // Test integer stored as JSON
        props.Set("age"u8, 42);
        Assert.That(props.GetInt32("age"u8), Is.EqualTo(42));
        
        // Test string stored as JSON (with special characters)
        props.Set("message"u8, "Hello \"World\"");
        Assert.That(props.GetString("message"u8), Is.EqualTo("Hello \"World\""));
        
        // Test boolean stored as JSON
        props.Set("active"u8, true);
        props.Set("inactive"u8, false);
        
        // Test null stored as JSON
        props.SetNull("nullValue"u8);
        
        // Test serialization to ensure everything works end-to-end
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartObject();
        props.Write(writer);
        writer.WriteEndObject();
        
        writer.Flush();
        
        string json = Encoding.UTF8.GetString(stream.ToArray());
        Assert.That(json, Does.Contain("\"age\":42"));
        Assert.That(json, Does.Contain("\"message\":\"Hello \\u0022World\\u0022\""));  // JSON uses Unicode escapes
        Assert.That(json, Does.Contain("\"active\":true"));
        Assert.That(json, Does.Contain("\"inactive\":false"));
        Assert.That(json, Does.Contain("\"nullValue\":null"));
    }
    
    [Test]
    public void VerifyInternalJsonStorage()
    {
        AdditionalPropertiesType props = new();
        
        // Set some values
        props.Set("number"u8, 42);
        props.Set("text"u8, "hello");
        props.Set("flag"u8, true);
        
        // Use reflection to inspect the internal storage
        var propertiesField = typeof(AdditionalPropertiesType).GetField("_properties", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(propertiesField, Is.Not.Null, "Should have _properties field");
        
        var propertiesDict = propertiesField.GetValue(props) as 
            System.Collections.Generic.Dictionary<byte[], byte[]>;
        Assert.That(propertiesDict, Is.Not.Null, "Should have dictionary");
        Assert.That(propertiesDict.Count, Is.EqualTo(3), "Should have 3 entries");
        
        // Check that values are stored as JSON (after the ValueKind byte)
        foreach (var kvp in propertiesDict)
        {
            string propertyName = Encoding.UTF8.GetString(kvp.Key);
            byte[] encodedValue = kvp.Value;
            
            Assert.That(encodedValue.Length, Is.GreaterThan(1), 
                $"Property {propertyName} should have value data after kind byte");
            
            // Skip the first byte (ValueKind) and get the actual stored value
            byte[] valueBytes = encodedValue.AsSpan(1).ToArray();
            string storedValue = Encoding.UTF8.GetString(valueBytes);
            
            switch (propertyName)
            {
                case "number":
                    Assert.That(storedValue, Is.EqualTo("42"), 
                        "Number should be stored as JSON number string");
                    break;
                case "text":
                    Assert.That(storedValue, Is.EqualTo("\"hello\""), 
                        "String should be stored as JSON string with quotes");
                    break;
                case "flag":
                    Assert.That(storedValue, Is.EqualTo("true"), 
                        "Boolean should be stored as JSON boolean string");
                    break;
            }
        }
    }
}