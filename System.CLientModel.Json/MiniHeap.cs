using System;
using static System.ClientModel.Primitives.RecordStore;

namespace System.CLientModel.Json;
public struct MiniHeap
{
    byte[] _heap;

    public MiniHeap(byte[] heap)
    {
        _heap = heap;
    }

    internal void Set(PropertyRecord entry)
    {
        throw new NotImplementedException();
    }
    internal PropertyRecord Get(ReadOnlySpan<byte> name) 
    {
        throw new NotImplementedException();
    }
    internal PropertyRecord Get(int address)
    {
        throw new NotImplementedException();
    }

    PropertyRecord CreateObjectDescriptor(ReadOnlySpan<byte> names)
    {
        PropertyRecord descriptor = new PropertyRecord("$descriptor"u8, names.Length);
        names.CopyTo(descriptor.ValueBuffer);
        return descriptor;
    }
}
