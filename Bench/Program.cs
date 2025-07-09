using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.ClientModel.Primitives;
using System.Text;

BenchmarkRunner.Run<BuifferVsDictionary>();

[MemoryDiagnoser]
public class BuifferVsDictionary
{
    Dictionary<string, int> _dictionary = new();
    RecordStore _recordStore = new();
    DictionaryStore _dictionaryStore = new();
    byte[] _sequential;
    int _sequentialIndex;

    int max = 3;
    byte[] maxUtf8;
    string maxString;

    public BuifferVsDictionary()
    {
        _sequential = new byte[max * 20];
        _sequentialIndex = 0;
        for (int i = 1; i <= max; i++) {
            _dictionary.Add($"P{i}", i);
            byte[] nextName = Encoding.UTF8.GetBytes($"P{i}");
            _recordStore.Set(nextName, i);
            _dictionaryStore.Set(nextName, i);
            nextName.CopyTo(_sequential.AsSpan(_sequentialIndex));
            _sequentialIndex += nextName.Length;
            _sequential[_sequentialIndex] = 0; // delimiter
            _sequentialIndex++;
        }

        maxString = $"P{max}";
        maxUtf8= Encoding.UTF8.GetBytes(maxString);
    }

    [Benchmark]
    public bool RawDictionaryContains() => _dictionary.ContainsKey(maxString);

    [Benchmark]
    public bool CustomRecordStoreContains() => _recordStore.Contains(maxUtf8);

    [Benchmark]
    public bool DictionaryBasedStoreContains() => _dictionaryStore.Contains(maxUtf8);

    [Benchmark]
    public bool SequentialNamesContains() => _sequential.AsSpan(0, _sequentialIndex).IndexOf(maxUtf8) != -1;

    [Benchmark]
    public bool AllocateDictionaryAndAdd()
    {
        Dictionary<string, int> dict = new();
        dict.Add("P1", max);
        return dict.ContainsKey(maxString);
    }

    [Benchmark]
    public bool AllocateCustomRecordStoreAndAdd()
    {
        RecordStore buffer = new();
        buffer.Set("P1"u8, max);
        return buffer.Contains(maxUtf8);
    }

    [Benchmark]
    public bool AllocateDictionaryBasedStoreAndAdd()
    {
        DictionaryStore additionalProps = new();
        additionalProps.Set("P1"u8, max);
        return additionalProps.Contains(maxUtf8);
    }
}