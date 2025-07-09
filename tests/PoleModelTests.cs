using NUnit.Framework;

namespace AdditionalProperties;

public class PoleModelTests
{
    [Test]
    public void Basics()
    {
        PoleModel model = new();
        model.Name = "test";
    }
}
