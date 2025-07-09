using NUnit.Framework;
using System;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Collections.Generic;
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
}