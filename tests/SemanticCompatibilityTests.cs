using NUnit.Framework;
using System;
using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text;
using System.IO;
using System.Collections.Generic;
using AdditionalPropertiesType = System.ClientModel.Primitives.AdditionalProperties;

namespace AdditionalProperties.Tests;

[TestFixture]
public class SemanticCompatibilityTests
{
    [Test]
    public void CompareWithExtensionProperties()
    {
        // Create both types
        AdditionalPropertiesType additionalProps = new();
        ExtensionProperties extensionProps = new();
        
        // Set same values
        additionalProps.Set("name"u8, "John");
        extensionProps.Set("name"u8, "John");
        
        additionalProps.Set("age"u8, 30);
        extensionProps.Set("age"u8, 30);
        
        additionalProps.Set("active"u8, true);
        extensionProps.Set("active"u8, true);
        
        // Compare results
        Assert.That(additionalProps.GetString("name"u8), Is.EqualTo(extensionProps.GetString("name"u8)));
        Assert.That(additionalProps.GetInt32("age"u8), Is.EqualTo(extensionProps.GetInt32("age"u8)));
        Assert.That(additionalProps.Contains("active"u8), Is.EqualTo(extensionProps.Contains("active"u8)));
        
        // Test JSON serialization compatibility
        using var stream1 = new MemoryStream();
        using var writer1 = new Utf8JsonWriter(stream1);
        writer1.WriteStartObject();
        additionalProps.Write(writer1);
        writer1.WriteEndObject();
        writer1.Flush();
        
        using var stream2 = new MemoryStream();
        using var writer2 = new Utf8JsonWriter(stream2);
        writer2.WriteStartObject();
        extensionProps.Write(writer2);
        writer2.WriteEndObject();
        writer2.Flush();
        
        string json1 = Encoding.UTF8.GetString(stream1.ToArray());
        string json2 = Encoding.UTF8.GetString(stream2.ToArray());
        
        // Parse both JSONs to compare structure (order may differ)
        var doc1 = JsonDocument.Parse(json1);
        var doc2 = JsonDocument.Parse(json2);
        
        // Check that all properties exist in both
        Assert.That(doc1.RootElement.GetProperty("name").GetString(), Is.EqualTo(doc2.RootElement.GetProperty("name").GetString()));
        Assert.That(doc1.RootElement.GetProperty("age").GetInt32(), Is.EqualTo(doc2.RootElement.GetProperty("age").GetInt32()));
        Assert.That(doc1.RootElement.GetProperty("active").GetBoolean(), Is.EqualTo(doc2.RootElement.GetProperty("active").GetBoolean()));
    }
    
    [Test]
    public void JsonPointerCompatibility()
    {
        AdditionalPropertiesType additionalProps = new();
        ExtensionProperties extensionProps = new();
        
        string jsonData = """{"user": {"name": "John", "age": 30}}""";
        ReadOnlySpan<byte> jsonBytes = Encoding.UTF8.GetBytes(jsonData);
        
        additionalProps.Set("data"u8, jsonBytes);
        extensionProps.Set("data"u8, jsonBytes);
        
        // Test JSON pointer navigation
        Assert.That(additionalProps.GetString("data/user/name"u8), Is.EqualTo(extensionProps.GetString("data/user/name"u8)));
        Assert.That(additionalProps.GetInt32("data/user/age"u8), Is.EqualTo(extensionProps.GetInt32("data/user/age"u8)));
    }
}