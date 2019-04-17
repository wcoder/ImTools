# ImTools

[![license](https://img.shields.io/github/license/dadhi/ImTools.svg)](http://opensource.org/licenses/MIT)

- Windows: [![Windows build](https://ci.appveyor.com/api/projects/status/el9echuqfnl86u53?svg=true)](https://ci.appveyor.com/project/MaksimVolkau/imtools/branch/master)
- Linux, MacOS: [![Linux build](https://travis-ci.org/dadhi/ImTools.svg?branch=master)](https://travis-ci.org/dadhi/ImTools)

- Lib package: [![NuGet Badge](https://buildstats.info/nuget/ImTools.dll)](https://www.nuget.org/packages/ImTools.dll)
- Code package: [![NuGet Badge](https://buildstats.info/nuget/ImTools)](https://www.nuget.org/packages/ImTools)

Immutable persistent collections, Ref, and Array helpers designed for performance.

Split from [DryIoc](https://github.com/dadhi/dryioc).


## Benchmarks

The comparison is done against the `ImMap` V1 version and a variety of BCL C# collections including the experimental 
`Microsoft.Collections.Extensions.DictionarySlim<K, V>`.

__Note:__ Keep in mind that immutable collections have a different use-case and a thread-safety guarantees compared to the 
`Dictionary`, `DictionarySlim` or even `ConcurrentDictionary`. The closest comparable would be the `ImmutableDictionary`. 
The benchmarks do not take the collection "nature" into account and run though a simplest available API path.

*Benchmark environment*:
```
BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17134.648 (1803/April2018Update/Redstone4)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
Frequency=2156254 Hz, Resolution=463.7673 ns, Timer=TSC
.NET Core SDK=3.0.100-preview3-010431
  [Host]     : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.2.3 (CoreCLR 4.6.27414.05, CoreFX 4.6.27414.05), 64bit RyuJIT
```


### ImMap with string values

`ImMap<string>` stores the `int` keys and `string` values.

#### ImMap Population

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) inserts from 10 to 100 000 `Count` of items into the `ImMap<string>`, 
where value is `i.ToString()`:

```md
|                    Method |  Count |             Mean |          Error |          StdDev |           Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|-------------------------- |------- |-----------------:|---------------:|----------------:|-----------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|         ImMap_AddOrUpdate |     10 |         677.4 ns |       3.304 ns |       3.0910 ns |         677.0 ns |  1.00 |    0.00 |      0.4435 |           - |           - |             2.05 KB |
|      ImMap_V1_AddOrUpdate |     10 |         810.3 ns |       7.279 ns |       6.8085 ns |         807.9 ns |  1.20 |    0.01 |      0.5455 |           - |           - |             2.52 KB |
| DictSlim_GetOrAddValueRef |     10 |         440.2 ns |       1.045 ns |       0.9773 ns |         440.4 ns |  0.65 |    0.00 |      0.2437 |           - |           - |             1.13 KB |
|               Dict_TryAdd |     10 |         465.7 ns |       8.925 ns |      11.2878 ns |         470.2 ns |  0.68 |    0.02 |      0.2780 |           - |           - |             1.28 KB |
|     ConcurrentDict_TryAdd |     10 |         764.8 ns |       1.658 ns |       1.2941 ns |         765.0 ns |  1.13 |    0.01 |      0.3281 |           - |           - |             1.52 KB |
|         ImmutableDict_Add |     10 |       5,625.4 ns |     106.513 ns |      99.6319 ns |       5,672.8 ns |  8.30 |    0.14 |      0.6256 |           - |           - |             2.89 KB |
|                           |        |                  |                |                 |                  |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate |    100 |      12,501.0 ns |     181.743 ns |     170.0026 ns |      12,517.9 ns |  1.00 |    0.00 |      7.9651 |           - |           - |            36.73 KB |
|      ImMap_V1_AddOrUpdate |    100 |      14,307.7 ns |      43.586 ns |      38.6381 ns |      14,301.3 ns |  1.15 |    0.01 |      9.4147 |           - |           - |            43.39 KB |
| DictSlim_GetOrAddValueRef |    100 |       3,694.1 ns |      71.555 ns |      79.5335 ns |       3,724.1 ns |  0.30 |    0.01 |      1.8311 |      0.0038 |           - |             8.45 KB |
|               Dict_TryAdd |    100 |       4,340.6 ns |      41.170 ns |      36.4957 ns |       4,348.1 ns |  0.35 |    0.01 |      2.8305 |           - |           - |            13.08 KB |
|     ConcurrentDict_TryAdd |    100 |      12,070.4 ns |      35.388 ns |      33.1017 ns |      12,070.1 ns |  0.97 |    0.01 |      4.8828 |      0.0153 |           - |            22.55 KB |
|         ImmutableDict_Add |    100 |     104,824.8 ns |   2,122.120 ns |   5,553.2191 ns |     106,168.5 ns |  7.89 |    0.81 |     10.6201 |           - |           - |            49.09 KB |
|                           |        |                  |                |                 |                  |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate |   1000 |     228,929.6 ns |   4,169.645 ns |   3,900.2882 ns |     230,140.7 ns |  1.00 |    0.00 |    113.0371 |      0.2441 |           - |           521.94 KB |
|      ImMap_V1_AddOrUpdate |   1000 |     260,340.5 ns |   3,842.012 ns |   3,593.8209 ns |     260,879.7 ns |  1.14 |    0.03 |    127.9297 |      0.9766 |           - |           591.73 KB |
| DictSlim_GetOrAddValueRef |   1000 |      38,421.4 ns |     507.728 ns |     474.9295 ns |      38,580.0 ns |  0.17 |    0.00 |     15.5029 |      0.0610 |           - |            71.72 KB |
|               Dict_TryAdd |   1000 |      49,308.8 ns |     693.257 ns |     614.5539 ns |      49,280.4 ns |  0.22 |    0.00 |     28.2593 |      0.0610 |           - |           131.07 KB |
|     ConcurrentDict_TryAdd |   1000 |     144,514.2 ns |   2,808.447 ns |   2,489.6151 ns |     145,501.9 ns |  0.63 |    0.01 |     40.7715 |      9.5215 |           - |           200.83 KB |
|         ImmutableDict_Add |   1000 |   1,510,495.0 ns |  13,517.349 ns |  11,982.7778 ns |   1,509,123.8 ns |  6.61 |    0.10 |    150.3906 |           - |           - |           693.88 KB |
|                           |        |                  |                |                 |                  |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate |  10000 |   5,236,366.2 ns | 103,401.736 ns | 151,564.9236 ns |   5,203,333.7 ns |  1.00 |    0.00 |   1117.1875 |    242.1875 |    109.3750 |          6879.88 KB |
|      ImMap_V1_AddOrUpdate |  10000 |   5,210,495.0 ns | 136,248.688 ns | 293,289.6752 ns |   5,077,546.9 ns |  1.02 |    0.09 |   1234.3750 |    226.5625 |    101.5625 |          7582.30 KB |
| DictSlim_GetOrAddValueRef |  10000 |     481,380.9 ns |   8,822.891 ns |   8,252.9382 ns |     477,200.6 ns |  0.09 |    0.00 |    125.0000 |    124.0234 |    124.0234 |          1023.47 KB |
|               Dict_TryAdd |  10000 |     589,676.6 ns |   7,133.904 ns |   6,673.0578 ns |     586,556.1 ns |  0.11 |    0.00 |    221.6797 |    221.6797 |    221.6797 |          1302.74 KB |
|     ConcurrentDict_TryAdd |  10000 |   3,168,594.9 ns |  32,814.610 ns |  30,694.8067 ns |   3,176,325.8 ns |  0.61 |    0.02 |    289.0625 |    128.9063 |     42.9688 |          1677.33 KB |
|         ImmutableDict_Add |  10000 |  19,435,948.8 ns |  84,745.419 ns |  79,270.9172 ns |  19,425,969.1 ns |  3.76 |    0.12 |   1468.7500 |    281.2500 |    125.0000 |           9124.5 KB |
|                           |        |                  |                |                 |                  |       |         |             |             |             |                     |
|         ImMap_AddOrUpdate | 100000 |  64,378,774.9 ns | 569,934.609 ns | 533,117.1868 ns |  64,337,391.3 ns |  1.00 |    0.00 |  14375.0000 |   2250.0000 |    625.0000 |         84472.50 KB |
|      ImMap_V1_AddOrUpdate | 100000 |  66,387,743.3 ns | 317,490.210 ns | 281,446.8012 ns |  66,266,405.8 ns |  1.03 |    0.01 |  15375.0000 |   2000.0000 |    500.0000 |         91502.90 KB |
| DictSlim_GetOrAddValueRef | 100000 |  10,592,191.3 ns |  85,596.015 ns |  71,476.5493 ns |  10,583,706.9 ns |  0.16 |    0.00 |   1234.3750 |    968.7500 |    734.3750 |          9019.38 KB |
|               Dict_TryAdd | 100000 |  10,953,115.5 ns | 135,428.893 ns | 126,680.2701 ns |  10,989,448.3 ns |  0.17 |    0.00 |   1125.0000 |    812.5000 |    609.3750 |         12152.85 KB |
|     ConcurrentDict_TryAdd | 100000 |  35,298,247.4 ns | 634,210.234 ns | 562,210.8529 ns |  35,136,630.4 ns |  0.55 |    0.01 |   2625.0000 |   1250.0000 |    500.0000 |         15486.84 KB |
|         ImmutableDict_Add | 100000 | 247,218,695.0 ns | 755,870.150 ns | 707,041.4069 ns | 247,280,231.9 ns |  3.84 |    0.03 |  19000.0000 |   2666.6667 |    666.6667 |        112113.42 KB |

```

### ImMap Lookup

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImMapBenchmarks.cs) lookups for **the last index (key)** in the `ImMap<string>` 
of specified `Count` of elements.

```md
|                     Method |  Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------- |------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|              ImMap_TryFind |     10 |  2.994 ns | 0.0113 ns | 0.0106 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind |     10 |  5.566 ns | 0.0354 ns | 0.0331 ns |  1.86 |    0.01 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue |     10 |  4.112 ns | 0.0155 ns | 0.0138 ns |  1.37 |    0.01 |           - |           - |           - |                   - |
|           Dict_TryGetValue |     10 |  7.188 ns | 0.0317 ns | 0.0281 ns |  2.40 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue |     10 | 10.392 ns | 0.0217 ns | 0.0203 ns |  3.47 |    0.01 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue |     10 | 59.361 ns | 0.2914 ns | 0.2725 ns | 19.83 |    0.11 |           - |           - |           - |                   - |
|                            |        |           |           |           |       |         |             |             |             |                     |
|              ImMap_TryFind |    100 |  4.377 ns | 0.0283 ns | 0.0251 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind |    100 |  6.716 ns | 0.0228 ns | 0.0214 ns |  1.53 |    0.01 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue |    100 |  3.374 ns | 0.0121 ns | 0.0113 ns |  0.77 |    0.01 |           - |           - |           - |                   - |
|           Dict_TryGetValue |    100 |  7.213 ns | 0.0472 ns | 0.0442 ns |  1.65 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue |    100 | 10.453 ns | 0.1343 ns | 0.1257 ns |  2.39 |    0.03 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue |    100 | 69.985 ns | 1.4111 ns | 1.3859 ns | 15.96 |    0.29 |           - |           - |           - |                   - |
|                            |        |           |           |           |       |         |             |             |             |                     |
|              ImMap_TryFind |   1000 |  7.160 ns | 0.0865 ns | 0.0809 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind |   1000 |  8.160 ns | 0.0399 ns | 0.0373 ns |  1.14 |    0.01 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue |   1000 |  3.375 ns | 0.0125 ns | 0.0111 ns |  0.47 |    0.01 |           - |           - |           - |                   - |
|           Dict_TryGetValue |   1000 |  7.165 ns | 0.0426 ns | 0.0398 ns |  1.00 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue |   1000 | 10.614 ns | 0.1033 ns | 0.0966 ns |  1.48 |    0.02 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue |   1000 | 72.254 ns | 1.2773 ns | 1.1948 ns | 10.09 |    0.20 |           - |           - |           - |                   - |
|                            |        |           |           |           |       |         |             |             |             |                     |
|              ImMap_TryFind |  10000 | 13.560 ns | 0.1585 ns | 0.1405 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind |  10000 | 15.908 ns | 0.1155 ns | 0.1080 ns |  1.17 |    0.01 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue |  10000 |  3.930 ns | 0.0123 ns | 0.0115 ns |  0.29 |    0.00 |           - |           - |           - |                   - |
|           Dict_TryGetValue |  10000 |  7.175 ns | 0.0382 ns | 0.0357 ns |  0.53 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue |  10000 | 11.132 ns | 0.0250 ns | 0.0234 ns |  0.82 |    0.01 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue |  10000 | 85.523 ns | 0.3271 ns | 0.3059 ns |  6.31 |    0.07 |           - |           - |           - |                   - |
|                            |        |           |           |           |       |         |             |             |             |                     |
|              ImMap_TryFind | 100000 | 15.374 ns | 0.0290 ns | 0.0271 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|           ImMap_V1_TryFind | 100000 | 18.809 ns | 0.0399 ns | 0.0333 ns |  1.22 |    0.00 |           - |           - |           - |                   - |
|       DictSlim_TryGetValue | 100000 |  3.364 ns | 0.0088 ns | 0.0078 ns |  0.22 |    0.00 |           - |           - |           - |                   - |
|           Dict_TryGetValue | 100000 |  7.204 ns | 0.0279 ns | 0.0247 ns |  0.47 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDict_TryGetValue | 100000 | 10.578 ns | 0.0549 ns | 0.0514 ns |  0.69 |    0.00 |           - |           - |           - |                   - |
|  ImmutableDict_TryGetValue | 100000 | 92.043 ns | 0.3051 ns | 0.2854 ns |  5.99 |    0.02 |           - |           - |           - |                   - |

```

**Interpreting results:** `ImMap` holds very good against it `ImmutableDcitionary` sibling and even against `Dictionary`(s) up to certain count, 
indicating that immutable collection could be quite fast.

### ImHashMap of string keys and string values

#### ImHashMap Population

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks_StringString.cs) inserts from 10 to 100 000 `Count` of items into the `ImHashMap<string, string>`, 
where the key is `i + "hubba-bubba" + i` and the value is `i + "hubba-bubba"`:

```md
|                     Method |  Count |           Mean |         Error |        StdDev | Ratio | RatioSD |      Gen 0 |     Gen 1 |    Gen 2 |    Allocated |
|--------------------------- |------- |---------------:|--------------:|--------------:|------:|--------:|-----------:|----------:|---------:|-------------:|
|          ImMap_AddOrUpdate |     10 |       1.185 us |     0.0049 us |     0.0045 us |  1.00 |    0.00 |     0.7420 |         - |        - |      3.42 KB |
|       ImMap_V1_AddOrUpdate |     10 |       1.203 us |     0.0022 us |     0.0021 us |  1.01 |    0.00 |     0.7610 |         - |        - |      3.52 KB |
|  DictSlim_GetOrAddValueRef |     10 |       1.170 us |     0.0033 us |     0.0029 us |  0.99 |    0.00 |     0.5322 |         - |        - |      2.45 KB |
|                Dict_TryAdd |     10 |       1.048 us |     0.0162 us |     0.0152 us |  0.88 |    0.01 |     0.5150 |         - |        - |      2.38 KB |
|      ConcurrentDict_TryAdd |     10 |       1.529 us |     0.0085 us |     0.0075 us |  1.29 |    0.01 |     0.5817 |         - |        - |      2.69 KB |
|          ImmutableDict_Add |     10 |       5.764 us |     0.1096 us |     0.1076 us |  4.87 |    0.09 |     0.8087 |         - |        - |      3.73 KB |
|                            |        |                |               |               |       |         |            |           |          |              |
|          ImMap_AddOrUpdate |    100 |      19.693 us |     0.0921 us |     0.0862 us |  1.00 |    0.00 |    10.8337 |    0.0305 |        - |     50.02 KB |
|       ImMap_V1_AddOrUpdate |    100 |      20.991 us |     0.0365 us |     0.0341 us |  1.07 |    0.00 |    11.8713 |    0.0305 |        - |     54.84 KB |
|  DictSlim_GetOrAddValueRef |    100 |      11.067 us |     0.0980 us |     0.0869 us |  0.56 |    0.01 |     4.6234 |         - |        - |     21.38 KB |
|                Dict_TryAdd |    100 |      10.396 us |     0.1406 us |     0.1316 us |  0.53 |    0.01 |     5.2032 |         - |        - |     24.02 KB |
|      ConcurrentDict_TryAdd |    100 |      27.156 us |     0.4384 us |     0.4101 us |  1.38 |    0.02 |     9.6436 |    0.0610 |        - |     44.47 KB |
|          ImmutableDict_Add |    100 |     102.030 us |     2.1747 us |     2.7503 us |  5.20 |    0.16 |    12.4512 |         - |        - |     57.72 KB |
|                            |        |                |               |               |       |         |            |           |          |              |
|          ImMap_AddOrUpdate |   1000 |     366.492 us |     5.3528 us |     5.0070 us |  1.00 |    0.00 |   138.1836 |   33.2031 |        - |    661.45 KB |
|       ImMap_V1_AddOrUpdate |   1000 |     411.711 us |     3.3075 us |     2.9320 us |  1.12 |    0.01 |   127.9297 |   58.5938 |        - |    701.77 KB |
|  DictSlim_GetOrAddValueRef |   1000 |     135.818 us |     1.3496 us |     1.2624 us |  0.37 |    0.01 |    39.0625 |   15.3809 |        - |    204.11 KB |
|                Dict_TryAdd |   1000 |     114.091 us |     1.9093 us |     1.7860 us |  0.31 |    0.01 |    46.2646 |   15.5029 |        - |    247.48 KB |
|      ConcurrentDict_TryAdd |   1000 |     360.638 us |     6.3500 us |     5.9398 us |  0.98 |    0.02 |    77.1484 |   38.5742 |        - |    449.71 KB |
|          ImmutableDict_Add |   1000 |   1,578.688 us |    18.5404 us |    17.3427 us |  4.31 |    0.07 |   156.2500 |   60.5469 |        - |    791.91 KB |
|                            |        |                |               |               |       |         |            |           |          |              |
|          ImMap_AddOrUpdate |  10000 |  10,265.260 us |    92.1719 us |    86.2177 us |  1.00 |    0.00 |  1359.3750 |  359.3750 | 156.2500 |   8277.42 KB |
|       ImMap_V1_AddOrUpdate |  10000 |  11,061.120 us |   169.4658 us |   158.5185 us |  1.08 |    0.02 |  1421.8750 |  421.8750 | 187.5000 |   8700.75 KB |
|  DictSlim_GetOrAddValueRef |  10000 |   2,596.471 us |    10.3734 us |     9.1957 us |  0.25 |    0.00 |   398.4375 |  199.2188 | 199.2188 |   2450.55 KB |
|                Dict_TryAdd |  10000 |   2,380.701 us |    13.8106 us |    11.5325 us |  0.23 |    0.00 |   441.4063 |  218.7500 | 218.7500 |   2473.84 KB |
|      ConcurrentDict_TryAdd |  10000 |   7,215.799 us |   127.0207 us |   118.8153 us |  0.70 |    0.01 |   539.0625 |  296.8750 | 132.8125 |   3247.21 KB |
|          ImmutableDict_Add |  10000 |  25,002.045 us |   225.6550 us |   200.0373 us |  2.43 |    0.03 |  1656.2500 |  375.0000 | 125.0000 |  10112.72 KB |
|                            |        |                |               |               |       |         |            |           |          |              |
|          ImMap_AddOrUpdate | 100000 | 209,350.633 us | 2,393.2964 us | 2,238.6909 us |  1.00 |    0.00 | 17000.0000 | 4666.6667 | 666.6667 | 100333.97 KB |
|       ImMap_V1_AddOrUpdate | 100000 | 223,663.675 us | 2,464.5772 us | 2,184.7835 us |  1.07 |    0.02 | 17666.6667 | 4666.6667 | 666.6667 | 104491.82 KB |
|  DictSlim_GetOrAddValueRef | 100000 |  55,598.480 us | 1,101.7717 us | 2,122.7400 us |  0.27 |    0.01 |  3333.3333 | 1444.4444 | 666.6667 |  24191.45 KB |
|                Dict_TryAdd | 100000 |  54,494.452 us | 1,067.3681 us | 1,270.6258 us |  0.26 |    0.01 |  3375.0000 | 1562.5000 | 687.5000 |  25276.91 KB |
|      ConcurrentDict_TryAdd | 100000 | 100,291.074 us | 1,430.4632 us | 1,268.0684 us |  0.48 |    0.01 |  4600.0000 | 2000.0000 | 600.0000 |  27459.84 KB |
|          ImmutableDict_Add | 100000 | 375,614.567 us | 1,515.8877 us | 1,343.7950 us |  1.79 |    0.02 | 20000.0000 | 5000.0000 |        - | 123738.77 KB |
```

### ImHashMap Lookup

[The benchmark](https://github.com/dadhi/ImTools/blob/master/playground/ImTools.Benchmarks/ImHashMapBenchmarks_StringString.cs) lookups for **the last added key** in the `ImHashMap<string, string>` of specified `Count` of elements.

```md
|                           Method |  Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------------- |------- |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|                ImHashMap_TryFind |     10 |  14.97 ns | 0.0286 ns | 0.0254 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind |     10 |  17.09 ns | 0.0262 ns | 0.0218 ns |  1.14 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |     10 |  18.88 ns | 0.0186 ns | 0.0155 ns |  1.26 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |     10 |  23.47 ns | 0.0699 ns | 0.0654 ns |  1.57 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |     10 |  31.24 ns | 0.0501 ns | 0.0469 ns |  2.09 |    0.00 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |     10 |  85.26 ns | 1.7318 ns | 2.2518 ns |  5.64 |    0.18 |           - |           - |           - |                   - |
|                                  |        |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |    100 |  18.38 ns | 0.0345 ns | 0.0323 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind |    100 |  19.95 ns | 0.0380 ns | 0.0355 ns |  1.09 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |    100 |  20.12 ns | 0.1021 ns | 0.0955 ns |  1.09 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |    100 |  24.50 ns | 0.3207 ns | 0.3000 ns |  1.33 |    0.02 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |    100 |  32.26 ns | 0.4910 ns | 0.4100 ns |  1.76 |    0.02 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |    100 |  87.50 ns | 0.3134 ns | 0.2931 ns |  4.76 |    0.02 |           - |           - |           - |                   - |
|                                  |        |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |   1000 |  22.61 ns | 0.0386 ns | 0.0361 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind |   1000 |  27.44 ns | 0.0962 ns | 0.0900 ns |  1.21 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |   1000 |  20.99 ns | 0.0756 ns | 0.0708 ns |  0.93 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |   1000 |  26.08 ns | 0.0482 ns | 0.0450 ns |  1.15 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |   1000 |  33.84 ns | 0.2134 ns | 0.1996 ns |  1.50 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |   1000 | 103.94 ns | 0.8352 ns | 0.7813 ns |  4.60 |    0.03 |           - |           - |           - |                   - |
|                                  |        |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |  10000 |  27.13 ns | 0.1008 ns | 0.0943 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind |  10000 |  32.64 ns | 0.1863 ns | 0.1652 ns |  1.20 |    0.01 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |  10000 |  22.05 ns | 0.0508 ns | 0.0451 ns |  0.81 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |  10000 |  27.16 ns | 0.1111 ns | 0.1039 ns |  1.00 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |  10000 |  33.97 ns | 0.1325 ns | 0.1175 ns |  1.25 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |  10000 | 110.48 ns | 0.2626 ns | 0.2456 ns |  4.07 |    0.02 |           - |           - |           - |                   - |
|                                  |        |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind | 100000 |  32.34 ns | 0.1657 ns | 0.1550 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_V1_TryFind | 100000 |  39.72 ns | 0.2306 ns | 0.2157 ns |  1.23 |    0.01 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue | 100000 |  23.23 ns | 0.0179 ns | 0.0149 ns |  0.72 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue | 100000 |  28.32 ns | 0.0485 ns | 0.0453 ns |  0.88 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue | 100000 |  36.86 ns | 0.1183 ns | 0.1049 ns |  1.14 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet | 100000 | 116.50 ns | 0.1618 ns | 0.1351 ns |  3.61 |    0.02 |           - |           - |           - |                   - |

```



## End-to-end Example

Let's assume you are implementing yet another DI container because why not :-)

Container should contain registry of `Type` to `Factory` mappings. 
On resolution `Factory` is compiled to the delegate which you would like to cache, because compilation is costly. 
The cache will store the mappings from `Type` to `Func<object>`.

__The requirements:__

- The container may be used in parallel from different threads including registrations and resolutions. 
- The container state should not be corrupted and the cache should correspond to the current state of registrations.

Let's design the basic container structure to support the requirements and __without locking__:

```cs
    public class Container
    {
        private readonly Ref<Registry> _registry = Ref.Of(new Registry());

        public void Register<TService, TImpl>() where TImpl : TService, new()
        {
            _registry.Swap(reg => reg.With(typeof(TService), new Factory(typeof(TImpl))));
        }

        public object Resolve<TService>()
        {
            return (TService)(_registry.Value.Resolve(typeof(TService)) ?? ThrowUnableToResolve(typeof(TService)));
        }
        
        public object ThrowUnableToResolve(Type t) { throw new InvalidOperationException("Unable to resolve: " + t); }

        class Registry 
        {
            ImHashMap<Type, Factory> _registrations = ImHashMap<Type, Factory>.Empty;
            Ref<ImHashMap<Type, Func<object>>> _resolutionCache = Ref.Of(ImHashMap<Type, Func<object>>.Empty);

            // Creating a new registry with +1 registration and new reference to cache value
            public Registry With(Type serviceType, Factory implFactory)
            {
                return new Registry() 
                {	
                    _registrations = _registrations.AddOrUpdate(serviceType, implFactory),
                        
                    // Here is most interesting part:
                    // We are creating new independent reference pointing to cache value,
                    // isolating it from possible parallel resolutions, 
                    // which will swap older version/ref of cache and won't touch the new one.
                    _resolutionCache = Ref.Of(_resolutionCache.Value)
                };
            }

            public object Resolve(Type serviceType)
            {
                var func = _resolutionCache.Value.GetValueOrDefault(serviceType);
                if (func != null)
                    return func();

                var reg = _registrations.GetValueOrDefault(serviceType);
                if (reg == null)
                    return null;
                
                func = reg.CompileDelegate();
                _resolutionCache.Swap(cache => cache.AddOrUpdate(serviceType, func));
                return func.Invoke();
            }
        }
        
        class Factory 
        {
            public readonly Type ImplType;
            public Factory(Type implType) { ImplType = implType; }
            public Func<object> CompileDelegate() { return () => Activator.CreateInstance(ImplType); }
        } 
    }
```
