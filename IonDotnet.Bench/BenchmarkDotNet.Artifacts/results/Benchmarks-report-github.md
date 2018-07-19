``` ini

BenchmarkDotNet=v0.10.14, OS=Windows 10.0.17134
Intel Core i7-6700HQ CPU 2.60GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
Frequency=2531253 Hz, Resolution=395.0613 ns, Timer=TSC
.NET Core SDK=2.1.301
  [Host]     : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.1 (CoreCLR 4.6.26606.02, CoreFX 4.6.26606.05), 64bit RyuJIT


```
|             Method |      Mean |     Error |    StdDev |  Gen 0 | Allocated |
|------------------- |----------:|----------:|----------:|-------:|----------:|
| GetByteCountDecode | 100.74 ns | 1.4563 ns | 1.2161 ns | 0.0483 |     152 B |
|   GetByteCountSpan |  26.34 ns | 0.1708 ns | 0.1597 ns |      - |       0 B |
