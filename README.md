## Ion .NET

[![Build Status](https://travis-ci.com/amzn/ion-dotnet.svg?branch=master)](https://travis-ci.com/amzn/ion-dotnet)
[![nuget version](https://img.shields.io/nuget/v/Amazon.IonDotnet)](https://www.nuget.org/packages/Amazon.IonDotnet)

Amazon Ion ( http://amzn.github.io/ion-docs/ ) library for .NET

This package is based on work from Huy Hoang ([dhhoang](https://github.com/dhhoang)) on https://github.com/dhhoang/IonDotnet. The Ion team greatly appreciates Huy's contributions to the Ion community.

### Manual read/write

You can create a `reader` that can read from a (input) stream. You can specify text encoding in the `ReaderOptions`, otherwise Utf8 will be used by default. There are two different `reader`s that you can create, binary and text. Four `reader` objects are created below with different inputs. 
```csharp
Stream stream;  //input stream
String text;    //text form
ICatalog catalog;
IIonReader reader;

//create a text reader that automatically detect whether the stream is text/binary
reader = IonReaderBuilder.Build(stream);

//explicitly create a text reader
reader = IonReaderBuilder.Build(stream, new ReaderOptions {Format = ReaderFormat.Text});

//explicitly create a binary reader
reader = IonReaderBuilder.Build(stream, new ReaderOptions {Format = ReaderFormat.Binary});

//create a text reader that reads a string and uses a catalog
reader = IonReaderBuilder.Build(text, new ReaderOptions {Catalog = catalog});
```

Example  of using a  `reader`.
```csharp
/*reader semantics for
{
    number: 1,
    text: "hello world"
}
*/

using (IIonReader reader = IonReaderBuilder.Build(stream))
{
    Console.WriteLine(reader.MoveNext()); // Struct
    reader.StepIn();
    Console.WriteLine(reader.MoveNext()); // Int
    Console.WriteLine(reader.CurrentFieldName); // "number"
    Console.WriteLine(reader.IntValue());   // 1
    Console.WriteLine(reader.MoveNext()); // String
    Console.WriteLine(reader.CurrentFieldName); // "text"
    Console.WriteLine(reader.StringValue());   // "hello world"
    reader.StepOut();
}
```

Similarly you can create a writer that write to a (output) stream. There are two different `writer`s that you can create, binary and text. Three `writer` objects are created below with different inputs. 
```csharp
Stream stream; //output stream
var stringWriter = new StringWriter();
IIonWriter writer;
ISymbolTable table1,table2;


//create a text writer that write to the stream.
writer = IonTextWriterBuilder.Build(new StreamWriter(stream));

//create a text writer that write to a stringwriter/builder.
writer = IonTextWriterBuilder.Build(stringWriter);

//create a binary writer using multiple symbol tables
writer = IonBinaryWriterBuilder.Build(stream, new []{table1,table2});

```

Example  of using a  `writer`.
```csharp
/*writer semantics for
{
    number: 1,
    text: "hello world"
}
*/

using (IIonWriter writer = IonTextWriterBuilder.Build(new StreamWriter(stream)))
{
    writer.StepIn(IonType.Struct);
    writer.SetFieldName("number");
    writer.WriteInt(1);
    writer.SetFieldName("text");
    writer.WriteString("hello world");
    writer.StepOut();
    writer.Finish();    //this is important
}
```

### Serialization
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
    public int[] Outputs { get; set; }
}

var experiment = new Experiment
{
    Id = 233,
    Name = "Measure performance impact of boxing",
    StartDate = new DateTimeOffset(2018, 07, 21, 11, 11, 11, TimeSpan.Zero),
    IsActive = true,
    Result = ExperimentResult.Failure,
    SampleData = new byte[10],
    Budget = decimal.Parse("12345.01234567890123456789"),
    Outputs = new[] {1, 2, 3}
};

//Serialize an object to byte array
byte[] ionBytes = IonSerialization.Binary.Serialize(experiment);

//Deserialize a byte array to an object
Experiment deserialized = IonSerialization.Binary.Deserialize<Experiment>(ionBytes);

//Serialize an object to string
string text = IonSerialization.Text.Serialize(experiment, new IonTextOptions {PrettyPrint = true});

//Deserialize a string to an object
deserialized = IonSerialization.Text.Deserialize<Experiment>(text);

Console.WriteLine(text);
/* Output
{
  Id: 233,
  Name: "Boxing Perftest",
  Description: "Measure performance impact of boxing",
  StartDate: 2018-07-21T11:11:11.0000000+00:00,
  IsActive: true,
  SampleData: {{ AAAAAAAAAAAAAA== }},
  Budget: 12345.01234567890123456789,
  Result: 'Failure',
  Outputs: [
    1.2e0,
    2.3e0,
    3.1e0
  ]
}
*/
```

### Setup
This repository contains a git submodule called ion-tests, which holds test data used by ion-dotnet's unit tests.
Clone the whole repository and initialize the submodule by:
```
$ git clone --recurse-submodules git@github.com:amzn/ion-dotnet.git
```
Or you can initilize the submodules in the clone
```
$ git submodule init
$ git submodule update
```
The project currently uses default dotnet [CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/?tabs=netcore2x) tools,
you can build the project simply by:
```
$ dotnet build
```

## License

This library is licensed under the MIT License.
