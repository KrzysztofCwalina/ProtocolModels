using System.ClientModel.Primitives;
using System.Text.Json;

// it's internal, so we can modify it later
internal interface IJsonModel
{
    bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value);
    void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value);
    bool TryGetPropertyType(ReadOnlySpan<byte> name, out Type value);
}