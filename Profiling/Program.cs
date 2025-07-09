
using System.ClientModel.Primitives;

ExtensionProperties _buffer = new();
_buffer.Set("P1"u8, 5);

Console.WriteLine("PRESS ENTER");
Console.ReadLine();
int found = 0;
for (long i = 2; i <= 10000000000; i++)
{
    if (_buffer.Contains("P1"u8)) found++;
}

