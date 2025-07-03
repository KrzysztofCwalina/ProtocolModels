using System.Buffers.Binary;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

// this is a datastructure for efficiently storing JSON properties
public partial struct JsonProperties
{
    public void Set(ReadOnlySpan<byte> name, string value)
        => Set(name, Encoding.UTF8.GetBytes(value));

    public string GetString(ReadOnlySpan<byte> name)
    {
        if (!TryGet(name, out ReadOnlySpan<byte> value))
            ThrowPropertyNotFoundException(name);
        return Encoding.UTF8.GetString(value);
    }
}

