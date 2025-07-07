using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

// this is a datastructure for efficiently storing JSON properties
public partial struct JsonPatch
{
    // this is either null (empty) or the first property contains the count of properties (including count property)
    private PropertyRecord[] _entries;

    public int Count => PrivateCount - 1;

    private int IndexOf(ReadOnlySpan<byte> name)
    {
        if (_entries == null) return -1;
        int count = PrivateCount;
        // Search for the property by name (skip index 0 which is the count property)
        for (int i = 1; i < count; i++)
        {
            if (_entries[i].EqualsName(name))
            {
                return i;
            }
        }
        return -1;
    }

    private void Add(PropertyRecord entry)
    {
        if (_entries == null)
        {
            _entries = new PropertyRecord[2];
            _entries[0] = new PropertyRecord("$count"u8, 2);
            _entries[1] = entry;
            return;
        }
        EnsureCapacity();
        int count = PrivateCount;
        PrivateCount = count + 1;
        _entries[count] = entry;
    }

    private void Set(PropertyRecord entry)
    {
        ReadOnlyMemory<byte> name = entry.Name;
        int index = IndexOf(name.Span);
        if (index >= 0)
        {
            _entries[index] = entry;
        }
        else
        {
            Add(entry);
        }
    }

    private PropertyRecord GetAt(int index)
    {
        return _entries[index];
    }

    private PropertyRecord Get(ReadOnlySpan<byte> name)
    {
        int index = IndexOf(name);
        if (index < 0) ThrowPropertyNotFoundException(name);
        return _entries[index];
    }

    public bool Contains(ReadOnlySpan<byte> name)
        => IndexOf(name) >= 0;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Write(Utf8JsonWriter writer)
    {
        if (_entries == null) return;
        int count = PrivateCount;
        for (int i = 1; i < count; i++)
        {
            PropertyRecord entry = _entries[i];
            entry.WriteAsJson(writer);
        }
    }

    public override string ToString()
    {
        // TODO: reimplement this in terms of Write method (above)
        StringBuilder sb = new StringBuilder();
        int count = PrivateCount;
        for (int i = 1; i < count; i++)
        {
            if (i > 0) sb.AppendLine(",");
            sb.Append(Encoding.UTF8.GetString(_entries[i].Name.Span));
            sb.Append(": ");
            sb.Append(Encoding.UTF8.GetString(_entries[i].Value.Span));
        }
        if (count > 0)
            sb.AppendLine();
        return sb.ToString();

    }

    private void EnsureCapacity()
    {
        if (_entries == null)
        {
            Debug.Fail("this should never happen");
            _entries = new PropertyRecord[2];
            _entries[0] = new PropertyRecord("$count"u8, 1);
            return;
        }

        int count = PrivateCount;
        if (count == _entries.Length)
        {
            Array.Resize(ref _entries, _entries.Length * 2);
        }
    }

    private int PrivateCount
    {
        get
        {
            if (_entries == null) return 0;
            return _entries[0].GetInt32();
        }
        set
        {
            Debug.Assert(_entries != null);
            Debug.Assert(_entries[0].EqualsName("$count"u8));
            _entries[0].Set(value);
        }
    }

    [Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private void ThrowPropertyNotFoundException(ReadOnlySpan<byte> name)
    {
        throw new KeyNotFoundException(Encoding.UTF8.GetString(name.ToArray()));
    }
}

