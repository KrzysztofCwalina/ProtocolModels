using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

public partial struct JsonPatch
{
    private enum ValueKind : byte
    {
        Json = 1,
        Int32 = 2,
        Utf8String = 4,
    }
    // value_offset (2 bytes) | value kind (1 byte) | 1 byte (reserved) |name (variable length) | value (variable length)
    private readonly struct JsonPatchEntry
    {
        private readonly byte[] _buffer;

        private const int KindOffset = 2;
        private const int NameOffset = 4;

        private ushort ValueOffset
        {
            get
            {
                Debug.Assert(_buffer != null);
                Debug.Assert(_buffer.Length >= NameOffset);
                return BinaryPrimitives.ReadUInt16LittleEndian(_buffer);
            }
        }
        public ValueKind Kind =>(ValueKind)_buffer[KindOffset];
        
        public ReadOnlyMemory<byte> Name
        {
            get
            {
                int valueOffset = ValueOffset;
                return _buffer.AsMemory(NameOffset, valueOffset - NameOffset);
            }
        }

        public ReadOnlyMemory<byte> Value
        {
            get
            {
                int valueOffset = ValueOffset;
                return _buffer.AsMemory(valueOffset);
            }
        }

        private Span<byte> ValueBuffer => _buffer.AsSpan(ValueOffset);

        public bool EqualsName(ReadOnlySpan<byte> name)
        {
            Debug.Assert(_buffer != null);

            if (_buffer.Length == NameOffset)
                return false; // Count properties have no name

            return Name.Span.SequenceEqual(name);
        }

        private JsonPatchEntry(ReadOnlySpan<byte> name, ValueKind kind, int valueLength)
        {
            int valueOffset = NameOffset + name.Length;
            if (valueOffset > ushort.MaxValue) throw new ArgumentException("name too long");

            _buffer = new byte[NameOffset + name.Length + valueLength];
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer, (ushort)(NameOffset + name.Length));
            _buffer[KindOffset] = (byte)kind;
            name.CopyTo(_buffer.AsSpan(NameOffset));
        }

        // string
        public JsonPatchEntry(ReadOnlySpan<byte> name, string text)
        {
            int valueOffset = NameOffset + name.Length;
            if (valueOffset > ushort.MaxValue) throw new ArgumentException("name too long");
            int valueLength = Encoding.UTF8.GetByteCount(text);
            _buffer = new byte[NameOffset + name.Length + valueLength];
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer, (ushort)(NameOffset + name.Length));
            _buffer[KindOffset] = (byte)ValueKind.Utf8String;
            name.CopyTo(_buffer.AsSpan(NameOffset));
            Encoding.UTF8.GetBytes(text, _buffer.AsSpan(valueOffset));
        }
        // Int32
        public JsonPatchEntry(ReadOnlySpan<byte> name, int value)
            : this(name, ValueKind.Int32, sizeof(int))
        {
            BinaryPrimitives.WriteInt32LittleEndian(ValueBuffer, value);
        }
        internal int GetInt32() => BinaryPrimitives.ReadInt32LittleEndian(ValueBuffer);
        
        internal void Set(int value)
        {
            Debug.Assert(this.Kind == ValueKind.Int32);
            BinaryPrimitives.WriteInt32LittleEndian(ValueBuffer, value);
        }
        // JSON
        public JsonPatchEntry(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
            : this(name, ValueKind.Json, json.Length)
        {
            json.CopyTo(_buffer.AsSpan(ValueOffset));
        }

        public override string ToString()
        {
            if (_buffer.Length == NameOffset)
            {
                // Count property - entire buffer is the int32 value
                return $"Count = {BinaryPrimitives.ReadInt32BigEndian(_buffer)}";
            }
            else
            {
                // Regular property - name starts at NameOffset, value starts at offset
                int offset = ValueOffset;
                return $"{Encoding.UTF8.GetString(_buffer, NameOffset, offset - NameOffset)} = {Encoding.UTF8.GetString(_buffer, offset, _buffer.Length - offset)}";
            }
        }

        public void WriteAsJson(Utf8JsonWriter writer)
        {
            int offset = ValueOffset;
            ReadOnlySpan<byte> name = Name.Span;
            ReadOnlySpan<byte> value = Value.Span;
            switch (Kind)
            {
                case ValueKind.Utf8String:
                    writer.WriteString(name, value);
                    break;
                case ValueKind.Int32:
                    writer.WriteNumber(name, BinaryPrimitives.ReadInt32LittleEndian(value));
                    break;
                case ValueKind.Json:
                    writer.WritePropertyName(name);
                    writer.WriteRawValue(value);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported value kind: {Kind}");
            }
        }
    }
}

