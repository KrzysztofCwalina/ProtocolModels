using System.Reflection;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

// TODO: will this work for AOT?
public abstract class ExtensibleModel<T> : JsonModel<T>, IExtensibleModel
{
    private JsonProperties additionalProperties = new();

    // method to access and manipulate model properties (whether CLR or JSON)

    // this overload (ROS) is more efficient for JSON properties than CLR properties
    protected bool TryGetClrOrJsonProperty(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> json)
    {
        // if additional property exists, it's either trully additional or changed type
        if (additionalProperties.TryGet(name, out json))
        {
            return true;
        }

        if (TryGetClrProperty(name, out object? clrValue))
        {
            // this allocates
            json = clrValue.ToJson().Span;
            return true;
        }

        throw new KeyNotFoundException($"Property '{Encoding.UTF8.GetString(name)}' not found.");
    }

    protected void SetClrOrJsonProperty(ReadOnlySpan<byte> name, object value)
    {
        if (TrySetClrProperty(name, value))
        {
            return;
        }
        
        // No CLR property exists, store in additionalProperties
        additionalProperties.Set(name, value);
        return;
    }

    protected void SetClrOrJsonProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        // Check if a CLR property with this name exists
        if (!TryGetClrPropertyType(name, out Type? ptype))
        {
            // No CLR property exists, store in additionalProperties
            additionalProperties.Set(name, value);
            return;
        }

        // A CLR property exists, check if value can be deserialized to it
        if (SerializationHelpers.TryJsonToClrValue(value, ptype, out object clrValue))
        {
            if (!TrySetClrProperty(name, clrValue))
            {
                throw new NotImplementedException();
            }
        }

        additionalProperties.Set(name, value);
    }

    #region IExtensibleModel implementations
    bool IExtensibleModel.TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
        => TryGetClrOrJsonProperty(name, out value);
    void IExtensibleModel.Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        => SetClrOrJsonProperty(name, value);
    bool IExtensibleModel.TryGetPropertyType(ReadOnlySpan<byte> name, out Type? value)
        => TryGetClrPropertyType(name, out value);
    #endregion
    #region IJsonModel<T> helpers
    // the following two methods are used by IJsonModel<T> implemetnations to write and read additional properties
    protected void WriteAdditionalProperties(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    => additionalProperties.Write(writer, options);
    protected static void ReadAdditionalProperty(JsonView model, JsonProperty property)
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes(property.Name);
        byte[] valueBytes = Encoding.UTF8.GetBytes(property.Value.GetRawText());
        model.Set(nameBytes, (ReadOnlySpan<byte>)valueBytes);
    }
    #endregion
    #region PSEUDOREFLECTION
    // this can be overriden to avoid reflection or when properties are renamed
    protected virtual bool TryGetClrPropertyType(ReadOnlySpan<byte> name, out Type? type)
    {
        Type modelType = GetType();
        string clrProperty = name.ToClrPropertyName();
        var propertyInfo = modelType.GetProperty(clrProperty);

        if (propertyInfo != null)
        {
            type = propertyInfo.PropertyType;
            return true;
        }
        type = null;
        return false;
    }
    protected virtual bool TryGetClrProperty(ReadOnlySpan<byte> name, out object? value)
    {
        Type modelType = GetType();
        string clrProperty = name.ToClrPropertyName();
        var propertyInfo = modelType.GetProperty(clrProperty);

        if (propertyInfo != null)
        {
            value = propertyInfo.GetValue(this, null);
            return true;
        }
        value = null;
        return false;
    }
    protected virtual bool TrySetClrProperty(ReadOnlySpan<byte> name, object value)
    {
        Type modelType = GetType();
        string clrProperty = name.ToClrPropertyName();
        PropertyInfo propertyInfo = modelType.GetProperty(clrProperty);
        if (propertyInfo != null)
        {
            Type propertyType = propertyInfo?.PropertyType!;
            Type valueType = value.GetType();
            if (propertyType.IsAssignableFrom(valueType))
            {
                try
                {
                    propertyInfo.SetValue(this, value);
                    return true;
                }
                catch
                {
                    // set additional property (below)
                }
            }
            else
            {
                return false; // the property exists, but the types are not compatible
            }
        }
        throw new ArgumentException($"Property '{Encoding.UTF8.GetString(name)}' does not exist or is not assignable.", nameof(name));
    }
    #endregion
    #region CONVENIENCE
    public JsonView Json => new JsonView((IExtensibleModel)this);
    // cute helper APIs
    public ReadOnlySpan<byte> this[ReadOnlySpan<byte> name]
    {
        set => Json.Set(name, value);
    }
    public object this[string name]
    {
        set => SetClrOrJsonProperty(Encoding.UTF8.GetBytes(name), value);
    }
    #endregion
}

