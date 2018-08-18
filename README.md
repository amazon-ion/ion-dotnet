# IonDotnet
Amazon Ion ( http://amzn.github.io/ion-docs/ ) library for dotnet 
### Note 
This project is still in early development and not ready for production use.

[Basic usage](https://github.com/dhhoang/IonDotnet/blob/master/README.md#basic-usage)  
[Performance](https://github.com/dhhoang/IonDotnet/blob/master/README.md#benchmarks)

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

### Benchmarks
#### Serialization: compared to JSON.NET
Test: serialize 1000 records
```
BenchmarkDotNet=v0.11.0, OS=Windows 10.0.17134.191 (1803/April2018Update/Redstone4)
Intel Core i7-6700HQ CPU 2.60GHz (Max: 2.59GHz) (Skylake), 1 CPU, 8 logical and 4 physical cores
Frequency=2531251 Hz, Resolution=395.0616 ns, Timer=TSC
.NET Core SDK=2.1.302
  [Host]     : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.2 (CoreCLR 4.6.26628.05, CoreFX 4.6.26629.01), 64bit RyuJIT
  
                  Method |     Mean |     Error |    StdDev |    Gen 0 |    Gen 1 |    Gen 2 |  Allocated |
------------------------ |---------:|----------:|----------:|---------:|---------:|---------:|-----------:|
              JSONDotnet | 3.198 ms | 0.0291 ms | 0.0272 ms | 371.0938 | 371.0938 | 371.0938 | 1575.56 KB |
 IonReflectionSerializer | 5.406 ms | 0.1076 ms | 0.1057 ms |  54.6875 |  15.6250 |        - |  522.33 KB |
        IonExpSerializer | 2.731 ms | 0.0287 ms | 0.0269 ms |  35.1563 |  11.7188 |        - |  412.95 KB |
         IonDotnetManual | 2.288 ms | 0.0322 ms | 0.0302 ms |  31.2500 |  11.7188 |        - |  383.37 KB |
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
