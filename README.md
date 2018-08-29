# IonDotnet
Amazon Ion ( http://amzn.github.io/ion-docs/ ) library for dotnet 

[![Build Status](https://travis-ci.org/dhhoang/IonDotnet.svg?branch=master)](https://travis-ci.org/dhhoang/IonDotnet)

### Note 
This project is still in early development and not ready for production use.

[Basic usage](https://github.com/dhhoang/IonDotnet/blob/master/README.md#basic-usage)  
[Benchmarks](https://github.com/dhhoang/IonDotnet/blob/master/README.md#benchmarks)

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
        1,
        2,
        3
    ]
}
*/
```

### Setup
This repository contains a git submodule called ion-tests, which holds test data used by ion-java's unit tests.  
Clone the whole repository and initialize the submodule by:
```
$ git clone --recurse-submodules git@github.com:dhhoang/IonDotnet.git
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
### Benchmarks
Environment
```
BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.191 (1803/April2018Update/Redstone4)
Intel Core i7-6700HQ CPU 2.60GHz (Max: 2.59GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
Frequency=2531251 Hz, Resolution=395.0616 ns, Timer=TSC
.NET Core SDK=2.1.302
  [Host]     : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
```

#### Serialization: Text vs JSON.NET
Test: Serialize 1000 record
```
           Method |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
----------------- |---------:|----------:|----------:|---------:|---------:|---------:|----------:|
    IonDotnetText | 2.030 ms | 0.0378 ms | 0.0388 ms | 417.9688 | 312.5000 | 183.5938 |   1.91 MB |
 JsonDotnetString | 2.611 ms | 0.0488 ms | 0.0433 ms | 351.5625 | 277.3438 | 167.9688 |   1.58 MB |
```

#### Serialization: Binary vs JSON.NET
Test: serialize 1000 records
```
             Method |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
------------------- |---------:|----------:|----------:|---------:|---------:|---------:|-----------:|
    JsonDotnetBytes | 2.807 ms | 0.0520 ms | 0.0486 ms | 433.5938 | 347.6563 | 347.6563 | 1594.46 KB |
 IonDotnetExpBinary | 2.576 ms | 0.0507 ms | 0.0660 ms |  58.5938 |  58.5938 |  58.5938 |  382.47 KB |
```

#### Compared to Java implementation
Test 1000 serializations of 1000 records. Currently the following code/output is used to reference ion-java speed (probably not fair)
```sh
# sample output
ion-java: writing took 707.86935ms
IonDotnet: writing took  401.4152ms
```
```java
// JAVA code
public class MyBenchmark {
    private static final IonBinaryWriterBuilder builder = IonBinaryWriterBuilder.standard();

    public static void main(String[] args) throws IOException {
        // warm up
        System.nanoTime();
        runOnce();

        long start = System.nanoTime();
        for (int i = 0; i < 1000; i++) {
            runOnce();
        }
        long end = System.nanoTime();
        double usec = (end - start) * 1.0 / 1000000;
        System.out.println("ion-java: writing took " + usec + "ms");
    }

    private static void runOnce() throws IOException {
        ByteArrayOutputStream out = new ByteArrayOutputStream();
        IonWriter writer = builder.build(out);
        writer.stepIn(IonType.LIST);

        for (int i = 0; i < 1000; i++) {
            writer.stepIn(IonType.STRUCT);

            writer.setFieldName("boolean");
            writer.writeBool(true);
            writer.setFieldName("string");
            writer.writeString("this is a string");
            writer.setFieldName("integer");
            writer.writeInt(Integer.MAX_VALUE);
            writer.setFieldName("float");
            writer.writeFloat(432.23123f);
            writer.setFieldName("timestamp");
            writer.writeTimestamp(Timestamp.forDay(2000, 11, 11));

            writer.stepOut();
        }

        writer.stepOut();
        writer.finish();
    }
}
```
```csharp
// C# code
public void Run(string[] args)
{
    //warmup
    var sw = new Stopwatch();
    sw.Start();
    sw.Stop();
    RunOnce();

    sw.Start();

    for (var i = 0; i < 1000; i++)
    {
        RunOnce();
    }

    sw.Stop();
    Console.WriteLine($"IonDotnet: writing took {sw.ElapsedTicks * 1.0 / TimeSpan.TicksPerMillisecond}ms");
}


private static void RunOnce()
{
    byte[] bytes = null;
    Benchmark.Writer.StepIn(IonType.List);

    for (var i = 0; i < 1000; i++)
    {
        Benchmark.Writer.StepIn(IonType.Struct);

        Benchmark.Writer.SetFieldName("boolean");
        Benchmark.Writer.WriteBool(true);
        Benchmark.Writer.SetFieldName("string");
        Benchmark.Writer.WriteString("this is a string");
        Benchmark.Writer.SetFieldName("integer");
        Benchmark.Writer.WriteInt(int.MaxValue);
        Benchmark.Writer.SetFieldName("float");
        Benchmark.Writer.WriteFloat(432.23123f);
        Benchmark.Writer.SetFieldName("timestamp");
        Benchmark.Writer.WriteTimestamp(new Timestamp(new DateTime(2000, 11, 11)));

        Benchmark.Writer.StepOut();
    }

    Benchmark.Writer.StepOut();
    Benchmark.Writer.Flush(ref bytes);
    Benchmark.Writer.Finish();
}
```
