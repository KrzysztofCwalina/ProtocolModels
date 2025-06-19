using System.Text;
/// <summary>
/// Extension methods for IJsonModel
/// </summary>
internal static class JsonModelExtensions
{
    public static ReadOnlySpan<byte> Get(this IJsonModel model, string name)
    {
        ReadOnlySpan<byte> nameBytes = Encoding.UTF8.GetBytes(name);
        if (!model.TryGet(nameBytes, out ReadOnlySpan<byte> value))
            throw new KeyNotFoundException($"Property '{name}' not found");
        return value;
    }

    public static ReadOnlySpan<byte> Get(this IJsonModel model, ReadOnlySpan<byte> name)
    {
        if (!model.TryGet(name, out ReadOnlySpan<byte> value))
        {
            // Only convert to string for the exception message
            throw new KeyNotFoundException($"Property not found");
        }
        return value;
    }

    public static ReadOnlySpan<byte> Get(this JsonProperties properties, string name)
    {
        ReadOnlySpan<byte> nameBytes = Encoding.UTF8.GetBytes(name);
        if (!properties.TryGet(nameBytes, out ReadOnlySpan<byte> value))
            throw new KeyNotFoundException($"Property '{name}' not found");
        return value;
    }

    public static ReadOnlySpan<byte> Get(this JsonProperties properties, ReadOnlySpan<byte> name)
    {
        if (!properties.TryGet(name, out ReadOnlySpan<byte> value))
        {
            // Only convert to string for the exception message
            throw new KeyNotFoundException($"Property not found");
        }
        return value;
    }
}

