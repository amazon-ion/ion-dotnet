# IonDotnet
Amazon Ion ( http://amzn.github.io/ion-docs/ ) library for dotnet 
### Note 
This project is still in early development and not ready for production use.

### Basic usage
IonDotnet can (de)serialize any POCO object 

```csharp
private class Experiment
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public bool IsActive { get; set; }
    public byte[] SampleData { get; set; }
    public decimal Budget { get; set; }
    public ExperimentResult Result { get; set; }
}

var experiment = new Experiment
{
    Id = 233,
    Name = "Measure performance impact of boxing",
    StartDate = new DateTimeOffset(2018, 07, 21, 11, 11, 11, TimeSpan.Zero),
    IsActive = true,
    Result = ExperimentResult.Failure,
    SampleData = new byte[100],
    Budget = decimal.Parse("12345.01234567890123456789")
};

//Serialize an object to byte array
byte[] ionBytes = IonSerialization.Serialize(experiment, converter);

//Deserialize a byte array to an object
Experiment deserialized = IonSerialization.Deserialize<Experiment>(ionBytes, converter);
```
