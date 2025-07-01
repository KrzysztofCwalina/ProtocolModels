using System.Text;
/// <summary>
/// Extension methods for IJsonModel
/// </summary>
internal static class JsonModelExtensions
{
    // TODO: can this be eliminated?
    public static ReadOnlySpan<byte> Get(this IExtensibleModel model, ReadOnlySpan<byte> name)
    {
        if (!model.TryGet(name, out ReadOnlySpan<byte> value))
        {
            // Only convert to string for the exception message
            throw new KeyNotFoundException($"Property not found");
        }
        return value;
    }

    public static string ToClrPropertyName(this ReadOnlySpan<byte> jsonName)
    {
        Span<byte> upperCased = stackalloc byte[jsonName.Length];
        jsonName.CopyTo(upperCased);
        char upper = Char.ToUpper((char)jsonName[0]);
        upperCased[0] = (byte)upper;
        string clrName = Encoding.UTF8.GetString(upperCased);
        return clrName;
    }
}

