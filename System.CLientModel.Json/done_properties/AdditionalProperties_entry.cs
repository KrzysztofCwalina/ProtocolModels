using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace System.ClientModel.Primitives;

public partial struct AdditionalProperties
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

    internal readonly struct PropertyValue
    {
        private readonly object? _value;
        private readonly ValueKind _kind;

        public ValueKind Kind => _kind;

        // String constructor
        public PropertyValue(string text)
        {
            _value = text;
            _kind = ValueKind.Utf8String;
        }
        
        // Int32 constructor
        public PropertyValue(int value)
        {
            _value = value;
            _kind = ValueKind.Int32;
        }

        // Boolean constructor
        public PropertyValue(bool value)
        {
            _value = value;
            _kind = value ? ValueKind.BooleanTrue : ValueKind.BooleanFalse;
        }

        // JSON constructor
        public PropertyValue(ReadOnlySpan<byte> json)
        {
            _value = json.ToArray(); // Store as byte array
            _kind = ValueKind.Json;
        }

        private PropertyValue(ValueKind kind)
        {
            _value = null;
            _kind = kind;
        }

        // Static factory methods for special values
        public static PropertyValue CreateRemoved()
        {
            return new PropertyValue(ValueKind.Removed);
        }
        
        public static PropertyValue CreateNull()
        {
            return new PropertyValue(ValueKind.Null);
        }

        // Property accessors
        public string StringValue
        {
            get
            {
                Debug.Assert(_kind == ValueKind.Utf8String);
                return (string)_value!;
            }
        }

        public ReadOnlyMemory<byte> Utf8Bytes
        {
            get
            {
                Debug.Assert(_kind == ValueKind.Utf8String);
                return Encoding.UTF8.GetBytes((string)_value!);
            }
        }

        public int Int32Value
        {
            get
            {
                Debug.Assert(_kind == ValueKind.Int32);
                return (int)_value!;
            }
        }

        public bool BooleanValue
        {
            get
            {
                Debug.Assert(_kind == ValueKind.BooleanTrue || _kind == ValueKind.BooleanFalse);
                return _kind == ValueKind.BooleanTrue;
            }
        }

        public ReadOnlyMemory<byte> JsonBytes
        {
            get
            {
                Debug.Assert(_kind == ValueKind.Json);
                return (byte[])_value!;
            }
        }

        public override string ToString()
        {
            return _kind switch
            {
                ValueKind.Utf8String => StringValue,
                ValueKind.Int32 => Int32Value.ToString(),
                ValueKind.BooleanTrue => "true",
                ValueKind.BooleanFalse => "false",
                ValueKind.Json => Encoding.UTF8.GetString(JsonBytes.Span),
                ValueKind.Null => "null",
                ValueKind.Removed => "<removed>",
                _ => throw new NotSupportedException($"Unsupported value kind: {_kind}")
            };
        }

        public void WriteAsJson(Utf8JsonWriter writer, string propertyName)
        {
            // Skip removed properties during serialization
            if (_kind == ValueKind.Removed) return;
            
            ReadOnlySpan<byte> nameBytes = Encoding.UTF8.GetBytes(propertyName);
            
            switch (_kind)
            {
                case ValueKind.Utf8String:
                    writer.WriteString(nameBytes, StringValue);
                    break;
                case ValueKind.Int32:
                    writer.WriteNumber(nameBytes, Int32Value);
                    break;
                case ValueKind.BooleanTrue:
                    writer.WriteBoolean(nameBytes, true);
                    break;
                case ValueKind.BooleanFalse:
                    writer.WriteBoolean(nameBytes, false);
                    break;
                case ValueKind.Json:
                    writer.WritePropertyName(nameBytes);
                    writer.WriteRawValue(JsonBytes.Span);
                    break;
                case ValueKind.Null:
                    writer.WriteNull(nameBytes);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported value kind: {_kind}");
            }
        }
    }
}