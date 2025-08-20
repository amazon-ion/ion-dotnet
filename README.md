## Ion .NET (Deprecated)

[![Build Status](https://github.com/amzn/ion-dotnet/workflows/Ion%20DotNet%20CI/badge.svg)](https://github.com/amzn/ion-dotnet/actions?query=workflow%3A%22Ion+DotNet+CI%22)
[![nuget version](https://img.shields.io/nuget/v/Amazon.IonDotnet)](https://www.nuget.org/packages/Amazon.IonDotnet)

As of 8/20/2024, this library is deprecated and will not receive further updates. Users should consider migrating to [other Ion solutions](https://amazon-ion.github.io/ion-docs/libs.html).

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
