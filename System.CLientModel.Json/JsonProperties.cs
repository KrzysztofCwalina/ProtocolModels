using System.ClientModel.Primitives;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

// this is a datastructure for efficiently storing JSON properties
internal struct JsonProperties
{
    Property[] _properties;
    int _count; // TODO: we could optimize it to store the count in the first property

    public void Set(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        if (name.IsEmpty)
            throw new ArgumentException("Property name cannot be empty", nameof(name));

        // Check if the property already exists and update it if found
        for (int i = 0; i < _count; i++)
        {
            if (_properties[i].EqualsName(name))
            {
                _properties[i] = new Property(name, value);
                return;
            }
        }

        EnsureCapacity();
        _properties[_count++] = new Property(name, value);
    }

    public void Set(ReadOnlySpan<byte> name, object value)
    {
        ReadOnlyMemory<byte> valueBytes = value.ToJson();
        Set(name, valueBytes.Span);
    }

    private void EnsureCapacity()
    {
        if (_properties == null)
        {
            _properties = new Property[1];
        }
        else if (_count == _properties.Length)
        {
            Array.Resize(ref _properties, _properties.Length * 2);
        }
    }

    public bool TryGet(ReadOnlySpan<byte> name, out ReadOnlySpan<byte> value)
    {
        value = default;
        
        if (_properties == null || _count == 0)
            return false;

        // Search for the property by name
        for (int i = 0; i < _count; i++)
        {
            if (_properties[i].EqualsName(name))
            {
                value = _properties[i].Value;
                return true;
            }
        }

        return false;
    }

    internal void Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
    {
        if (_properties == null || _count == 0)
            return;

        for (int i = 0; i < _count; i++)
        {
            _properties[i].Write(writer, options);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < _count; i++)
        {
            if (i > 0) sb.AppendLine(",");
            sb.Append(_properties[i].ToString());
        }
        if (_count > 0)
            sb.AppendLine();
        return sb.ToString();

    }

    internal readonly struct Property
    {
        private readonly byte[] _buffer;
        private readonly int _valueOffset;
        
        public ReadOnlySpan<byte> Name 
        { 
            get 
            {
                Debug.Assert(_buffer != null);
                return _buffer.AsSpan(0, _valueOffset);
            }
        }
        
        public ReadOnlySpan<byte> Value 
        { 
            get 
            {
                Debug.Assert(_buffer != null);
                return _buffer.AsSpan(_valueOffset);
            }
        }
        
        public Property(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            if (name.IsEmpty)
                throw new ArgumentException("Property name cannot be empty", nameof(name));

            _buffer = new byte[name.Length + value.Length];
            _valueOffset = name.Length;
            
            name.CopyTo(_buffer);
            value.CopyTo(_buffer.AsSpan(_valueOffset));
        }

        public bool EqualsName(ReadOnlySpan<byte> name)
        {
            Debug.Assert(_buffer != null);
            
            if (_valueOffset <= 0)
                return false;

            return Name.SequenceEqual(name);
        }

        public void Write(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        {
            Debug.Assert(_buffer != null);
            
            if (_buffer == null || _buffer.Length == 0)
                return;
            
            writer.WritePropertyName(Name);
            writer.WriteRawValue(Value);
        }

        public override string ToString()
            => $"{Encoding.UTF8.GetString(_buffer, 0, _valueOffset)} = {Encoding.UTF8.GetString(_buffer, _valueOffset, _buffer.Length - _valueOffset)}" ;
    }
}

