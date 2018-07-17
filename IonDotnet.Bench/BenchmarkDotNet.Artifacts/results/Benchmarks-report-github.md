``` ini

BenchmarkDotNet=v0.10.14, OS=macOS Sierra 10.12.6 (16G1510) [Darwin 16.7.0]
Intel Core i5-7360U CPU 2.30GHz (Kaby Lake), 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=2.1.301
  [Host]     : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT


```
|        Method |     Mean |    Error |    StdDev |    Gen 0 | Allocated |
|-------------- |---------:|---------:|----------:|---------:|----------:|
| ReadStringOld | 310.6 us | 5.958 us |  5.852 us | 109.8633 | 225.48 KB |
| ReadStringNew | 293.8 us | 5.793 us | 11.298 us |  78.6133 | 162.28 KB |
