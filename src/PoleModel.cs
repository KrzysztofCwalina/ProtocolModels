using System.ClientModel.Primitives;

public class PoleModel 
{
    RecordStore _extensions = new();

    public string Name
    {
        get => _extensions.GetString("name"u8);
        set => _extensions.Set("name"u8, value);
    }

    public void Write(Stream stream)
    {
        throw new NotImplementedException();
    }
    public static PoleModel Read(Stream stream)
    {
        throw new NotImplementedException();
    }
}

