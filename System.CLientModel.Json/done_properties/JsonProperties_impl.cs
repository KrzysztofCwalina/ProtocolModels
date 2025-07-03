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
    // this is either null (empty) or the first property contains the count of properties (including count property)
    private Property[] _properties;

    public int Count => PrivateCount - 1;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
    {
        if (name.IsEmpty)
            throw new ArgumentException("Property name cannot be empty", nameof(name));

        if (_properties == null)
        {
            _properties = new Property[2];
            _properties[0] = Property.CreateInt32(2);
            _properties[1] = new Property(name, json);
            return;
        }

        int count = PrivateCount;
        // Check if the property already exists and update it if found
        for (int i = 1; i < count; i++)
        {
            if (_properties[i].EqualsName(name))
            {
                _properties[i] = new Property(name, json);
                return;
            }
        }

        EnsureCapacity();
        count++;
        PrivateCount = count;
        _properties[count] = new Property(name, json);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> json)
    {
        json = default;
        
        if (_properties == null) return false;

        int count = PrivateCount;
        // Search for the property by name
        for (int i = 1; i < count; i++)
        {
            if (_properties[i].EqualsName(name))
            {
                json = _properties[i].Value;
                return true;
            }
        }

        return false;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool Contains(ReadOnlySpan<byte> name)
    {
        if (_properties == null) return false;
        int count = PrivateCount; 
        // Search for the property by name
        for (int i = 0; i < count; i++)
        {
            if (_properties[i].EqualsName(name))
            {
                return true;
            }
        }
        return false;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Write(Utf8JsonWriter writer)
    {
        if (_properties == null) return;
        int count = PrivateCount;
        for (int i = 1; i < count; i++)
        {
            _properties[i].Write(writer);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        int count = PrivateCount;
        for (int i = 1; i < count; i++)
        {
            if (i > 0) sb.AppendLine(",");
            sb.Append(_properties[i].ToString());
        }
        if (count > 0)
            sb.AppendLine();
        return sb.ToString();

    }

    private void EnsureCapacity()
    {
        if (_properties == null)
        {
            Debug.Fail("this should never happen");
            _properties = new Property[2];
            _properties[0] = Property.CreateInt32(2);
            return;
        }

        int count = PrivateCount;
        if (count == _properties.Length)
        {
            Array.Resize(ref _properties, _properties.Length * 2);
        }
    }

    private int PrivateCount
    {
        get
        {
            if (_properties == null) return 0;
            return _properties[0].GetInt32();
        }
        set
        {
            Debug.Assert(_properties != null);
            _properties[0].SetInt32(value);
        }
    }

    [Runtime.CompilerServices.MethodImpl(Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void ThrowPropertyNotFoundException(ReadOnlySpan<byte> name)
    {
        throw new KeyNotFoundException(Encoding.UTF8.GetString(name.ToArray()));
    }

    private readonly struct Property
    {
        private readonly byte[] _buffer;
        private readonly int _valueOffset; // TODO: the offset should be in the buffer. It will be better for alignment

        internal ReadOnlySpan<byte> Name
        {
            get
            {
                Debug.Assert(_buffer != null);
                return _buffer.AsSpan(0, _valueOffset);
            }
        }

        internal ReadOnlySpan<byte> Value
        {
            get
            {
                Debug.Assert(_buffer != null);
                return _buffer.AsSpan(_valueOffset);
            }
        }

        internal Property(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            if (name.IsEmpty)
                throw new ArgumentException("Property name cannot be empty", nameof(name));

            _buffer = new byte[name.Length + value.Length];
            _valueOffset = name.Length;

            name.CopyTo(_buffer);
            value.CopyTo(_buffer.AsSpan(_valueOffset));
        }
        private Property(byte[] buffer, int valueOffset)
        {
            _buffer = buffer;
            _valueOffset = valueOffset;
        }

        internal bool EqualsName(ReadOnlySpan<byte> name)
        {
            Debug.Assert(_buffer != null);

            if (_valueOffset <= 0)
                return false;

            return Name.SequenceEqual(name);
        }

        internal void Write(Utf8JsonWriter writer)
        {
            Debug.Assert(_buffer != null);

            if (_buffer == null || _buffer.Length == 0)
                return;

            writer.WritePropertyName(Name);
            writer.WriteRawValue(Value);
        }

        public override string ToString()
            => $"{Encoding.UTF8.GetString(_buffer, 0, _valueOffset)} = {Encoding.UTF8.GetString(_buffer, _valueOffset, _buffer.Length - _valueOffset)}";

        // the count property support
        internal static Property CreateInt32(int value)
        {
            byte[] buffer = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            Property property = new(buffer, 0);
            return property;
        }
        internal void SetInt32(int value)
        {
            Debug.Assert(_valueOffset == 0);
            BinaryPrimitives.WriteInt32LittleEndian(_buffer, value);
        }
        internal int GetInt32()
        {
            Debug.Assert(_valueOffset == 0);
            return BinaryPrimitives.ReadInt32LittleEndian(_buffer);
        }
    }
}

