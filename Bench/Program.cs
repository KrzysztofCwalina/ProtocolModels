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
    ExtensionProperties _buffer = new();
    byte[] _sequential;
    int _sequentialIndex;

    int max = 2;
    byte[] maxUtf8;
    string maxString;

    public BuifferVsDictionary()
    {
        _sequential = new byte[max * 20];
        _sequentialIndex = 0;
        for (int i = 1; i <= max; i++) {
            _dictionary.Add($"P{i}", i);
            byte[] nextName = Encoding.UTF8.GetBytes($"P{i}");
            _buffer.Set(nextName, i);
            nextName.CopyTo(_sequential.AsSpan(_sequentialIndex));
            _sequentialIndex += nextName.Length;
            _sequential[_sequentialIndex] = 0; // delimiter
            _sequentialIndex++;
        }

        maxString = $"P{max}";
        maxUtf8= Encoding.UTF8.GetBytes(maxString);
    }

    [Benchmark]
    public bool Dictionary() => _dictionary.ContainsKey(maxString);

    [Benchmark]
    public bool Buffer() => _buffer.Contains(maxUtf8);

    [Benchmark]
    public bool Sequential() => _sequential.AsSpan(0, _sequentialIndex).IndexOf(maxUtf8) != -1;

    [Benchmark]
    public bool AllocateDictionaryAndAdd()
    {
        Dictionary<string, int> dict = new();
        dict.Add("P1", max);
        return dict.ContainsKey(maxString);
    }

    [Benchmark]
    public bool AllocateBufferAndAdd()
    {
        ExtensionProperties buffer = new();
        buffer.Set("P1"u8, max);
        return buffer.Contains(maxUtf8);
    }
}