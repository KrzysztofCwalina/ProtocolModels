﻿using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

public partial struct RecordStore
{
    internal enum ValueKind : byte
    {
        Json = 1,
        Int32 = 2,
        Utf8String = 3,
        Removed = 4,
        Null = 5,
        BooleanTrue = 6,
        BooleanFalse = 7,
        // TODO: add other types: ulong, long, double, float, ushort?, short?
        // TODO: add support for arrays?
    }
    // value_offset (2 bytes) | value kind (1 byte) | 1 byte (reserved) |name (variable length) | value (variable length)
    internal readonly struct PropertyRecord
    {
        // TODO: maybe allow more than one record in a single byte[]. So that an array of records in a single byte[]
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

        internal Span<byte> ValueBuffer => _buffer.AsSpan(ValueOffset);

        public bool EqualsName(ReadOnlySpan<byte> name)
        {
            Debug.Assert(_buffer != null);

            if (_buffer.Length == NameOffset)
                return false; // Count properties have no name

            return Name.Span.SequenceEqual(name);
        }

        private PropertyRecord(ReadOnlySpan<byte> name, ValueKind kind, int valueLength)
        {
            int valueOffset = NameOffset + name.Length;
            if (valueOffset > ushort.MaxValue) throw new ArgumentException("name too long");

            _buffer = new byte[NameOffset + name.Length + valueLength];
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer, (ushort)(NameOffset + name.Length));
            _buffer[KindOffset] = (byte)kind;
            name.CopyTo(_buffer.AsSpan(NameOffset));
        }

        // string
        public PropertyRecord(ReadOnlySpan<byte> name, string text)
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
        public PropertyRecord(ReadOnlySpan<byte> name, int value)
            : this(name, ValueKind.Int32, sizeof(int))
        {
            BinaryPrimitives.WriteInt32LittleEndian(ValueBuffer, value);
        }

        // Boolean
        public PropertyRecord(ReadOnlySpan<byte> name, bool value)
            : this(name, value ? ValueKind.BooleanTrue : ValueKind.BooleanFalse, 0)
        { }

        // JSON
        public PropertyRecord(ReadOnlySpan<byte> name, ReadOnlySpan<byte> json)
            : this(name, ValueKind.Json, json.Length)
        {
            json.CopyTo(_buffer.AsSpan(ValueOffset));
        }
        
        // Removed property
        public static PropertyRecord CreateRemoved(ReadOnlySpan<byte> name)
        {
            return new PropertyRecord(name, ValueKind.Removed, 0);
        }
        
        // Null property
        public static PropertyRecord CreateNull(ReadOnlySpan<byte> name)
        {
            return new PropertyRecord(name, ValueKind.Null, 0);
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
            // Skip removed properties during serialization
            if (Kind == ValueKind.Removed) return;
            
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
                case ValueKind.BooleanTrue:
                    writer.WriteBoolean(name, true);
                    break;
                case ValueKind.BooleanFalse:
                    writer.WriteBoolean(name, false);
                    break;
                case ValueKind.Json:
                    writer.WritePropertyName(name);
                    writer.WriteRawValue(value);
                    break;
                case ValueKind.Null:
                    writer.WriteNull(name);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported value kind: {Kind}");
            }
        }

        public int GetInt32() => BinaryPrimitives.ReadInt32LittleEndian(ValueBuffer);

        public void Set(int value)
        {
            Debug.Assert(this.Kind == ValueKind.Int32);
            BinaryPrimitives.WriteInt32LittleEndian(ValueBuffer, value);
        }

        // Boolean:
        public bool GetBoolean() => Kind == ValueKind.BooleanTrue;

        public void Set(bool value)
        {
            Debug.Assert(this.Kind == ValueKind.BooleanTrue || this.Kind == ValueKind.BooleanFalse);
            _buffer[KindOffset] = (byte)(value ? ValueKind.BooleanTrue : ValueKind.BooleanFalse);
        }
    }
}

