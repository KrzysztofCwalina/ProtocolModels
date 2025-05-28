// convinience APIs

SomeClient client = new();
ConvenienceInputModel inputModel = new()
{
    Id = "number facts",
    Numbers = new List<NamedNumber> { 
        new NamedNumber("my first building", 42),
        new NamedNumber("PI", 3) 
    }
};
ConvenienceOutputModel convenienceOutput = client.DoSomething(inputModel);
inputModel.Json.Set("temperature"u8, "90"u8);

// typed protocol 
SomeClientOptions options = new()
{
    Endpoint = new Uri("https://example.com"),
    ModelReaderWriterProxies = [
        new Skip(typeof(InputModel), nameof(InputModel.Category)),
        new Skip(typeof(OutputModel), nameof(OutputModel.Confidence))
    ]
};
SomeClient modified = new(options);
InputModel protocolInput = client.CreateProtocolInputModel(inputModel);
protocolInput["temperature"] = 90;
OutputModel protocolOutput = client.DoSomething(inputModel.Id, protocolInput);
var temperature = (int)protocolOutput["temperature"]; // todo: how do we know the type?

