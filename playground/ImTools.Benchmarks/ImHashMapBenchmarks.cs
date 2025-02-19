using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using BenchmarkDotNet.Attributes;

using Microsoft.Collections.Extensions;
using ImTools;
using ImToolsV3;
using ImTools.V2;
using ImTools.V2.Experimental;
using System.Runtime.CompilerServices;

#pragma warning disable CS0649, CS0169

namespace Playground
{
    public class ImHashMapBenchmarks
    {
        private static readonly Type[] _keys = typeof(Dictionary<,>).Assembly.GetTypes().Take(1000).ToArray();

        public struct TypeVal : IEquatable<TypeVal>
        {
            public static implicit operator TypeVal(Type t) => new TypeVal(t);

            public readonly Type Type;
            public TypeVal(Type type) => Type = type;
            public bool Equals(TypeVal other) => Type == other.Type;
            public override bool Equals(object obj) => !ReferenceEquals(null, obj) && obj is TypeVal other && Equals(other);
            public override int GetHashCode() => Type.GetHashCode();
        }

        [MemoryDiagnoser]
        public class Populate
        {
            /*
            ## 15.01.2019:

                     Method |     Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
            --------------- |---------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                AddOrUpdate | 15.84 us | 0.1065 us | 0.0944 us |  1.00 |    0.00 |      7.3242 |           - |           - |            33.87 KB |
             AddOrUpdate_v1 | 27.00 us | 0.1792 us | 0.1588 us |  1.71 |    0.02 |      7.7515 |           - |           - |            35.77 KB |

            
            ## 16.01.2019: Total test against ImHashMap V1, System ImmutableDictionary and ConcurrentDictionary

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17134.523 (1803/April2018Update/Redstone4)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
Frequency=2156249 Hz, Resolution=463.7683 ns, Timer=TSC
.NET Core SDK=2.2.100
  [Host]     : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.6 (CoreCLR 4.6.27019.06, CoreFX 4.6.27019.05), 64bit RyuJIT


         Method | Count |           Mean |         Error |        StdDev |         Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |---------------:|--------------:|--------------:|---------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |    10 |       987.6 ns |      36.53 ns |      99.99 ns |       934.3 ns |  0.94 |    0.17 |      0.5589 |           - |           - |             2.58 KB |
    AddOrUpdate |    10 |     1,067.5 ns |      50.43 ns |     136.33 ns |     1,073.5 ns |  1.00 |    0.00 |      0.4044 |           - |           - |             1.87 KB |
 ConcurrentDict |    10 |     1,943.9 ns |      92.31 ns |      81.83 ns |     1,921.3 ns |  1.85 |    0.25 |      0.6371 |           - |           - |             2.95 KB |
 AddOrUpdate_v2 |    10 |     2,688.3 ns |     229.79 ns |     677.55 ns |     2,342.9 ns |  2.50 |    0.77 |      0.4349 |           - |           - |             2.02 KB |
  ImmutableDict |    10 |     5,903.1 ns |     749.28 ns |     801.72 ns |     5,607.8 ns |  5.66 |    1.06 |      0.5875 |           - |           - |             2.73 KB |
                |       |                |               |               |                |       |         |             |             |             |                     |
 ConcurrentDict |   100 |    14,476.1 ns |   1,184.05 ns |   1,215.93 ns |    14,193.3 ns |  0.48 |    0.21 |      3.6011 |      0.0305 |           - |            16.66 KB |
 AddOrUpdate_v1 |   100 |    16,999.4 ns |   1,522.75 ns |   4,441.93 ns |    14,281.8 ns |  0.59 |    0.25 |      8.4686 |           - |           - |            39.05 KB |
 AddOrUpdate_v2 |   100 |    28,695.4 ns |      41.78 ns |      32.62 ns |    28,697.6 ns |  0.94 |    0.33 |      7.7515 |           - |           - |            35.81 KB |
    AddOrUpdate |   100 |    31,854.4 ns |   2,882.03 ns |   8,497.72 ns |    36,547.9 ns |  1.00 |    0.00 |      7.3242 |           - |           - |            33.91 KB |
  ImmutableDict |   100 |    89,602.2 ns |   1,873.19 ns |   2,229.89 ns |    88,767.5 ns |  2.98 |    1.05 |      9.3994 |           - |           - |            43.68 KB |
                |       |                |               |               |                |       |         |             |             |             |                     |
 ConcurrentDict |  1000 |   219,064.7 ns |     559.14 ns |     466.91 ns |   218,894.7 ns |  0.69 |    0.01 |     49.3164 |     17.8223 |           - |           254.29 KB |
 AddOrUpdate_v1 |  1000 |   297,651.6 ns |   1,073.37 ns |     838.02 ns |   297,421.1 ns |  0.93 |    0.01 |    120.6055 |      3.4180 |           - |           556.41 KB |
    AddOrUpdate |  1000 |   319,478.3 ns |   2,768.11 ns |   2,161.16 ns |   319,079.8 ns |  1.00 |    0.00 |    113.2813 |      0.9766 |           - |           526.48 KB |
 AddOrUpdate_v2 |  1000 |   615,321.8 ns |  72,410.43 ns | 213,503.80 ns |   467,207.0 ns |  1.97 |    0.66 |    118.6523 |      0.4883 |           - |            547.3 KB |
  ImmutableDict |  1000 | 1,516,613.7 ns | 107,376.35 ns | 290,298.20 ns | 1,387,325.2 ns |  4.95 |    1.19 |    140.6250 |      1.9531 |           - |           648.02 KB |

## 21.01.2019 - all versions compared

         Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |    30 |   3.326 us | 0.0098 us | 0.0092 us |  0.84 |    0.01 |      2.0409 |           - |           - |             9.42 KB |
 AddOrUpdate_v2 |    30 |   3.515 us | 0.0700 us | 0.0688 us |  0.89 |    0.02 |      1.9302 |           - |           - |             8.91 KB |
    AddOrUpdate |    30 |   3.945 us | 0.0261 us | 0.0231 us |  1.00 |    0.00 |      2.1591 |           - |           - |             9.98 KB |
 AddOrUpdate_v3 |    30 |   4.607 us | 0.0875 us | 0.0819 us |  1.17 |    0.02 |      1.7624 |           - |           - |             8.13 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   150 |  24.883 us | 0.4840 us | 0.4527 us |  0.86 |    0.02 |     13.5193 |           - |           - |            62.39 KB |
 AddOrUpdate_v2 |   150 |  26.862 us | 0.5042 us | 0.4717 us |  0.92 |    0.02 |     13.7024 |           - |           - |             63.2 KB |
    AddOrUpdate |   150 |  29.054 us | 0.1205 us | 0.1127 us |  1.00 |    0.00 |     15.1978 |           - |           - |            70.17 KB |
 AddOrUpdate_v3 |   150 |  35.585 us | 0.3740 us | 0.3498 us |  1.22 |    0.01 |     12.5732 |           - |           - |            58.05 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   500 | 124.728 us | 0.7225 us | 0.6759 us |  0.91 |    0.01 |     54.4434 |           - |           - |           251.95 KB |
 AddOrUpdate_v2 |   500 | 128.650 us | 1.4938 us | 1.3973 us |  0.94 |    0.01 |     58.1055 |      0.2441 |           - |           267.97 KB |
    AddOrUpdate |   500 | 137.325 us | 1.9010 us | 1.7782 us |  1.00 |    0.00 |     63.2324 |      0.2441 |           - |           291.42 KB |
 AddOrUpdate_v3 |   500 | 166.994 us | 1.7109 us | 1.6004 us |  1.22 |    0.02 |     52.7344 |           - |           - |           243.81 KB |

## Inlining With and removing not necessary call to Balance in case of Update. 
 
         Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |    30 |   3.323 us | 0.0121 us | 0.0107 us |  0.91 |    0.00 |      2.0409 |           - |           - |             9.42 KB |
    AddOrUpdate |    30 |   3.647 us | 0.0111 us | 0.0093 us |  1.00 |    0.00 |      2.0409 |           - |           - |             9.42 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   150 |  24.605 us | 0.2023 us | 0.1690 us |  0.92 |    0.02 |     13.5193 |           - |           - |            62.39 KB |
    AddOrUpdate |   150 |  26.832 us | 0.5300 us | 0.5443 us |  1.00 |    0.00 |     13.5193 |           - |           - |            62.39 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   500 | 121.799 us | 0.9309 us | 0.8252 us |  0.92 |    0.01 |     54.4434 |           - |           - |           251.95 KB |
    AddOrUpdate |   500 | 132.886 us | 2.3470 us | 2.0805 us |  1.00 |    0.00 |     54.4434 |           - |           - |           251.95 KB |


## Special fast logic for adding to empty branch.

         Method | Count |       Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |    30 |   3.369 us | 0.0102 us | 0.0090 us |  0.94 |    0.01 |      2.0409 |           - |           - |             9.42 KB |
    AddOrUpdate |    30 |   3.587 us | 0.0244 us | 0.0228 us |  1.00 |    0.00 |      2.0409 |           - |           - |             9.42 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   150 |  25.025 us | 0.4927 us | 0.4609 us |  0.95 |    0.02 |     13.5193 |           - |           - |            62.39 KB |
    AddOrUpdate |   150 |  26.235 us | 0.1058 us | 0.0989 us |  1.00 |    0.00 |     13.5193 |           - |           - |            62.39 KB |
                |       |            |           |           |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   500 | 126.106 us | 1.0204 us | 0.9545 us |  1.00 |    0.01 |     54.4434 |           - |           - |           251.95 KB |
    AddOrUpdate |   500 | 126.844 us | 0.8788 us | 0.7339 us |  1.00 |    0.00 |     54.4434 |           - |           - |           251.95 KB |

## Removing not necessary imbalanced tree creation before balance - first memory win.

         Method | Count |       Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    AddOrUpdate |    30 |   3.357 us | 0.0163 us | 0.0145 us |  1.00 |      1.9417 |           - |           - |             8.95 KB |
 AddOrUpdate_v1 |    30 |   3.359 us | 0.0124 us | 0.0116 us |  1.00 |      2.0409 |           - |           - |             9.42 KB |
                |       |            |           |           |       |             |             |             |                     |
 AddOrUpdate_v1 |   150 |  24.513 us | 0.0420 us | 0.0393 us |  0.98 |     13.5193 |           - |           - |            62.39 KB |
    AddOrUpdate |   150 |  25.074 us | 0.1160 us | 0.1085 us |  1.00 |     12.9395 |      0.0305 |           - |            59.72 KB |
                |       |            |           |           |       |             |             |             |                     |
    AddOrUpdate |   500 | 122.624 us | 0.6553 us | 0.6130 us |  1.00 |     52.4902 |      0.2441 |           - |           242.06 KB |
 AddOrUpdate_v1 |   500 | 122.656 us | 0.8018 us | 0.7500 us |  1.00 |     54.4434 |           - |           - |           251.95 KB |

## More variety to benchmark input

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
    AddOrUpdate |     5 |     472.4 ns |     2.139 ns |     1.786 ns |  1.00 |      0.2537 |           - |           - |             1.17 KB |
 AddOrUpdate_v1 |     5 |     488.1 ns |     2.258 ns |     2.112 ns |  1.03 |      0.2737 |           - |           - |             1.27 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,855.4 ns |    19.395 ns |    17.194 ns |  0.98 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   4,974.1 ns |    17.098 ns |    15.157 ns |  1.00 |      2.8000 |           - |           - |            12.94 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  35,267.4 ns |   100.767 ns |    84.145 ns |  0.98 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  35,874.3 ns |   173.786 ns |   162.560 ns |  1.00 |     18.0054 |           - |           - |            83.02 KB |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |  1000 | 302,900.4 ns | 1,116.252 ns | 1,044.143 ns |  1.00 |    115.7227 |      2.4414 |           - |           535.08 KB |
 AddOrUpdate_v1 |  1000 | 303,552.8 ns |   906.769 ns |   803.827 ns |  1.00 |    120.6055 |      3.4180 |           - |           556.41 KB |

## Remove unnecessary temporary left leaf(y) branch creation before balancing - memory win

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |     5 |     491.4 ns |     2.121 ns |     1.984 ns |  1.00 |      0.2737 |           - |           - |             1.27 KB |
    AddOrUpdate |     5 |     493.4 ns |     2.199 ns |     2.057 ns |  1.00 |      0.2537 |           - |           - |             1.17 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,871.0 ns |    26.907 ns |    23.852 ns |  0.98 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   4,949.1 ns |    10.957 ns |     9.713 ns |  1.00 |      2.7542 |           - |           - |             12.7 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  35,540.5 ns |   271.146 ns |   240.364 ns |  0.95 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  37,344.9 ns |   127.081 ns |   118.871 ns |  1.00 |     17.6392 |           - |           - |            81.38 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |  1000 | 308,033.2 ns | 1,643.015 ns | 1,536.877 ns |  0.98 |    120.6055 |      3.4180 |           - |           556.41 KB |
    AddOrUpdate |  1000 | 314,370.2 ns | 1,781.632 ns | 1,666.540 ns |  1.00 |    113.7695 |      0.4883 |           - |           525.09 KB |


## Remove unnecessary temporary right leaf(y) branch creation before balancing - memory win

         Method | Count |         Mean |        Error |       StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v1 |     5 |     489.0 ns |     2.395 ns |     2.241 ns |  0.99 |    0.01 |      0.2737 |           - |           - |             1.27 KB |
    AddOrUpdate |     5 |     496.0 ns |     3.869 ns |     3.619 ns |  1.00 |    0.00 |      0.2432 |           - |           - |             1.13 KB |
                |       |              |              |              |       |         |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,873.1 ns |    35.313 ns |    33.032 ns |  0.91 |    0.01 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   5,346.9 ns |    18.590 ns |    16.480 ns |  1.00 |    0.00 |      2.6779 |           - |           - |            12.38 KB |
                |       |              |              |              |       |         |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  36,484.4 ns |   309.583 ns |   274.437 ns |  0.90 |    0.01 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  40,641.9 ns |   628.973 ns |   588.342 ns |  1.00 |    0.00 |     17.1509 |      0.0610 |           - |            79.08 KB |
                |       |              |              |              |       |         |             |             |             |                     |
 AddOrUpdate_v1 |  1000 | 316,678.8 ns | 5,722.780 ns | 5,353.092 ns |  0.98 |    0.02 |    120.6055 |      3.4180 |           - |           556.41 KB |
    AddOrUpdate |  1000 | 323,403.6 ns | 1,483.940 ns | 1,315.474 ns |  1.00 |    0.00 |    111.8164 |     32.7148 |           - |           515.39 KB |


## Parity with v1 via handling one more special case - where `Height == 1`

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
    AddOrUpdate |     5 |     480.3 ns |     3.645 ns |     3.410 ns |  1.00 |      0.2432 |           - |           - |             1.13 KB |
 AddOrUpdate_v1 |     5 |     487.1 ns |     2.547 ns |     2.382 ns |  1.01 |      0.2737 |           - |           - |             1.27 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,844.9 ns |    23.494 ns |    21.976 ns |  0.99 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   4,908.8 ns |    29.374 ns |    27.477 ns |  1.00 |      2.6779 |           - |           - |            12.38 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  35,727.8 ns |    84.717 ns |    75.099 ns |  0.99 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  36,231.4 ns |   136.671 ns |   121.156 ns |  1.00 |     17.1509 |      0.0610 |           - |            79.08 KB |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |  1000 | 304,238.0 ns | 1,636.331 ns | 1,366.410 ns |  1.00 |    111.8164 |     32.7148 |           - |           515.39 KB |
 AddOrUpdate_v1 |  1000 | 307,233.3 ns | 1,487.373 ns | 1,391.290 ns |  1.01 |    120.6055 |      3.4180 |           - |           556.41 KB |


## Some fixes for updating things

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
    AddOrUpdate |     5 |     469.8 ns |     4.613 ns |     3.602 ns |  1.00 |      0.2432 |           - |           - |             1.13 KB |
 AddOrUpdate_v1 |     5 |     480.6 ns |     2.648 ns |     2.477 ns |  1.02 |      0.2737 |           - |           - |             1.27 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |    40 |   4,731.4 ns |    22.512 ns |    21.058 ns |  0.96 |      2.9678 |           - |           - |            13.69 KB |
    AddOrUpdate |    40 |   4,945.0 ns |    12.489 ns |    10.429 ns |  1.00 |      2.6779 |           - |           - |            12.38 KB |
                |       |              |              |              |       |             |             |             |                     |
 AddOrUpdate_v1 |   200 |  35,094.6 ns |    70.945 ns |    66.362 ns |  0.98 |     18.7378 |      0.0610 |           - |            86.53 KB |
    AddOrUpdate |   200 |  35,821.9 ns |   129.417 ns |   121.057 ns |  1.00 |     17.1509 |      0.0610 |           - |            79.08 KB |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |  1000 | 303,448.5 ns | 2,017.835 ns | 1,887.484 ns |  1.00 |    111.8164 |     32.7148 |           - |           515.39 KB |
 AddOrUpdate_v1 |  1000 | 304,471.0 ns | 1,549.817 ns | 1,449.700 ns |  1.00 |    120.6055 |      3.4180 |           - |           556.41 KB |


## Inlining the Balance and removing its not used if-branches

         Method | Count |         Mean |        Error |       StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|-------------:|------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v2 |     5 |     418.2 ns |     2.540 ns |     2.376 ns |  0.90 |      0.2170 |           - |           - |              1024 B |
    AddOrUpdate |     5 |     464.6 ns |     2.301 ns |     2.039 ns |  1.00 |      0.2437 |           - |           - |              1152 B |
 AddOrUpdate_v1 |     5 |     487.7 ns |     1.769 ns |     1.655 ns |  1.05 |      0.2737 |           - |           - |              1296 B |
 AddOrUpdate_v3 |     5 |     516.5 ns |     2.289 ns |     2.141 ns |  1.11 |      0.1993 |           - |           - |               944 B |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |    40 |   4,621.6 ns |    19.899 ns |    18.614 ns |  1.00 |      2.6779 |           - |           - |             12672 B |
 AddOrUpdate_v1 |    40 |   4,910.9 ns |    19.805 ns |    18.525 ns |  1.06 |      2.9678 |           - |           - |             14016 B |
 AddOrUpdate_v2 |    40 |   5,115.0 ns |    26.410 ns |    23.411 ns |  1.11 |      2.8458 |           - |           - |             13456 B |
 AddOrUpdate_v3 |    40 |   6,953.3 ns |    24.391 ns |    22.815 ns |  1.50 |      2.6016 |           - |           - |             12304 B |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |   200 |  34,250.7 ns |    87.221 ns |    81.586 ns |  1.00 |     17.1509 |      0.0610 |           - |             80976 B |
 AddOrUpdate_v1 |   200 |  35,425.8 ns |   137.918 ns |   129.008 ns |  1.03 |     18.7378 |      0.0610 |           - |             88608 B |
 AddOrUpdate_v2 |   200 |  37,942.8 ns |   188.112 ns |   175.960 ns |  1.11 |     19.2261 |           - |           - |             91008 B |
 AddOrUpdate_v3 |   200 |  50,640.8 ns |    66.485 ns |    55.518 ns |  1.48 |     17.6392 |      0.0610 |           - |             83352 B |
                |       |              |              |              |       |             |             |             |                     |
    AddOrUpdate |  1000 | 290,830.8 ns |   915.770 ns |   811.806 ns |  1.00 |    111.8164 |     32.7148 |           - |            527760 B |
 AddOrUpdate_v2 |  1000 | 308,260.3 ns | 2,278.822 ns | 2,020.116 ns |  1.06 |    130.8594 |      0.9766 |           - |            619056 B |
 AddOrUpdate_v1 |  1000 | 308,355.6 ns | 2,046.461 ns | 1,914.261 ns |  1.06 |    120.6055 |      3.4180 |           - |            569760 B |
 AddOrUpdate_v3 |  1000 | 375,880.3 ns | 1,710.716 ns | 1,600.205 ns |  1.29 |    118.6523 |      0.4883 |           - |            560440 B |


         Method | Count |         Mean |        Error |        StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------- |------ |-------------:|-------------:|--------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 AddOrUpdate_v2 |     5 |     404.8 ns |     2.504 ns |     2.0907 ns |  0.91 |    0.01 |      0.2170 |           - |           - |                1 KB |
    AddOrUpdate |     5 |     447.1 ns |     4.673 ns |     4.1429 ns |  1.00 |    0.00 |      0.2437 |           - |           - |             1.13 KB |
 AddOrUpdate_v1 |     5 |     481.5 ns |     1.112 ns |     0.9854 ns |  1.08 |    0.01 |      0.2737 |           - |           - |             1.27 KB |
                |       |              |              |               |       |         |             |             |             |                     |
    AddOrUpdate |    40 |   4,563.0 ns |    91.302 ns |    89.6712 ns |  1.00 |    0.00 |      2.6779 |           - |           - |            12.38 KB |
 AddOrUpdate_v1 |    40 |   4,712.5 ns |    27.017 ns |    25.2716 ns |  1.03 |    0.02 |      2.9678 |           - |           - |            13.69 KB |
 AddOrUpdate_v2 |    40 |   4,943.3 ns |    22.715 ns |    21.2474 ns |  1.08 |    0.02 |      2.8458 |           - |           - |            13.14 KB |
                |       |              |              |               |       |         |             |             |             |                     |
    AddOrUpdate |   200 |  33,629.4 ns |   647.198 ns |   635.6353 ns |  1.00 |    0.00 |     17.1509 |      0.0610 |           - |            79.08 KB |
 AddOrUpdate_v1 |   200 |  34,643.5 ns |   155.549 ns |   145.5005 ns |  1.03 |    0.02 |     18.7378 |      0.0610 |           - |            86.53 KB |
 AddOrUpdate_v2 |   200 |  36,726.4 ns |   419.812 ns |   372.1521 ns |  1.10 |    0.01 |     19.2261 |           - |           - |            88.88 KB |
                |       |              |              |               |       |         |             |             |             |                     |
    AddOrUpdate |  1000 | 291,376.3 ns | 3,419.191 ns | 3,198.3136 ns |  1.00 |    0.00 |    111.8164 |     32.7148 |           - |           515.39 KB |
 AddOrUpdate_v2 |  1000 | 302,027.2 ns | 4,981.373 ns | 4,659.5798 ns |  1.04 |    0.02 |    130.8594 |      0.9766 |           - |           604.55 KB |
 AddOrUpdate_v1 |  1000 | 304,899.6 ns | 4,634.673 ns | 4,335.2763 ns |  1.05 |    0.02 |    120.6055 |      3.4180 |           - |           556.41 KB |

##  Some base line results

|                   Method | Count |           Mean |        Error |       StdDev |         Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|------------------------- |------ |---------------:|-------------:|-------------:|---------------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|    ImHashMap_AddOrUpdate |    10 |       887.4 ns |     3.617 ns |     3.206 ns |       887.2 ns |  1.00 |    0.00 |      0.4978 |           - |           - |              2.3 KB |
| ImHashMap_V1_AddOrUpdate |    10 |       935.7 ns |     1.756 ns |     1.466 ns |       936.0 ns |  1.05 |    0.00 |      0.5589 |           - |           - |             2.58 KB |
|          DictSlim_TryAdd |    10 |       607.5 ns |     1.668 ns |     1.479 ns |       607.6 ns |  0.68 |    0.00 |      0.2365 |           - |           - |             1.09 KB |
|              Dict_TryAdd |    10 |       675.1 ns |    13.422 ns |    20.497 ns |       677.2 ns |  0.75 |    0.02 |      0.2203 |           - |           - |             1.02 KB |
|    ConcurrentDict_TryAdd |    10 |     1,960.2 ns |    41.256 ns |    40.519 ns |     1,941.3 ns |  2.21 |    0.05 |      0.6371 |           - |           - |             2.95 KB |
|        ImmutableDict_Add |    10 |     5,841.7 ns |   110.674 ns |   108.696 ns |     5,858.8 ns |  6.59 |    0.13 |      0.5875 |           - |           - |             2.73 KB |
|                          |       |                |              |              |                |       |         |             |             |             |                     |
|    ImHashMap_AddOrUpdate |   100 |    14,010.7 ns |   277.074 ns |   329.837 ns |    14,093.4 ns |  1.00 |    0.00 |      7.6904 |           - |           - |            35.48 KB |
| ImHashMap_V1_AddOrUpdate |   100 |    14,531.7 ns |   242.810 ns |   215.244 ns |    14,492.5 ns |  1.05 |    0.03 |      8.4686 |           - |           - |            39.05 KB |
|          DictSlim_TryAdd |   100 |     4,530.6 ns |    74.934 ns |    70.093 ns |     4,521.4 ns |  0.33 |    0.01 |      1.5945 |           - |           - |             7.36 KB |
|              Dict_TryAdd |   100 |     5,406.3 ns |    47.278 ns |    44.224 ns |     5,398.7 ns |  0.39 |    0.01 |      2.1667 |           - |           - |               10 KB |
|    ConcurrentDict_TryAdd |   100 |    14,861.7 ns |   548.482 ns |   931.365 ns |    14,335.9 ns |  1.09 |    0.08 |      3.6011 |      0.0153 |           - |            16.66 KB |
|        ImmutableDict_Add |   100 |    90,922.3 ns |   342.680 ns |   320.543 ns |    90,936.7 ns |  6.54 |    0.15 |      9.3994 |           - |           - |            43.68 KB |
|                          |       |                |              |              |                |       |         |             |             |             |                     |
|    ImHashMap_AddOrUpdate |  1000 |   282,986.9 ns | 1,500.444 ns | 1,403.516 ns |   282,819.6 ns |  1.00 |    0.00 |    111.8164 |      0.9766 |           - |           516.19 KB |
| ImHashMap_V1_AddOrUpdate |  1000 |   300,616.8 ns |   960.056 ns |   898.037 ns |   300,469.6 ns |  1.06 |    0.01 |    120.6055 |      1.9531 |           - |           557.11 KB |
|          DictSlim_TryAdd |  1000 |    41,977.2 ns |   690.173 ns |   645.588 ns |    42,189.1 ns |  0.15 |    0.00 |     12.2070 |           - |           - |             56.5 KB |
|              Dict_TryAdd |  1000 |    55,159.1 ns |   134.543 ns |   125.852 ns |    55,200.5 ns |  0.19 |    0.00 |     21.5454 |      0.0610 |           - |            99.87 KB |
|    ConcurrentDict_TryAdd |  1000 |   223,008.7 ns |   640.296 ns |   567.606 ns |   222,865.0 ns |  0.79 |    0.00 |     49.3164 |     17.8223 |           - |           254.29 KB |
|        ImmutableDict_Add |  1000 | 1,403,209.7 ns | 6,072.856 ns | 5,383.428 ns | 1,401,542.6 ns |  4.96 |    0.03 |    140.6250 |      1.9531 |           - |           648.84 KB |

# V2

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


|                                    Method | Count |           Mean |       Error |      StdDev | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|------------------------------------------ |------ |---------------:|------------:|------------:|------:|--------:|---------:|--------:|------:|----------:|
|                     ImHashMap_AddOrUpdate |     1 |       119.1 ns |     0.50 ns |     0.47 ns |  1.00 |    0.00 |   0.0577 |       - |     - |     272 B |
|              ImHashMapSlots32_AddOrUpdate |     1 |       214.7 ns |     1.19 ns |     1.00 ns |  1.80 |    0.01 |   0.1070 |  0.0002 |     - |     504 B |
|                  ImHashMap_V1_AddOrUpdate |     1 |       129.1 ns |     0.48 ns |     0.43 ns |  1.08 |    0.01 |   0.0610 |       - |     - |     288 B |
|        Experimental_ImHashMap_AddOrUpdate |     1 |       104.6 ns |     2.65 ns |     2.60 ns |  0.88 |    0.02 |   0.0340 |       - |     - |     160 B |
| Experimental_ImHashMapSlots32_AddOrUpdate |     1 |       213.7 ns |     0.70 ns |     0.54 ns |  1.79 |    0.01 |   0.0865 |       - |     - |     408 B |
| Experimental_ImHashMapSlots64_AddOrUpdate |     1 |       303.5 ns |     2.19 ns |     2.05 ns |  2.55 |    0.02 |   0.1407 |       - |     - |     664 B |
|                           DictSlim_TryAdd |     1 |       128.2 ns |     2.59 ns |     2.42 ns |  1.08 |    0.02 |   0.0408 |       - |     - |     192 B |
|                               Dict_TryAdd |     1 |       134.6 ns |     1.63 ns |     1.52 ns |  1.13 |    0.01 |   0.0544 |       - |     - |     256 B |
|                     ConcurrentDict_TryAdd |     1 |       303.7 ns |     3.84 ns |     3.59 ns |  2.55 |    0.03 |   0.2074 |  0.0014 |     - |     976 B |
|                 ImmutableDict_Builder_Add |     1 |       406.3 ns |     7.79 ns |     8.66 ns |  3.40 |    0.08 |   0.0572 |       - |     - |     272 B |
|                         ImmutableDict_Add |     1 |       445.0 ns |     1.78 ns |     1.58 ns |  3.74 |    0.02 |   0.0677 |       - |     - |     320 B |
|                                           |       |                |             |             |       |         |          |         |       |           |
|                     ImHashMap_AddOrUpdate |    10 |       826.9 ns |    16.33 ns |    15.27 ns |  1.00 |    0.00 |   0.4911 |  0.0029 |     - |    2312 B |
|              ImHashMapSlots32_AddOrUpdate |    10 |       619.3 ns |     6.62 ns |     5.87 ns |  0.75 |    0.02 |   0.2956 |  0.0019 |     - |    1392 B |
|                  ImHashMap_V1_AddOrUpdate |    10 |       995.7 ns |     7.97 ns |     7.06 ns |  1.21 |    0.02 |   0.6218 |  0.0038 |     - |    2928 B |
|        Experimental_ImHashMap_AddOrUpdate |    10 |       646.4 ns |     1.69 ns |     1.58 ns |  0.78 |    0.01 |   0.3176 |  0.0010 |     - |    1496 B |
| Experimental_ImHashMapSlots32_AddOrUpdate |    10 |       518.7 ns |     3.67 ns |     3.43 ns |  0.63 |    0.01 |   0.1764 |  0.0010 |     - |     832 B |
| Experimental_ImHashMapSlots64_AddOrUpdate |    10 |       602.5 ns |     1.95 ns |     1.82 ns |  0.73 |    0.01 |   0.2308 |  0.0010 |     - |    1088 B |
|                           DictSlim_TryAdd |    10 |       584.1 ns |    11.52 ns |    21.64 ns |  0.70 |    0.01 |   0.2375 |  0.0010 |     - |    1120 B |
|                               Dict_TryAdd |    10 |       601.1 ns |     1.72 ns |     1.61 ns |  0.73 |    0.01 |   0.2193 |  0.0010 |     - |    1032 B |
|                     ConcurrentDict_TryAdd |    10 |     1,388.6 ns |     8.04 ns |     7.53 ns |  1.68 |    0.03 |   0.6294 |  0.0095 |     - |    2968 B |
|                 ImmutableDict_Builder_Add |    10 |     2,536.3 ns |     9.62 ns |     9.00 ns |  3.07 |    0.06 |   0.1793 |       - |     - |     848 B |
|                         ImmutableDict_Add |    10 |     4,191.8 ns |    14.08 ns |    13.17 ns |  5.07 |    0.09 |   0.6180 |       - |     - |    2920 B |
|                                           |       |                |             |             |       |         |          |         |       |           |
|                     ImHashMap_AddOrUpdate |   100 |    12,297.8 ns |    43.22 ns |    38.32 ns |  1.00 |    0.00 |   7.4005 |  0.3510 |     - |   34856 B |
|              ImHashMapSlots32_AddOrUpdate |   100 |     6,642.4 ns |    34.81 ns |    32.56 ns |  0.54 |    0.00 |   3.1052 |  0.2060 |     - |   14640 B |
|                  ImHashMap_V1_AddOrUpdate |   100 |    15,066.8 ns |    57.35 ns |    53.64 ns |  1.22 |    0.01 |   8.5602 |  0.4425 |     - |   40320 B |
|        Experimental_ImHashMap_AddOrUpdate |   100 |    10,427.2 ns |    31.56 ns |    29.52 ns |  0.85 |    0.00 |   5.9204 |  0.2136 |     - |   27880 B |
| Experimental_ImHashMapSlots32_AddOrUpdate |   100 |     4,826.3 ns |    11.64 ns |    10.32 ns |  0.39 |    0.00 |   1.7624 |  0.0763 |     - |    8304 B |
| Experimental_ImHashMapSlots64_AddOrUpdate |   100 |     4,215.3 ns |    27.18 ns |    24.09 ns |  0.34 |    0.00 |   1.4038 |  0.0534 |     - |    6640 B |
|                           DictSlim_TryAdd |   100 |     4,086.4 ns |    15.28 ns |    14.29 ns |  0.33 |    0.00 |   1.5945 |  0.0458 |     - |    7536 B |
|                               Dict_TryAdd |   100 |     5,053.1 ns |    11.55 ns |    10.80 ns |  0.41 |    0.00 |   2.1667 |  0.0916 |     - |   10232 B |
|                     ConcurrentDict_TryAdd |   100 |    15,524.7 ns |    64.51 ns |    60.35 ns |  1.26 |    0.01 |   6.5613 |  0.0305 |     - |   30944 B |
|                 ImmutableDict_Builder_Add |   100 |    33,701.3 ns |   105.33 ns |    98.53 ns |  2.74 |    0.01 |   1.4038 |  0.0610 |     - |    6608 B |
|                         ImmutableDict_Add |   100 |    64,397.1 ns |   216.23 ns |   202.27 ns |  5.24 |    0.03 |   9.3994 |  0.2441 |     - |   44793 B |
|                                           |       |                |             |             |       |         |          |         |       |           |
|                     ImHashMap_AddOrUpdate |  1000 |   262,754.3 ns | 1,072.25 ns | 1,002.98 ns |  1.00 |    0.00 | 108.3984 | 30.7617 |     - |  511209 B |
|              ImHashMapSlots32_AddOrUpdate |  1000 |   155,706.1 ns |   561.75 ns |   525.46 ns |  0.59 |    0.00 |  57.3730 | 19.0430 |     - |  270865 B |
|                  ImHashMap_V1_AddOrUpdate |  1000 |   311,962.3 ns | 1,446.41 ns | 1,352.98 ns |  1.19 |    0.01 | 121.0938 | 35.1563 |     - |  571249 B |
|        Experimental_ImHashMap_AddOrUpdate |  1000 |   267,922.8 ns | 1,178.42 ns | 1,102.30 ns |  1.02 |    0.01 |  93.2617 | 22.9492 |     - |  439976 B |
| Experimental_ImHashMapSlots32_AddOrUpdate |  1000 |   125,153.0 ns |   587.68 ns |   549.72 ns |  0.48 |    0.00 |  42.7246 | 11.9629 |     - |  202080 B |
| Experimental_ImHashMapSlots64_AddOrUpdate |  1000 |   102,422.2 ns |   321.92 ns |   285.37 ns |  0.39 |    0.00 |  33.8135 |  8.6670 |     - |  159168 B |
|                           DictSlim_TryAdd |  1000 |    38,421.2 ns |   302.00 ns |   282.49 ns |  0.15 |    0.00 |  12.2681 |  0.0610 |     - |   57856 B |
|                               Dict_TryAdd |  1000 |    50,308.1 ns |   842.51 ns |   788.09 ns |  0.19 |    0.00 |  21.6064 |  5.3711 |     - |  102256 B |
|                     ConcurrentDict_TryAdd |  1000 |   172,505.9 ns | 2,518.96 ns | 2,356.24 ns |  0.66 |    0.01 |  49.0723 | 21.7285 |     - |  260008 B |
|                 ImmutableDict_Builder_Add |  1000 |   515,561.1 ns | 1,244.32 ns | 1,163.94 ns |  1.96 |    0.01 |  12.6953 |  2.9297 |     - |   64209 B |
|                         ImmutableDict_Add |  1000 | 1,013,851.3 ns | 4,173.47 ns | 3,903.87 ns |  3.86 |    0.02 | 140.6250 | 33.2031 |     - |  662171 B |

## V3 - baseline

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

|                             Method | Count |         Mean |       Error |      StdDev | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|----------------------------------- |------ |-------------:|------------:|------------:|------:|--------:|---------:|--------:|------:|----------:|
|              ImHashMap_AddOrUpdate |     1 |     141.3 ns |     2.51 ns |     2.68 ns |  1.00 |    0.00 |   0.0648 |       - |     - |     272 B |
| Experimental_ImHashMap_AddOrUpdate |     1 |     116.8 ns |     1.00 ns |     0.89 ns |  0.83 |    0.01 |   0.0381 |       - |     - |     160 B |
|           ImHashMap234_AddOrUpdate |     1 |     102.9 ns |     2.04 ns |     1.70 ns |  0.73 |    0.01 |   0.0381 |       - |     - |     160 B |
|                                    |       |              |             |             |       |         |          |         |       |           |
|              ImHashMap_AddOrUpdate |    10 |     938.8 ns |    15.31 ns |    12.78 ns |  1.00 |    0.00 |   0.5512 |       - |     - |    2312 B |
| Experimental_ImHashMap_AddOrUpdate |    10 |     708.0 ns |     6.11 ns |     5.72 ns |  0.75 |    0.01 |   0.3576 |       - |     - |    1496 B |
|           ImHashMap234_AddOrUpdate |    10 |     678.9 ns |    10.68 ns |     9.47 ns |  0.72 |    0.02 |   0.2995 |       - |     - |    1256 B |
|                                    |       |              |             |             |       |         |          |         |       |           |
|              ImHashMap_AddOrUpdate |   100 |  14,155.3 ns |   124.99 ns |   116.92 ns |  1.00 |    0.00 |   8.3313 |       - |     - |   34856 B |
| Experimental_ImHashMap_AddOrUpdate |   100 |  11,756.1 ns |   104.34 ns |    87.13 ns |  0.83 |    0.01 |   6.6528 |       - |     - |   27880 B |
|           ImHashMap234_AddOrUpdate |   100 |  10,581.4 ns |   106.89 ns |    94.76 ns |  0.75 |    0.01 |   5.3406 |       - |     - |   22384 B |
|                                    |       |              |             |             |       |         |          |         |       |           |
|              ImHashMap_AddOrUpdate |  1000 | 308,506.7 ns | 3,081.93 ns | 2,882.84 ns |  1.00 |    0.00 | 122.0703 |  3.4180 |     - |  511209 B |
| Experimental_ImHashMap_AddOrUpdate |  1000 | 309,012.9 ns | 1,882.16 ns | 1,760.57 ns |  1.00 |    0.01 | 104.9805 | 25.8789 |     - |  439977 B |
|           ImHashMap234_AddOrUpdate |  1000 | 231,146.4 ns | 1,493.09 ns | 1,323.58 ns |  0.75 |    0.01 |  82.2754 | 18.0664 |     - |  344593 B |

## V3 - RTM

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT


|                            Method | Count |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|---------------------------------- |------ |-------------:|-------------:|-------------:|-------------:|------:|--------:|---------:|--------:|------:|----------:|
|          V2_ImHashMap_AddOrUpdate |     1 |     126.8 ns |      2.11 ns |      1.87 ns |     126.3 ns |  1.00 |    0.00 |   0.0432 |       - |     - |     272 B |
|          V3_ImHashMap_AddOrUpdate |     1 |     106.2 ns |      2.15 ns |      2.73 ns |     105.0 ns |  0.84 |    0.03 |   0.0253 |       - |     - |     160 B |
| V3_PartitionedHashMap_AddOrUpdate |     1 |     152.5 ns |      3.12 ns |      4.06 ns |     152.3 ns |  1.21 |    0.03 |   0.0446 |       - |     - |     280 B |
|                   DictSlim_TryAdd |     1 |     130.9 ns |      2.61 ns |      3.30 ns |     130.3 ns |  1.04 |    0.04 |   0.0305 |       - |     - |     192 B |
|                       Dict_TryAdd |     1 |     150.5 ns |      3.08 ns |      4.11 ns |     150.7 ns |  1.17 |    0.04 |   0.0420 |       - |     - |     264 B |
|       ConcurrentDictionary_TryAdd |     1 |     294.2 ns |      5.67 ns |      6.07 ns |     292.9 ns |  2.33 |    0.06 |   0.1540 |  0.0010 |     - |     968 B |
|         ImmutableDict_Builder_Add |     1 |     345.5 ns |      6.82 ns |      6.38 ns |     346.0 ns |  2.73 |    0.08 |   0.0429 |       - |     - |     272 B |
|                 ImmutableDict_Add |     1 |     369.7 ns |      6.77 ns |      5.66 ns |     369.5 ns |  2.91 |    0.06 |   0.0505 |       - |     - |     320 B |
|                                   |       |              |              |              |              |       |         |          |         |       |           |
|          V2_ImHashMap_AddOrUpdate |    10 |     947.0 ns |     18.95 ns |     20.27 ns |     952.2 ns |  1.00 |    0.00 |   0.3681 |  0.0019 |     - |    2312 B |
|          V3_ImHashMap_AddOrUpdate |    10 |     623.1 ns |      5.55 ns |      4.92 ns |     621.8 ns |  0.66 |    0.02 |   0.1669 |       - |     - |    1048 B |
| V3_PartitionedHashMap_AddOrUpdate |    10 |     547.4 ns |     10.18 ns |     11.31 ns |     544.0 ns |  0.58 |    0.01 |   0.1221 |       - |     - |     768 B |
|                   DictSlim_TryAdd |    10 |     625.1 ns |     12.03 ns |     13.37 ns |     621.7 ns |  0.66 |    0.02 |   0.1783 |       - |     - |    1120 B |
|                       Dict_TryAdd |    10 |     646.5 ns |     12.85 ns |     16.71 ns |     643.2 ns |  0.68 |    0.02 |   0.1650 |       - |     - |    1040 B |
|       ConcurrentDictionary_TryAdd |    10 |   1,425.0 ns |     28.01 ns |     50.50 ns |   1,405.0 ns |  1.49 |    0.04 |   0.4730 |  0.0076 |     - |    2968 B |
|         ImmutableDict_Builder_Add |    10 |   2,099.4 ns |     30.91 ns |     24.14 ns |   2,089.6 ns |  2.23 |    0.06 |   0.1335 |       - |     - |     848 B |
|                 ImmutableDict_Add |    10 |   3,182.2 ns |     31.64 ns |     26.42 ns |   3,191.6 ns |  3.37 |    0.07 |   0.4654 |       - |     - |    2920 B |
|                                   |       |              |              |              |              |       |         |          |         |       |           |
|          V2_ImHashMap_AddOrUpdate |   100 |  13,960.5 ns |    193.69 ns |    151.22 ns |  13,974.8 ns |  1.00 |    0.00 |   5.5542 |  0.2441 |     - |   34856 B |
|          V3_ImHashMap_AddOrUpdate |   100 |  11,501.2 ns |    217.62 ns |    241.88 ns |  11,495.1 ns |  0.82 |    0.02 |   3.1891 |  0.1068 |     - |   20008 B |
| V3_PartitionedHashMap_AddOrUpdate |   100 |   5,929.6 ns |    112.54 ns |    175.21 ns |   5,953.0 ns |  0.44 |    0.01 |   1.2436 |  0.0534 |     - |    7816 B |
|                   DictSlim_TryAdd |   100 |   4,930.6 ns |     95.70 ns |    127.75 ns |   5,001.7 ns |  0.35 |    0.01 |   1.1978 |  0.0305 |     - |    7536 B |
|                       Dict_TryAdd |   100 |   5,438.5 ns |     95.98 ns |    137.65 ns |   5,404.3 ns |  0.39 |    0.01 |   1.6251 |  0.0610 |     - |   10240 B |
|       ConcurrentDictionary_TryAdd |   100 |  16,669.7 ns |    326.23 ns |    362.61 ns |  16,763.5 ns |  1.19 |    0.03 |   4.9133 |  0.5798 |     - |   30968 B |
|         ImmutableDict_Builder_Add |   100 |  27,158.0 ns |    354.59 ns |    276.84 ns |  27,148.7 ns |  1.95 |    0.03 |   1.0376 |  0.0305 |     - |    6608 B |
|                 ImmutableDict_Add |   100 |  50,784.5 ns |    990.99 ns |  1,571.81 ns |  51,475.4 ns |  3.55 |    0.14 |   7.0801 |  0.2441 |     - |   44792 B |
|                                   |       |              |              |              |              |       |         |          |         |       |           |
|          V2_ImHashMap_AddOrUpdate |  1000 | 287,955.3 ns |  4,859.90 ns |  4,545.95 ns | 286,362.2 ns |  1.00 |    0.00 |  81.0547 |  0.9766 |     - |  511208 B |
|          V3_ImHashMap_AddOrUpdate |  1000 | 240,386.3 ns |  4,778.73 ns |  8,738.18 ns | 240,952.2 ns |  0.85 |    0.04 |  51.2695 | 11.7188 |     - |  324016 B |
| V3_PartitionedHashMap_AddOrUpdate |  1000 | 144,426.5 ns |  2,847.55 ns |  4,348.50 ns | 146,011.0 ns |  0.50 |    0.02 |  28.0762 |  8.0566 |     - |  176216 B |
|                   DictSlim_TryAdd |  1000 |  44,217.2 ns |    145.85 ns |    113.87 ns |  44,205.9 ns |  0.15 |    0.00 |   9.1553 |  1.5259 |     - |   57856 B |
|                       Dict_TryAdd |  1000 |  55,251.6 ns |  1,084.55 ns |  1,955.67 ns |  55,508.3 ns |  0.19 |    0.01 |  16.2354 |  5.3711 |     - |  102264 B |
|       ConcurrentDictionary_TryAdd |  1000 | 171,835.0 ns |  3,414.78 ns |  4,787.04 ns | 174,681.6 ns |  0.60 |    0.02 |  41.2598 | 14.6484 |     - |  260056 B |
|         ImmutableDict_Builder_Add |  1000 | 445,419.2 ns |  4,140.17 ns |  3,872.72 ns | 445,259.3 ns |  1.55 |    0.03 |   9.7656 |  1.9531 |     - |   64208 B |
|                 ImmutableDict_Add |  1000 | 815,930.7 ns | 15,795.06 ns | 17,556.16 ns | 810,772.7 ns |  2.83 |    0.06 | 105.4688 | 24.4141 |     - |  662168 B |

## Against static TypeDictionary by @rogeralsing (need to substract the cost of enumerating the array when adding items to ImHashMap and PartitionedHashMap)

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

|                            Method | Count |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|---------------------------------- |------ |---------:|---------:|---------:|------:|--------:|-------:|-------:|------:|----------:|
|          V3_ImHashMap_AddOrUpdate |     5 | 117.9 ns |  2.33 ns |  2.69 ns |  1.00 |    0.00 | 0.0675 |      - |     - |     424 B |
| V3_PartitionedHashMap_AddOrUpdate |     5 | 162.5 ns |  3.34 ns |  6.75 ns |  1.38 |    0.07 | 0.0675 |      - |     - |     424 B |
|                TypeDictionary_Add |     5 | 178.9 ns |  3.66 ns |  6.69 ns |  1.54 |    0.06 | 0.1402 | 0.0007 |     - |     880 B |
|                       Dict_TryAdd |     5 | 187.0 ns |  3.76 ns |  5.74 ns |  1.59 |    0.07 | 0.0739 |      - |     - |     464 B |

|          V3_ImHashMap_AddOrUpdate |    10 | 321.8 ns |  6.48 ns | 10.10 ns |  1.00 |    0.00 | 0.1593 | 0.0005 |     - |    1000 B |
| V3_PartitionedHashMap_AddOrUpdate |    10 | 287.6 ns |  5.76 ns | 11.09 ns |  0.90 |    0.04 | 0.1144 | 0.0005 |     - |     720 B |
|                TypeDictionary_Add |    10 | 292.1 ns |  5.83 ns |  7.78 ns |  0.91 |    0.04 | 0.1402 | 0.0005 |     - |     880 B |
|                       Dict_TryAdd |    10 | 371.1 ns |  7.40 ns | 14.78 ns |  1.14 |    0.06 | 0.1578 |      - |     - |     992 B |

|          V3_ImHashMap_AddOrUpdate |    20 | 864.3 ns | 16.66 ns | 22.24 ns |  1.00 |    0.00 | 0.3977 | 0.0029 |     - |    2496 B |
| V3_PartitionedHashMap_AddOrUpdate |    20 | 523.1 ns |  4.68 ns |  4.15 ns |  0.60 |    0.02 | 0.2031 | 0.0010 |     - |    1280 B |
|                TypeDictionary_Add |    20 | 492.6 ns |  4.73 ns |  4.19 ns |  0.57 |    0.02 | 0.1402 |      - |     - |     880 B |
|                       Dict_TryAdd |    20 | 666.1 ns | 10.66 ns |  8.90 ns |  0.77 |    0.02 | 0.3309 | 0.0019 |     - |    2080 B |

## Small condition optimization

|                   Method | Count |     Mean |   Error |  StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------------------- |------ |---------:|--------:|--------:|------:|--------:|-------:|-------:|------:|----------:|
| V3_ImHashMap_AddOrUpdate |    10 | 317.7 ns | 2.16 ns | 2.02 ns |  1.00 |    0.00 | 0.1593 | 0.0005 |     - |    1000 B |
| V2_ImHashMap_AddOrUpdate |    10 | 617.4 ns | 6.60 ns | 5.51 ns |  1.94 |    0.02 | 0.3605 | 0.0019 |     - |    2264 B |


## V4

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT


|                            Method | Count |          Mean |         Error |        StdDev | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|---------------------------------- |------ |--------------:|--------------:|--------------:|------:|--------:|---------:|--------:|------:|----------:|
|              V4_ImMap_AddOrUpdate |     1 |      44.46 ns |      0.643 ns |      0.602 ns |  1.00 |    0.00 |   0.0178 |       - |     - |     112 B |
|          V3_ImHashMap_AddOrUpdate |     1 |      43.57 ns |      0.755 ns |      0.706 ns |  0.98 |    0.02 |   0.0178 |       - |     - |     112 B |
| V4_PartitionedHashMap_AddOrUpdate |     1 |      99.75 ns |      1.110 ns |      1.038 ns |  2.24 |    0.04 |   0.0370 |       - |     - |     232 B |
| V3_PartitionedHashMap_AddOrUpdate |     1 |     101.54 ns |      1.586 ns |      1.406 ns |  2.29 |    0.04 |   0.0370 |       - |     - |     232 B |
|                   DictSlim_TryAdd |     1 |      61.10 ns |      0.926 ns |      0.774 ns |  1.38 |    0.03 |   0.0229 |       - |     - |     144 B |
|                       Dict_TryAdd |     1 |      67.67 ns |      1.356 ns |      1.202 ns |  1.53 |    0.04 |   0.0343 |       - |     - |     216 B |
|       ConcurrentDictionary_TryAdd |     1 |     200.94 ns |      2.530 ns |      1.976 ns |  4.53 |    0.08 |   0.1466 |  0.0007 |     - |     920 B |
|         ImmutableDict_Builder_Add |     1 |     220.29 ns |      4.085 ns |      3.621 ns |  4.97 |    0.11 |   0.0353 |       - |     - |     224 B |
|                 ImmutableDict_Add |     1 |     274.65 ns |      2.175 ns |      1.816 ns |  6.19 |    0.07 |   0.0429 |       - |     - |     272 B |
|                                   |       |               |               |               |       |         |          |         |       |           |
|              V4_ImMap_AddOrUpdate |    10 |     327.37 ns |      3.174 ns |      2.478 ns |  1.00 |    0.00 |   0.1564 |  0.0005 |     - |     984 B |
|          V3_ImHashMap_AddOrUpdate |    10 |     331.38 ns |      5.852 ns |      5.747 ns |  1.01 |    0.02 |   0.1593 |  0.0005 |     - |    1000 B |
| V4_PartitionedHashMap_AddOrUpdate |    10 |     345.95 ns |      5.741 ns |      4.794 ns |  1.06 |    0.02 |   0.1144 |  0.0005 |     - |     720 B |
| V3_PartitionedHashMap_AddOrUpdate |    10 |     358.84 ns |      2.883 ns |      2.407 ns |  1.10 |    0.01 |   0.1144 |  0.0005 |     - |     720 B |
|                   DictSlim_TryAdd |    10 |     367.22 ns |      5.589 ns |      5.228 ns |  1.12 |    0.02 |   0.1707 |  0.0005 |     - |    1072 B |
|                       Dict_TryAdd |    10 |     371.19 ns |      3.319 ns |      3.105 ns |  1.14 |    0.01 |   0.1578 |  0.0005 |     - |     992 B |
|       ConcurrentDictionary_TryAdd |    10 |   1,160.51 ns |     10.741 ns |      8.969 ns |  3.55 |    0.04 |   0.4730 |  0.0076 |     - |    2968 B |
|         ImmutableDict_Builder_Add |    10 |   1,483.70 ns |     17.120 ns |     15.177 ns |  4.53 |    0.04 |   0.1259 |       - |     - |     800 B |
|                 ImmutableDict_Add |    10 |   2,668.61 ns |     42.514 ns |     62.317 ns |  8.19 |    0.23 |   0.4349 |       - |     - |    2744 B |
|                                   |       |               |               |               |       |         |          |         |       |           |
|              V4_ImMap_AddOrUpdate |   100 |   8,444.12 ns |     87.991 ns |     78.002 ns |  1.00 |    0.00 |   2.8229 |  0.0916 |     - |   17792 B |
|          V3_ImHashMap_AddOrUpdate |   100 |   9,417.97 ns |    107.459 ns |     95.260 ns |  1.12 |    0.02 |   3.1891 |  0.1068 |     - |   20032 B |
| V4_PartitionedHashMap_AddOrUpdate |   100 |   4,086.00 ns |     41.785 ns |     34.892 ns |  0.48 |    0.01 |   1.2360 |  0.0534 |     - |    7776 B |
| V3_PartitionedHashMap_AddOrUpdate |   100 |   4,184.28 ns |     69.080 ns |     64.618 ns |  0.50 |    0.01 |   1.2436 |  0.0534 |     - |    7808 B |
|                   DictSlim_TryAdd |   100 |   2,876.76 ns |     26.714 ns |     23.681 ns |  0.34 |    0.00 |   1.1902 |  0.0305 |     - |    7488 B |
|                       Dict_TryAdd |   100 |   3,480.02 ns |     60.940 ns |     54.022 ns |  0.41 |    0.01 |   1.6174 |  0.0763 |     - |   10192 B |
|       ConcurrentDictionary_TryAdd |   100 |  14,052.50 ns |    183.111 ns |    171.282 ns |  1.66 |    0.02 |   4.8981 |  0.5798 |     - |   30824 B |
|         ImmutableDict_Builder_Add |   100 |  21,979.51 ns |    231.436 ns |    205.162 ns |  2.60 |    0.04 |   1.0376 |  0.0305 |     - |    6560 B |
|                 ImmutableDict_Add |   100 |  43,971.70 ns |    635.678 ns |    594.614 ns |  5.21 |    0.08 |   7.1411 |  0.2441 |     - |   44936 B |
|                                   |       |               |               |               |       |         |          |         |       |           |
|              V4_ImMap_AddOrUpdate |  1000 | 252,084.32 ns |  2,651.514 ns |  2,350.498 ns |  1.00 |    0.00 |  45.4102 | 10.7422 |     - |  286344 B |
|          V3_ImHashMap_AddOrUpdate |  1000 | 212,485.75 ns |  2,508.266 ns |  2,223.513 ns |  0.84 |    0.01 |  51.5137 | 12.2070 |     - |  324176 B |
| V4_PartitionedHashMap_AddOrUpdate |  1000 | 130,987.08 ns |  1,343.597 ns |  1,191.064 ns |  0.52 |    0.01 |  26.1230 |  7.5684 |     - |  164280 B |
| V3_PartitionedHashMap_AddOrUpdate |  1000 | 121,434.74 ns |    952.940 ns |    743.992 ns |  0.48 |    0.00 |  28.0762 |  6.8359 |     - |  176840 B |
|                   DictSlim_TryAdd |  1000 |  30,469.88 ns |    553.377 ns |    517.629 ns |  0.12 |    0.00 |   9.1553 |  1.6479 |     - |   57808 B |
|                       Dict_TryAdd |  1000 |  36,957.82 ns |    325.290 ns |    253.965 ns |  0.15 |    0.00 |  16.2354 |  5.3711 |     - |  102216 B |
|       ConcurrentDictionary_TryAdd |  1000 | 147,942.69 ns |  2,199.522 ns |  1,836.700 ns |  0.59 |    0.01 |  41.2598 | 13.6719 |     - |  259720 B |
|         ImmutableDict_Builder_Add |  1000 | 364,927.76 ns |  5,953.010 ns |  5,568.449 ns |  1.45 |    0.03 |   9.7656 |  2.4414 |     - |   64160 B |
|                 ImmutableDict_Add |  1000 | 717,361.53 ns | 11,811.986 ns | 10,471.017 ns |  2.85 |    0.05 | 105.4688 | 25.3906 |     - |  665001 B |


Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                            Method | Count |          Mean |        Error |       StdDev | Ratio | RatioSD |    Gen 0 |   Gen 1 | Gen 2 | Allocated |
|---------------------------------- |------ |--------------:|-------------:|-------------:|------:|--------:|---------:|--------:|------:|----------:|
|          V4_ImHashMap_AddOrUpdate |     1 |      33.32 ns |     0.439 ns |     0.389 ns |  1.00 |    0.00 |   0.0178 |       - |     - |     112 B |
|          V3_ImHashMap_AddOrUpdate |     1 |      35.97 ns |     0.639 ns |     0.567 ns |  1.08 |    0.02 |   0.0178 |       - |     - |     112 B |
| V4_PartitionedHashMap_AddOrUpdate |     1 |      97.72 ns |     1.294 ns |     1.148 ns |  2.93 |    0.05 |   0.0370 |       - |     - |     232 B |
|                   DictSlim_TryAdd |     1 |      59.37 ns |     0.519 ns |     0.434 ns |  1.78 |    0.02 |   0.0229 |       - |     - |     144 B |
|                       Dict_TryAdd |     1 |      65.94 ns |     0.631 ns |     0.560 ns |  1.98 |    0.02 |   0.0343 |       - |     - |     216 B |
|       ConcurrentDictionary_TryAdd |     1 |     197.29 ns |     1.883 ns |     1.761 ns |  5.92 |    0.09 |   0.1466 |  0.0007 |     - |     920 B |
|         ImmutableDict_Builder_Add |     1 |     220.79 ns |     2.648 ns |     2.477 ns |  6.62 |    0.10 |   0.0355 |       - |     - |     224 B |
|                 ImmutableDict_Add |     1 |     264.15 ns |     4.786 ns |     3.737 ns |  7.93 |    0.16 |   0.0429 |       - |     - |     272 B |
|                                   |       |               |              |              |       |         |          |         |       |           |
|          V4_ImHashMap_AddOrUpdate |    10 |     307.91 ns |     5.281 ns |     4.940 ns |  1.00 |    0.00 |   0.1564 |  0.0005 |     - |     984 B |
|          V3_ImHashMap_AddOrUpdate |    10 |     389.09 ns |     6.482 ns |     5.746 ns |  1.27 |    0.03 |   0.1593 |  0.0005 |     - |    1000 B |
| V4_PartitionedHashMap_AddOrUpdate |    10 |     392.47 ns |     4.296 ns |     5.276 ns |  1.27 |    0.03 |   0.1144 |  0.0005 |     - |     720 B |
|                   DictSlim_TryAdd |    10 |     409.45 ns |     2.216 ns |     1.965 ns |  1.33 |    0.02 |   0.1707 |  0.0005 |     - |    1072 B |
|                       Dict_TryAdd |    10 |     413.38 ns |     5.460 ns |     5.107 ns |  1.34 |    0.03 |   0.1578 |  0.0005 |     - |     992 B |
|       ConcurrentDictionary_TryAdd |    10 |   1,319.88 ns |    20.332 ns |    19.019 ns |  4.29 |    0.11 |   0.4730 |  0.0076 |     - |    2968 B |
|         ImmutableDict_Builder_Add |    10 |   1,712.26 ns |    15.264 ns |    26.733 ns |  5.59 |    0.12 |   0.1259 |       - |     - |     800 B |
|                 ImmutableDict_Add |    10 |   2,986.38 ns |    17.824 ns |    14.884 ns |  9.70 |    0.18 |   0.4349 |       - |     - |    2744 B |
|                                   |       |               |              |              |       |         |          |         |       |           |
|          V4_ImHashMap_AddOrUpdate |   100 |   9,188.79 ns |   116.272 ns |   108.761 ns |  1.00 |    0.00 |   2.8229 |  0.0916 |     - |   17792 B |
|          V3_ImHashMap_AddOrUpdate |   100 |  10,591.30 ns |   169.894 ns |   141.869 ns |  1.15 |    0.03 |   3.1891 |  0.1068 |     - |   20032 B |
| V4_PartitionedHashMap_AddOrUpdate |   100 |   4,491.75 ns |    81.205 ns |    75.959 ns |  0.49 |    0.01 |   1.2360 |  0.0534 |     - |    7776 B |
|                   DictSlim_TryAdd |   100 |   3,201.77 ns |    48.216 ns |    42.743 ns |  0.35 |    0.01 |   1.1902 |  0.0305 |     - |    7488 B |
|                       Dict_TryAdd |   100 |   3,856.61 ns |    70.806 ns |    62.768 ns |  0.42 |    0.01 |   1.6174 |  0.0687 |     - |   10192 B |
|       ConcurrentDictionary_TryAdd |   100 |  16,035.70 ns |   320.350 ns |   356.068 ns |  1.75 |    0.04 |   4.9133 |  0.5798 |     - |   30824 B |
|         ImmutableDict_Builder_Add |   100 |  24,427.83 ns |   470.630 ns |   417.201 ns |  2.66 |    0.07 |   1.0376 |  0.0305 |     - |    6560 B |
|                 ImmutableDict_Add |   100 |  48,772.32 ns |   687.894 ns |   574.422 ns |  5.30 |    0.10 |   7.1411 |  0.2441 |     - |   44936 B |
|                                   |       |               |              |              |       |         |          |         |       |           |
|          V4_ImHashMap_AddOrUpdate |  1000 | 277,024.17 ns | 3,349.815 ns | 2,969.524 ns |  1.00 |    0.00 |  45.4102 | 10.7422 |     - |  286344 B |
|          V3_ImHashMap_AddOrUpdate |  1000 | 234,444.41 ns | 2,324.332 ns | 1,940.923 ns |  0.85 |    0.01 |  51.5137 | 12.2070 |     - |  324176 B |
| V4_PartitionedHashMap_AddOrUpdate |  1000 | 146,984.47 ns |   954.053 ns |   845.743 ns |  0.53 |    0.01 |  26.1230 |  7.5684 |     - |  164280 B |
|                   DictSlim_TryAdd |  1000 |  33,447.54 ns |   254.743 ns |   225.823 ns |  0.12 |    0.00 |   9.1553 |  1.7700 |     - |   57808 B |
|                       Dict_TryAdd |  1000 |  40,234.23 ns |   430.048 ns |   381.226 ns |  0.15 |    0.00 |  16.2354 |  5.3711 |     - |  102216 B |
|       ConcurrentDictionary_TryAdd |  1000 | 165,018.54 ns | 3,075.862 ns | 2,568.484 ns |  0.60 |    0.01 |  41.2598 | 13.6719 |     - |  259720 B |
|         ImmutableDict_Builder_Add |  1000 | 396,623.48 ns | 4,099.094 ns | 3,834.295 ns |  1.43 |    0.02 |   9.7656 |  2.4414 |     - |   64160 B |
|                 ImmutableDict_Add |  1000 | 813,397.08 ns | 9,203.453 ns | 8,608.916 ns |  2.94 |    0.04 | 105.4688 | 25.3906 |     - |  665001 B |

## ??? regress ???

|                   Method | Count |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD |    Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------- |------ |-----------:|----------:|----------:|-----------:|------:|--------:|---------:|------:|------:|----------:|
| V4_ImHashMap_AddOrUpdate |   100 |   9.367 us | 0.1855 us | 0.3661 us |   9.307 us |  1.00 |    0.00 |   5.7831 |     - |     - |  17.73 KB |
| V3_ImHashMap_AddOrUpdate |   100 |  10.171 us | 0.2027 us | 0.5011 us |   9.958 us |  1.10 |    0.07 |   6.3782 |     - |     - |  19.56 KB |
|                          |       |            |           |           |            |       |         |          |       |       |           |
| V4_ImHashMap_AddOrUpdate |  1000 | 294.633 us | 5.8329 us | 6.4833 us | 293.203 us |  1.00 |    0.00 |  90.8203 |     - |     - | 279.65 KB |
| V3_ImHashMap_AddOrUpdate |  1000 | 253.610 us | 4.9597 us | 9.5557 us | 250.384 us |  0.86 |    0.03 | 103.0273 |     - |     - | 316.58 KB |

|                   Method | Count |      Mean |    Error |    StdDev | Ratio | RatioSD |    Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------- |------ |----------:|---------:|----------:|------:|--------:|---------:|------:|------:|----------:|
| V4_ImHashMap_AddOrUpdate |   100 |  10.98 us | 0.219 us |  0.600 us |  1.00 |    0.00 |   5.6763 |     - |     - |  17.42 KB |
| V3_ImHashMap_AddOrUpdate |   100 |  11.93 us | 0.236 us |  0.444 us |  1.08 |    0.07 |   6.3782 |     - |     - |  19.56 KB |
|                          |       |           |          |           |       |         |          |       |       |           |
| V4_ImHashMap_AddOrUpdate |  1000 | 345.70 us | 4.332 us |  4.815 us |  1.00 |    0.00 |  91.3086 |     - |     - | 279.86 KB |
| V3_ImHashMap_AddOrUpdate |  1000 | 296.34 us | 5.848 us | 11.680 us |  0.86 |    0.03 | 103.0273 |     - |     - | 316.58 KB |

|                   Method | Count |      Mean |    Error |    StdDev |    Median | Ratio | RatioSD |    Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------- |------ |----------:|---------:|----------:|----------:|------:|--------:|---------:|------:|------:|----------:|
| V4_ImHashMap_AddOrUpdate |   100 |  11.11 us | 0.221 us |  0.587 us |  10.94 us |  1.00 |    0.00 |   5.6610 |     - |     - |  17.38 KB |
| V3_ImHashMap_AddOrUpdate |   100 |  12.06 us | 0.238 us |  0.562 us |  11.84 us |  1.08 |    0.08 |   6.3782 |     - |     - |  19.56 KB |
|                          |       |           |          |           |           |       |         |          |       |       |           |
| V4_ImHashMap_AddOrUpdate |  1000 | 360.84 us | 7.210 us | 13.543 us | 355.43 us |  1.00 |    0.00 |  91.3086 |     - |     - | 280.23 KB |
| V3_ImHashMap_AddOrUpdate |  1000 | 299.54 us | 5.907 us | 10.500 us | 294.68 us |  0.83 |    0.05 | 103.0273 |     - |     - | 316.58 KB |

*/
            [Params(100, 1000)]
            // [Params(1, 10, 100, 1000)]
            public int Count;

            private Type[] _types;

            [GlobalSetup]
            public void Setup()
            {
                _types = _keys.Take(Count).ToArray();
            }

            [Benchmark(Baseline = true)]
            public ImTools.ImHashMap<Type, string> V4_ImHashMap_AddOrUpdate()
            {
                var map = ImTools.ImHashMap<Type, string>.Empty;

                foreach (var key in _types)
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!");
            }

            // [Benchmark]
            public ImTools.ImHashMap<Type, string> V4_ImMap_Add()
            {
                var map = ImTools.ImHashMap<Type, string>.Empty;

                foreach (var key in _types)
                    map = map.AddSureNotPresent(key.GetHashCode(), key, "a");

                return map.AddSureNotPresent(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!");
            }

            // [Benchmark]
            public ImTools.ImHashMap<int, KeyValuePair<Type, string>> V3_ImMap_AddOrUpdate()
            {
                var map = ImTools.ImHashMap<int, KeyValuePair<Type, string>>.Empty;

                foreach (var key in _types)
                    map = map.AddOrUpdate(key.GetHashCode(), new KeyValuePair<Type, string>(key, "a"));

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), new KeyValuePair<Type, string>(typeof(ImHashMapBenchmarks), "!"));
            }

            // [Benchmark]
            public ImTools.ImHashMap<Type, string>[] V4_PartitionedHashMap_AddOrUpdate()
            {
                var map = ImTools.PartitionedHashMap.CreateEmpty<Type, string>();

                foreach (var key in _types)
                    map.AddOrUpdate(key, "a");

                map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            [Benchmark]
            public ImToolsV3.ImHashMap<Type, string> V3_ImHashMap_AddOrUpdate()
            {
                var map = ImToolsV3.ImHashMap<Type, string>.Empty;

                foreach (var key in _types)
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!");
            }

            // [Benchmark]
            public ImToolsV3.ImHashMap<Type, string>[] V3_PartitionedHashMap_AddOrUpdate()
            {
                var map = ImToolsV3.PartitionedHashMap.CreateEmpty<Type, string>();

                foreach (var key in _types)
                    map.AddOrUpdate(key, "a");

                map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            // [Benchmark]
            public ImTools.V2.ImHashMap<Type, string> V2_ImHashMap_AddOrUpdate()
            {
                var map = ImTools.V2.ImHashMap<Type, string>.Empty;

                foreach (var key in _types)
                    map = map.AddOrUpdate(key, "a");

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!");
            }

            // [Benchmark]
            public ImTools.V2.ImHashMap<Type, string>[] ImHashMapSlots32_AddOrUpdate()
            {
                var map = ImTools.V2.ImHashMapSlots.CreateWithEmpty<Type, string>();

                foreach (var key in _types)
                    map.AddOrUpdate(key, "a");

                map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            // [Benchmark]
            public ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>> V2_ImHashMap_AVLOptimizedForAdd_AddOrUpdate()
            {
                var map = ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>>.Empty;

                foreach (var key in _types)
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!");
            }

            // [Benchmark]
            public ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>>[] Experimental_ImHashMapSlots32_AddOrUpdate()
            {
                var map = ImTools.V2.Experimental.ImMapSlots.CreateWithEmpty<ImTools.V2.Experimental.ImMap.KValue<Type>>();

                foreach (var key in _types)
                    map.AddOrUpdate(key.GetHashCode(), new ImTools.V2.Experimental.ImMap.KValue<Type>(key, "a"));

                map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), new ImTools.V2.Experimental.ImMap.KValue<Type>(typeof(ImHashMapBenchmarks), "!"));
                return map;
            }

            // [Benchmark]
            public ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>>[] Experimental_ImHashMapSlots64_AddOrUpdate()
            {
                var map = ImTools.V2.Experimental.ImMapSlots.CreateWithEmpty<ImTools.V2.Experimental.ImMap.KValue<Type>>(64);

                foreach (var key in _types)
                    map.AddOrUpdate(key.GetHashCode(), new ImTools.V2.Experimental.ImMap.KValue<Type>(key, "a"), 63);

                map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), new ImTools.V2.Experimental.ImMap.KValue<Type>(typeof(ImHashMapBenchmarks), "!"), 63);
                return map;
            }

            // [Benchmark]
            public DictionarySlim<TypeVal, string> DictSlim_TryAdd()
            {
                var map = new DictionarySlim<TypeVal, string>();

                foreach (var key in _types)
                    map.GetOrAddValueRef(key) = "a";

                map.GetOrAddValueRef(typeof(ImHashMapBenchmarks)) = "!";
                return map;
            }

            // [Benchmark]
            public Dictionary<Type, string> Dict_TryAdd()
            {
                var map = new Dictionary<Type, string>();

                foreach (var key in _types)
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            // [Benchmark]
            public ConcurrentDictionary<Type, string> ConcurrentDictionary_TryAdd()
            {
                var map = new ConcurrentDictionary<Type, string>();

                foreach (var key in _types)
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            // [Benchmark]
            public ImmutableDictionary<Type, string> ImmutableDict_Builder_Add()
            {
                var builder = ImmutableDictionary.CreateBuilder<Type, string>();

                foreach (var key in _types)
                    builder.Add(key, "a");
                builder.Add(typeof(ImHashMapBenchmarks), "!");
                return builder.ToImmutable();
            }

            // [Benchmark]
            public ImmutableDictionary<Type, string> ImmutableDict_Add()
            {
                var map = ImmutableDictionary<Type, string>.Empty;

                foreach (var key in _types)
                    map = map.Add(key, "a");

                return map.Add(typeof(ImHashMapBenchmarks), "!");
            }

            // [Benchmark]
            public TypeDictionary<string> TypeDictionary_Add()
            {
                var map = new TypeDictionary<string>();

                map.Add<A1>("a");
                map.Add<A2>("a");
                map.Add<A3>("a");
                map.Add<A4>("a");
                map.Add<A5>("a");
                // map.Add<A6>("a");
                // map.Add<A7>("a");
                // map.Add<A8>("a");
                // map.Add<A9>("a");
                // map.Add<A10>("a");
                // map.Add<B1>("a");
                // map.Add<B2>("a");
                // map.Add<B3>("a");
                // map.Add<B4>("a");
                // map.Add<B5>("a");
                // map.Add<B6>("a");
                // map.Add<B7>("a");
                // map.Add<B8>("a");
                // map.Add<B9>("a");
                // map.Add<B10>("a");

                map.Add<ImHashMapBenchmarks>("!");

                return map;
            }
        }

        class A1 {}
        class A2 {}
        class A3 {}
        class A4 {}
        class A5 {}
        class A6 {}
        class A7 {}
        class A8 {}
        class A9 {}
        class A10 {}
        class B1 {}
        class B2 {}
        class B3 {}
        class B4 {}
        class B5 {}
        class B6 {}
        class B7 {}
        class B8 {}
        class B9 {}
        class B10 {}

        public class TypeDictionary<TValue>
        {
            // ReSharper disable once StaticMemberInGenericType
            private static int typeIndex;
            private readonly object _lockObject = new object();

            private TValue[] _values = new TValue[100];

            public void Add<TKey>(TValue value)
            {
                lock (_lockObject)
                {
                    var id = TypeKey<TKey>.Id;
                    if (id >= _values.Length) 
                        Array.Resize(ref _values, id * 2);
                    _values[id] = value;
                }
            }

            public TValue Get<TKey>()
            {
                var id = TypeKey<TKey>.Id;
                return id >= _values.Length ? default : _values[id];
            }

            // ReSharper disable once UnusedTypeParameter
            private static class TypeKey<TKey>
            {
                // ReSharper disable once StaticMemberInGenericType
                internal static readonly int Id = Interlocked.Increment(ref typeIndex);
            }
        }

        [MemoryDiagnoser]
        public class Lookup
        {
            /*
## 21.01.2019: All versions.

               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 | 13.74 ns | 0.0686 ns | 0.0642 ns |  0.79 |           - |           - |           - |                   - |
    GetValueOrDefault | 17.43 ns | 0.0924 ns | 0.0864 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 | 19.15 ns | 0.0786 ns | 0.0656 ns |  1.10 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 | 25.73 ns | 0.0711 ns | 0.0665 ns |  1.48 |           - |           - |           - |                   - |

## For some reason dropping lookup speed with only changes to AddOrUpdate

               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 | 13.89 ns | 0.0938 ns | 0.0877 ns |  0.80 |           - |           - |           - |                   - |
    GetValueOrDefault | 17.40 ns | 0.0888 ns | 0.0831 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 | 19.04 ns | 0.0712 ns | 0.0666 ns |  1.09 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 | 25.93 ns | 0.0474 ns | 0.0420 ns |  1.49 |           - |           - |           - |                   - |

## Got back some perf by moving GetValueOrDefault to static method and specializing for Type

               Method |     Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |---------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 | 13.87 ns | 0.0400 ns | 0.0355 ns |  0.85 |           - |           - |           - |                   - |
    GetValueOrDefault | 16.34 ns | 0.0932 ns | 0.0826 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 | 19.18 ns | 0.0460 ns | 0.0430 ns |  1.17 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 | 25.96 ns | 0.0756 ns | 0.0707 ns |  1.59 |           - |           - |           - |                   - |

## Benchmark against variety of inputs on par with Populate benchmark

               Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 |     5 |  6.155 ns | 0.0321 ns | 0.0301 ns |  0.98 |    0.01 |           - |           - |           - |                   - |
    GetValueOrDefault |     5 |  6.267 ns | 0.0510 ns | 0.0452 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |     5 |  7.439 ns | 0.0763 ns | 0.0676 ns |  1.19 |    0.02 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |     5 |  9.558 ns | 0.0409 ns | 0.0383 ns |  1.52 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
 GetValueOrDefault_v1 |    40 | 10.897 ns | 0.0673 ns | 0.0629 ns |  0.95 |    0.01 |           - |           - |           - |                   - |
    GetValueOrDefault |    40 | 11.467 ns | 0.0325 ns | 0.0304 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |    40 | 14.012 ns | 0.1092 ns | 0.1022 ns |  1.22 |    0.01 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |    40 | 19.945 ns | 0.1032 ns | 0.0965 ns |  1.74 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
 GetValueOrDefault_v1 |   200 | 13.664 ns | 0.0291 ns | 0.0258 ns |  0.97 |    0.00 |           - |           - |           - |                   - |
    GetValueOrDefault |   200 | 14.051 ns | 0.0524 ns | 0.0491 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |   200 | 16.722 ns | 0.0568 ns | 0.0531 ns |  1.19 |    0.01 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |   200 | 24.473 ns | 0.0792 ns | 0.0702 ns |  1.74 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
 GetValueOrDefault_v1 |  1000 | 14.213 ns | 0.1528 ns | 0.1354 ns |  0.96 |    0.01 |           - |           - |           - |                   - |
    GetValueOrDefault |  1000 | 14.805 ns | 0.0518 ns | 0.0485 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v2 |  1000 | 16.645 ns | 0.0447 ns | 0.0419 ns |  1.12 |    0.01 |           - |           - |           - |                   - |
 GetValueOrDefault_v3 |  1000 | 27.489 ns | 0.0890 ns | 0.0832 ns |  1.86 |    0.01 |           - |           - |           - |                   - |

## Adding aggressive inlining to the Data { Hash, Key, Value } properties

               Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    GetValueOrDefault |     5 |  5.853 ns | 0.0685 ns | 0.0607 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |     5 |  5.913 ns | 0.0373 ns | 0.0349 ns |  1.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |    40 |  9.649 ns | 0.0235 ns | 0.0220 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 10.266 ns | 0.0236 ns | 0.0221 ns |  1.06 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |   200 | 11.613 ns | 0.0554 ns | 0.0491 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 12.052 ns | 0.0555 ns | 0.0520 ns |  1.04 |           - |           - |           - |                   - |

## Using  `!= Empty` instead of `.Height != 0` drops some perf

               Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
 GetValueOrDefault_v1 |     5 |  5.933 ns | 0.0310 ns | 0.0290 ns |  0.93 |    0.03 |           - |           - |           - |                   - |
    GetValueOrDefault |     5 |  6.386 ns | 0.1807 ns | 0.1602 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
    GetValueOrDefault |    40 |  9.820 ns | 0.0521 ns | 0.0488 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 10.257 ns | 0.0300 ns | 0.0266 ns |  1.05 |    0.01 |           - |           - |           - |                   - |
                      |       |           |           |           |       |         |             |             |             |                     |
    GetValueOrDefault |   200 | 11.717 ns | 0.0689 ns | 0.0644 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 12.104 ns | 0.0548 ns | 0.0486 ns |  1.03 |    0.01 |           - |           - |           - |                   - |

## Removing `.Height != 0` check completely did not change much, but let it stay cause less code is better

               Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    GetValueOrDefault |     5 |  5.903 ns | 0.0612 ns | 0.0573 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |     5 |  5.931 ns | 0.0503 ns | 0.0470 ns |  1.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |    40 |  9.636 ns | 0.0419 ns | 0.0392 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 10.231 ns | 0.0333 ns | 0.0312 ns |  1.06 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |   200 | 11.637 ns | 0.0721 ns | 0.0602 ns |  1.00 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 12.042 ns | 0.0607 ns | 0.0568 ns |  1.03 |           - |           - |           - |                   - |

## TryFind

             Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
        ----------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
         TryFind_v1 |     5 |  5.229 ns | 0.0307 ns | 0.0257 ns |  1.00 |           - |           - |           - |                   - |
            TryFind |     5 |  6.766 ns | 0.0695 ns | 0.0650 ns |  1.30 |           - |           - |           - |                   - |
                    |       |           |           |           |       |             |             |             |                     |
         TryFind_v1 |    40 |  9.268 ns | 0.0116 ns | 0.0108 ns |  1.00 |           - |           - |           - |                   - |
            TryFind |    40 |  9.755 ns | 0.0219 ns | 0.0205 ns |  1.05 |           - |           - |           - |                   - |
                    |       |           |           |           |       |             |             |             |                     |
         TryFind_v1 |   200 | 11.773 ns | 0.0558 ns | 0.0494 ns |  1.00 |           - |           - |           - |                   - |
            TryFind |   200 | 12.212 ns | 0.0456 ns | 0.0380 ns |  1.04 |           - |           - |           - |                   - |

     Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
----------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    TryFind |     5 |  5.906 ns | 0.0191 ns | 0.0178 ns |  0.97 |           - |           - |           - |                   - |
 TryFind_v1 |     5 |  6.079 ns | 0.0947 ns | 0.0839 ns |  1.00 |           - |           - |           - |                   - |
            |       |           |           |           |       |             |             |             |                     |
    TryFind |    40 |  9.211 ns | 0.0214 ns | 0.0200 ns |  0.87 |           - |           - |           - |                   - |
 TryFind_v1 |    40 | 10.566 ns | 0.0149 ns | 0.0132 ns |  1.00 |           - |           - |           - |                   - |
            |       |           |           |           |       |             |             |             |                     |
    TryFind |   200 | 11.400 ns | 0.1152 ns | 0.1078 ns |  0.88 |           - |           - |           - |                   - |
 TryFind_v1 |   200 | 12.929 ns | 0.0712 ns | 0.0666 ns |  1.00 |           - |           - |           - |                   - |

    ## GetOrDefault a bit optimized
    
               Method | Count |      Mean |     Error |    StdDev | Ratio | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------- |------ |----------:|----------:|----------:|------:|------------:|------------:|------------:|--------------------:|
    GetValueOrDefault |     5 |  6.782 ns | 0.0449 ns | 0.0398 ns |  0.96 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |     5 |  7.042 ns | 0.0659 ns | 0.0616 ns |  1.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |    40 | 10.962 ns | 0.0866 ns | 0.0768 ns |  0.99 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |    40 | 11.094 ns | 0.0973 ns | 0.0813 ns |  1.00 |           - |           - |           - |                   - |
                      |       |           |           |           |       |             |             |             |                     |
    GetValueOrDefault |   200 | 13.329 ns | 0.0338 ns | 0.0299 ns |  0.97 |           - |           - |           - |                   - |
 GetValueOrDefault_v1 |   200 | 13.722 ns | 0.0537 ns | 0.0448 ns |  1.00 |           - |           - |           - |                   - |

    ## The whole result for the docs

                Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
---------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
            TryFind_v1 |    10 |  7.274 ns | 0.0410 ns | 0.0384 ns |  0.98 |    0.01 |           - |           - |           - |                   - |
               TryFind |    10 |  7.422 ns | 0.0237 ns | 0.0222 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |    10 | 21.664 ns | 0.0213 ns | 0.0189 ns |  2.92 |    0.01 |           - |           - |           - |                   - |
  ImmutableDict_TryGet |    10 | 71.199 ns | 0.1312 ns | 0.1228 ns |  9.59 |    0.03 |           - |           - |           - |                   - |
                       |       |           |           |           |       |         |             |             |             |                     |
            TryFind_v1 |   100 |  8.426 ns | 0.0236 ns | 0.0221 ns |  0.91 |    0.00 |           - |           - |           - |                   - |
               TryFind |   100 |  9.304 ns | 0.0305 ns | 0.0270 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |   100 | 21.791 ns | 0.1072 ns | 0.0951 ns |  2.34 |    0.01 |           - |           - |           - |                   - |
  ImmutableDict_TryGet |   100 | 74.985 ns | 0.1053 ns | 0.0879 ns |  8.06 |    0.03 |           - |           - |           - |                   - |
                       |       |           |           |           |       |         |             |             |             |                     |
               TryFind |  1000 | 13.837 ns | 0.0291 ns | 0.0272 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
            TryFind_v1 |  1000 | 16.108 ns | 0.0415 ns | 0.0367 ns |  1.16 |    0.00 |           - |           - |           - |                   - |
 ConcurrentDict_TryGet |  1000 | 21.876 ns | 0.0325 ns | 0.0288 ns |  1.58 |    0.00 |           - |           - |           - |                   - |
  ImmutableDict_TryGet |  1000 | 83.563 ns | 0.1046 ns | 0.0873 ns |  6.04 |    0.01 |           - |           - |           - |                   - |


    ## 2019-03-28: Comparing vs `Dictionary<K, V>`:

                           Method | Count |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------------------- |------ |----------:|----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
                          TryFind |    10 |  7.722 ns | 0.0451 ns | 0.0422 ns |  7.718 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |    10 | 18.475 ns | 0.0502 ns | 0.0470 ns | 18.470 ns |  2.39 |    0.01 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |    10 | 22.661 ns | 0.0463 ns | 0.0433 ns | 22.653 ns |  2.93 |    0.02 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |    10 | 72.911 ns | 1.5234 ns | 2.1355 ns | 74.134 ns |  9.25 |    0.23 |           - |           - |           - |                   - |
                                  |       |           |           |           |           |       |         |             |             |             |                     |
                          TryFind |   100 |  9.987 ns | 0.0543 ns | 0.0508 ns |  9.978 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |   100 | 18.110 ns | 0.0644 ns | 0.0602 ns | 18.088 ns |  1.81 |    0.01 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |   100 | 24.402 ns | 0.0978 ns | 0.0915 ns | 24.435 ns |  2.44 |    0.02 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |   100 | 76.689 ns | 0.3632 ns | 0.3397 ns | 76.704 ns |  7.68 |    0.04 |           - |           - |           - |                   - |
                                  |       |           |           |           |           |       |         |             |             |             |                     |
                          TryFind |  1000 | 12.600 ns | 0.1506 ns | 0.1335 ns | 12.551 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |  1000 | 19.023 ns | 0.0575 ns | 0.0538 ns | 19.036 ns |  1.51 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |  1000 | 22.651 ns | 0.1238 ns | 0.1097 ns | 22.613 ns |  1.80 |    0.02 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |  1000 | 83.608 ns | 0.3105 ns | 0.2904 ns | 83.612 ns |  6.64 |    0.07 |           - |           - |           - |                   - |

    ## 2019-03-29: Comparing vs `DictionarySlim<K, V>`:

                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
       DictionarySlim_TryGetValue |    10 |  8.228 ns | 0.0682 ns | 0.0604 ns |  1.00 |    0.01 |           - |           - |           - |                   - |
                          TryFind |    10 |  8.257 ns | 0.0796 ns | 0.0706 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |    10 | 19.615 ns | 0.0251 ns | 0.0209 ns |  2.38 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |    10 | 22.339 ns | 0.0922 ns | 0.0863 ns |  2.71 |    0.03 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |    10 | 69.872 ns | 0.2699 ns | 0.2524 ns |  8.46 |    0.07 |           - |           - |           - |                   - |
                                  |       |           |           |           |       |         |             |             |             |                     |
       DictionarySlim_TryGetValue |   100 |  8.351 ns | 0.1613 ns | 0.1508 ns |  0.69 |    0.01 |           - |           - |           - |                   - |
                          TryFind |   100 | 12.144 ns | 0.0570 ns | 0.0533 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |   100 | 17.985 ns | 0.0880 ns | 0.0823 ns |  1.48 |    0.01 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |   100 | 22.312 ns | 0.0564 ns | 0.0471 ns |  1.84 |    0.01 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |   100 | 75.374 ns | 0.3042 ns | 0.2846 ns |  6.21 |    0.04 |           - |           - |           - |                   - |
                                  |       |           |           |           |       |         |             |             |             |                     |
       DictionarySlim_TryGetValue |  1000 |  8.202 ns | 0.0713 ns | 0.0667 ns |  0.55 |    0.01 |           - |           - |           - |                   - |
                          TryFind |  1000 | 14.919 ns | 0.1101 ns | 0.0919 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
           Dictionary_TryGetValue |  1000 | 18.073 ns | 0.2415 ns | 0.2141 ns |  1.21 |    0.02 |           - |           - |           - |                   - |
 ConcurrentDictionary_TryGetValue |  1000 | 22.406 ns | 0.1039 ns | 0.0921 ns |  1.50 |    0.01 |           - |           - |           - |                   - |
             ImmutableDict_TryGet |  1000 | 84.215 ns | 0.2835 ns | 0.2513 ns |  5.65 |    0.04 |           - |           - |           - |                   - |

## 2019-04-08: Full test

|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|                ImHashMap_TryFind |    10 |  9.072 ns | 0.0301 ns | 0.0282 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |    10 |  8.405 ns | 0.0124 ns | 0.0116 ns |  0.93 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |    10 |  8.199 ns | 0.0118 ns | 0.0105 ns |  0.90 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |    10 | 18.151 ns | 0.0724 ns | 0.0677 ns |  2.00 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |    10 | 22.281 ns | 0.1872 ns | 0.1462 ns |  2.45 |    0.02 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |    10 | 70.143 ns | 0.2833 ns | 0.2650 ns |  7.73 |    0.04 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |   100 | 12.698 ns | 0.0545 ns | 0.0510 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |   100 | 12.440 ns | 0.0145 ns | 0.0129 ns |  0.98 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |   100 |  8.197 ns | 0.0157 ns | 0.0139 ns |  0.65 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |   100 | 18.108 ns | 0.0263 ns | 0.0205 ns |  1.43 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |   100 | 22.834 ns | 0.0627 ns | 0.0524 ns |  1.80 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |   100 | 76.253 ns | 0.2767 ns | 0.2311 ns |  6.00 |    0.03 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |  1000 | 14.960 ns | 0.0457 ns | 0.0427 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |  1000 | 14.614 ns | 0.0508 ns | 0.0451 ns |  0.98 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |  1000 |  8.209 ns | 0.0534 ns | 0.0499 ns |  0.55 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |  1000 | 18.256 ns | 0.0383 ns | 0.0320 ns |  1.22 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |  1000 | 22.261 ns | 0.1509 ns | 0.1411 ns |  1.49 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |  1000 | 83.095 ns | 0.3395 ns | 0.3176 ns |  5.55 |    0.03 |           - |           - |           - |                   - |

## 2019-04-08: Different variants tested

|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|               ImHashMap_TryFind2 |    10 |  7.979 ns | 0.0446 ns | 0.0417 ns |  0.93 |    0.01 |           - |           - |           - |                   - |
|               ImHashMap_TryFind3 |    10 |  7.908 ns | 0.0295 ns | 0.0262 ns |  0.92 |    0.00 |           - |           - |           - |                   - |
|                ImHashMap_TryFind |    10 |  8.608 ns | 0.0186 ns | 0.0174 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |    10 |  8.248 ns | 0.0262 ns | 0.0232 ns |  0.96 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |    10 |  7.212 ns | 0.0194 ns | 0.0181 ns |  0.84 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |    10 | 17.707 ns | 0.0631 ns | 0.0590 ns |  2.06 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |    10 | 22.210 ns | 0.1074 ns | 0.1004 ns |  2.58 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |    10 | 72.093 ns | 0.4162 ns | 0.3893 ns |  8.37 |    0.05 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|               ImHashMap_TryFind2 |   100 | 11.245 ns | 0.0174 ns | 0.0163 ns |  0.92 |    0.00 |           - |           - |           - |                   - |
|               ImHashMap_TryFind3 |   100 | 11.488 ns | 0.0801 ns | 0.0710 ns |  0.94 |    0.01 |           - |           - |           - |                   - |
|                ImHashMap_TryFind |   100 | 12.165 ns | 0.0141 ns | 0.0118 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |   100 | 12.272 ns | 0.0367 ns | 0.0343 ns |  1.01 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |   100 |  7.019 ns | 0.0516 ns | 0.0458 ns |  0.58 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |   100 | 17.825 ns | 0.1278 ns | 0.1196 ns |  1.47 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |   100 | 22.189 ns | 0.1034 ns | 0.0968 ns |  1.82 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |   100 | 75.564 ns | 0.3778 ns | 0.3534 ns |  6.21 |    0.03 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|               ImHashMap_TryFind2 |  1000 | 15.909 ns | 0.0924 ns | 0.0864 ns |  1.07 |    0.01 |           - |           - |           - |                   - |
|               ImHashMap_TryFind3 |  1000 | 13.643 ns | 0.0715 ns | 0.0669 ns |  0.92 |    0.01 |           - |           - |           - |                   - |
|                ImHashMap_TryFind |  1000 | 14.819 ns | 0.0465 ns | 0.0412 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |  1000 | 14.541 ns | 0.0555 ns | 0.0520 ns |  0.98 |    0.01 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |  1000 |  7.678 ns | 0.0341 ns | 0.0302 ns |  0.52 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |  1000 | 17.664 ns | 0.0403 ns | 0.0377 ns |  1.19 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |  1000 | 22.010 ns | 0.0497 ns | 0.0465 ns |  1.48 |    0.00 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |  1000 | 83.661 ns | 0.4033 ns | 0.3772 ns |  5.65 |    0.03 |           - |           - |           - |                   - |

## Selecting the 3rd variant:

|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0/1k Op | Gen 1/1k Op | Gen 2/1k Op | Allocated Memory/Op |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------------:|------------:|------------:|--------------------:|
|                ImHashMap_TryFind |    10 |  8.078 ns | 0.0405 ns | 0.0379 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |    10 |  8.224 ns | 0.0114 ns | 0.0101 ns |  1.02 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |    10 |  7.387 ns | 0.0406 ns | 0.0380 ns |  0.91 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |    10 | 17.917 ns | 0.0429 ns | 0.0401 ns |  2.22 |    0.01 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |    10 | 22.256 ns | 0.0726 ns | 0.0643 ns |  2.75 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |    10 | 70.638 ns | 0.6266 ns | 0.5861 ns |  8.74 |    0.09 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |   100 | 11.577 ns | 0.0168 ns | 0.0141 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |   100 | 12.329 ns | 0.0295 ns | 0.0276 ns |  1.07 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |   100 |  7.410 ns | 0.0398 ns | 0.0353 ns |  0.64 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |   100 | 17.890 ns | 0.0425 ns | 0.0377 ns |  1.55 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |   100 | 22.240 ns | 0.0654 ns | 0.0580 ns |  1.92 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |   100 | 78.697 ns | 1.1242 ns | 1.0516 ns |  6.78 |    0.08 |           - |           - |           - |                   - |
|                                  |       |           |           |           |       |         |             |             |             |                     |
|                ImHashMap_TryFind |  1000 | 13.731 ns | 0.0292 ns | 0.0258 ns |  1.00 |    0.00 |           - |           - |           - |                   - |
|             ImHashMap_TryFind_V1 |  1000 | 14.553 ns | 0.0370 ns | 0.0346 ns |  1.06 |    0.00 |           - |           - |           - |                   - |
|       DictionarySlim_TryGetValue |  1000 |  7.345 ns | 0.0208 ns | 0.0194 ns |  0.53 |    0.00 |           - |           - |           - |                   - |
|           Dictionary_TryGetValue |  1000 | 18.672 ns | 0.0483 ns | 0.0451 ns |  1.36 |    0.00 |           - |           - |           - |                   - |
| ConcurrentDictionary_TryGetValue |  1000 | 22.150 ns | 0.1141 ns | 0.1068 ns |  1.61 |    0.01 |           - |           - |           - |                   - |
|             ImmutableDict_TryGet |  1000 | 82.402 ns | 0.9798 ns | 0.9165 ns |  6.01 |    0.07 |           - |           - |           - |                   - |

## V2:

BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                ImHashMap_TryFind |     1 |  4.755 ns | 0.1690 ns | 0.1878 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_TryFind_V1 |     1 |  3.657 ns | 0.0458 ns | 0.0406 ns |  0.78 |    0.04 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |     1 |  2.518 ns | 0.0149 ns | 0.0132 ns |  0.53 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     1 |  6.804 ns | 0.0272 ns | 0.0254 ns |  1.44 |    0.07 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     1 | 16.495 ns | 0.3948 ns | 0.4993 ns |  3.49 |    0.16 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     1 | 15.812 ns | 0.1016 ns | 0.0951 ns |  3.35 |    0.15 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |     1 | 24.346 ns | 0.1253 ns | 0.1172 ns |  5.16 |    0.23 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|                ImHashMap_TryFind |    10 |  6.080 ns | 0.0235 ns | 0.0208 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_TryFind_V1 |    10 |  6.091 ns | 0.0707 ns | 0.0590 ns |  1.00 |    0.01 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |    10 |  2.517 ns | 0.0206 ns | 0.0193 ns |  0.41 |    0.00 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |    10 |  6.670 ns | 0.0278 ns | 0.0260 ns |  1.10 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    10 | 16.202 ns | 0.0634 ns | 0.0562 ns |  2.66 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    10 | 15.764 ns | 0.0659 ns | 0.0617 ns |  2.59 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |    10 | 26.282 ns | 0.2232 ns | 0.2088 ns |  4.32 |    0.03 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|                ImHashMap_TryFind |   100 |  9.350 ns | 0.0315 ns | 0.0295 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_TryFind_V1 |   100 | 10.752 ns | 0.0199 ns | 0.0166 ns |  1.15 |    0.00 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |   100 |  5.664 ns | 0.0391 ns | 0.0366 ns |  0.61 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |   100 |  6.665 ns | 0.0287 ns | 0.0254 ns |  0.71 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   100 | 17.007 ns | 0.0615 ns | 0.0576 ns |  1.82 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   100 | 16.024 ns | 0.3340 ns | 0.3124 ns |  1.71 |    0.03 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |   100 | 30.667 ns | 0.1026 ns | 0.0960 ns |  3.28 |    0.02 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|                ImHashMap_TryFind |  1000 | 12.729 ns | 0.0393 ns | 0.0368 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             ImHashMap_TryFind_V1 |  1000 | 13.352 ns | 0.0638 ns | 0.0597 ns |  1.05 |    0.00 |     - |     - |     - |         - |
|           ImHashMapSlots_TryFind |  1000 |  7.131 ns | 0.0246 ns | 0.0230 ns |  0.56 |    0.00 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |  1000 |  6.686 ns | 0.0317 ns | 0.0297 ns |  0.53 |    0.00 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  1000 | 16.848 ns | 0.0593 ns | 0.0526 ns |  1.32 |    0.01 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  1000 | 15.684 ns | 0.0695 ns | 0.0650 ns |  1.23 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |  1000 | 33.579 ns | 0.1077 ns | 0.0955 ns |  2.64 |    0.01 |     - |     - |     - |         - |


BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


|                                Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                     ImHashMap_TryFind |     1 |  4.365 ns | 0.0198 ns | 0.0185 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|              ImHashMapSlots32_TryFind |     1 |  2.400 ns | 0.0229 ns | 0.0214 ns |  0.55 |    0.00 |     - |     - |     - |         - |
|                  ImHashMap_TryFind_V1 |     1 |  3.385 ns | 0.0071 ns | 0.0063 ns |  0.78 |    0.00 |     - |     - |     - |         - |
|        Experimental_ImHashMap_TryFind |     1 |  4.892 ns | 0.0134 ns | 0.0118 ns |  1.12 |    0.01 |     - |     - |     - |         - |
| Experimental_ImHashMapSlots32_TryFind |     1 |  6.268 ns | 0.0455 ns | 0.0425 ns |  1.44 |    0.01 |     - |     - |     - |         - |
| Experimental_ImHashMapSlots64_TryFind |     1 |  6.175 ns | 0.0335 ns | 0.0313 ns |  1.41 |    0.01 |     - |     - |     - |         - |
|            DictionarySlim_TryGetValue |     1 |  6.190 ns | 0.0524 ns | 0.0490 ns |  1.42 |    0.02 |     - |     - |     - |         - |
|                Dictionary_TryGetValue |     1 | 15.425 ns | 0.0636 ns | 0.0595 ns |  3.53 |    0.02 |     - |     - |     - |         - |
|      ConcurrentDictionary_TryGetValue |     1 | 14.754 ns | 0.0807 ns | 0.0716 ns |  3.38 |    0.02 |     - |     - |     - |         - |
|                  ImmutableDict_TryGet |     1 | 23.449 ns | 0.1027 ns | 0.0960 ns |  5.37 |    0.04 |     - |     - |     - |         - |
|                                       |       |           |           |           |       |         |       |       |       |           |
|                     ImHashMap_TryFind |    10 |  5.449 ns | 0.0216 ns | 0.0202 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|              ImHashMapSlots32_TryFind |    10 |  2.911 ns | 0.0089 ns | 0.0083 ns |  0.53 |    0.00 |     - |     - |     - |         - |
|                  ImHashMap_TryFind_V1 |    10 |  6.019 ns | 0.0129 ns | 0.0115 ns |  1.10 |    0.00 |     - |     - |     - |         - |
|        Experimental_ImHashMap_TryFind |    10 |  8.060 ns | 0.0239 ns | 0.0224 ns |  1.48 |    0.01 |     - |     - |     - |         - |
| Experimental_ImHashMapSlots32_TryFind |    10 |  5.605 ns | 0.0261 ns | 0.0244 ns |  1.03 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMapSlots64_TryFind |    10 |  5.504 ns | 0.0456 ns | 0.0404 ns |  1.01 |    0.01 |     - |     - |     - |         - |
|            DictionarySlim_TryGetValue |    10 |  5.216 ns | 0.0252 ns | 0.0236 ns |  0.96 |    0.01 |     - |     - |     - |         - |
|                Dictionary_TryGetValue |    10 | 15.346 ns | 0.0732 ns | 0.0685 ns |  2.82 |    0.02 |     - |     - |     - |         - |
|      ConcurrentDictionary_TryGetValue |    10 | 14.870 ns | 0.1094 ns | 0.1023 ns |  2.73 |    0.02 |     - |     - |     - |         - |
|                  ImmutableDict_TryGet |    10 | 24.888 ns | 0.0798 ns | 0.0746 ns |  4.57 |    0.02 |     - |     - |     - |         - |
|                                       |       |           |           |           |       |         |       |       |       |           |
|                     ImHashMap_TryFind |   100 |  7.665 ns | 0.0208 ns | 0.0184 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|              ImHashMapSlots32_TryFind |   100 |  5.240 ns | 0.0238 ns | 0.0223 ns |  0.68 |    0.00 |     - |     - |     - |         - |
|                  ImHashMap_TryFind_V1 |   100 |  9.222 ns | 0.0223 ns | 0.0208 ns |  1.20 |    0.00 |     - |     - |     - |         - |
|        Experimental_ImHashMap_TryFind |   100 | 12.248 ns | 0.0584 ns | 0.0546 ns |  1.60 |    0.01 |     - |     - |     - |         - |
| Experimental_ImHashMapSlots32_TryFind |   100 |  7.546 ns | 0.0966 ns | 0.0903 ns |  0.98 |    0.01 |     - |     - |     - |         - |
| Experimental_ImHashMapSlots64_TryFind |   100 |  5.780 ns | 0.0250 ns | 0.0234 ns |  0.75 |    0.00 |     - |     - |     - |         - |
|            DictionarySlim_TryGetValue |   100 |  6.452 ns | 0.0538 ns | 0.0503 ns |  0.84 |    0.01 |     - |     - |     - |         - |
|                Dictionary_TryGetValue |   100 | 15.705 ns | 0.0586 ns | 0.0549 ns |  2.05 |    0.01 |     - |     - |     - |         - |
|      ConcurrentDictionary_TryGetValue |   100 | 15.321 ns | 0.0413 ns | 0.0386 ns |  2.00 |    0.01 |     - |     - |     - |         - |
|                  ImmutableDict_TryGet |   100 | 27.937 ns | 0.1050 ns | 0.0982 ns |  3.64 |    0.02 |     - |     - |     - |         - |
|                                       |       |           |           |           |       |         |       |       |       |           |
|                     ImHashMap_TryFind |  1000 | 11.459 ns | 0.0306 ns | 0.0286 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|              ImHashMapSlots32_TryFind |  1000 |  8.633 ns | 0.0154 ns | 0.0144 ns |  0.75 |    0.00 |     - |     - |     - |         - |
|                  ImHashMap_TryFind_V1 |  1000 | 12.486 ns | 0.0527 ns | 0.0493 ns |  1.09 |    0.00 |     - |     - |     - |         - |
|        Experimental_ImHashMap_TryFind |  1000 | 16.002 ns | 0.0378 ns | 0.0353 ns |  1.40 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMapSlots32_TryFind |  1000 | 11.673 ns | 0.0638 ns | 0.0597 ns |  1.02 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMapSlots64_TryFind |  1000 |  9.817 ns | 0.0123 ns | 0.0115 ns |  0.86 |    0.00 |     - |     - |     - |         - |
|            DictionarySlim_TryGetValue |  1000 |  6.469 ns | 0.0428 ns | 0.0401 ns |  0.56 |    0.00 |     - |     - |     - |         - |
|                Dictionary_TryGetValue |  1000 | 19.170 ns | 0.0708 ns | 0.0628 ns |  1.67 |    0.01 |     - |     - |     - |         - |
|      ConcurrentDictionary_TryGetValue |  1000 | 16.273 ns | 0.4046 ns | 0.5116 ns |  1.43 |    0.05 |     - |     - |     - |         - |
|                  ImmutableDict_TryGet |  1000 | 29.662 ns | 0.1529 ns | 0.1355 ns |  2.59 |    0.01 |     - |     - |     - |         - |

## V3 - baseline

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.572 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.1.403
  [Host]     : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT
  DefaultJob : .NET Core 3.1.9 (CoreCLR 4.700.20.47201, CoreFX 4.700.20.47203), X64 RyuJIT

## v3 - baseline

|                         Method | Count |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------- |------ |----------:|----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|              ImHashMap_TryFind |     1 |  4.614 ns | 0.1073 ns | 0.0951 ns |  4.589 ns |  1.00 |    0.00 |     - |     - |     - |         - |    
| Experimental_ImHashMap_TryFind |     1 |  7.398 ns | 0.0926 ns | 0.0821 ns |  7.413 ns |  1.60 |    0.04 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |     1 |  4.912 ns | 0.0595 ns | 0.0528 ns |  4.903 ns |  1.07 |    0.03 |     - |     - |     - |         - |
|                                |       |           |           |           |           |       |         |       |       |       |           |    
|              ImHashMap_TryFind |    10 |  8.205 ns | 0.0765 ns | 0.0639 ns |  8.186 ns |  1.00 |    0.00 |     - |     - |     - |         - |    
| Experimental_ImHashMap_TryFind |    10 | 10.689 ns | 0.2738 ns | 0.2561 ns | 10.684 ns |  1.31 |    0.03 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |    10 |  6.418 ns | 0.0731 ns | 0.0611 ns |  6.412 ns |  0.78 |    0.01 |     - |     - |     - |         - |    
|                                |       |           |           |           |           |       |         |       |       |       |           |    
|              ImHashMap_TryFind |   100 | 10.398 ns | 0.0434 ns | 0.0385 ns | 10.391 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |   100 | 13.982 ns | 0.2432 ns | 0.2275 ns | 13.946 ns |  1.35 |    0.02 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |   100 | 10.759 ns | 0.1757 ns | 0.1467 ns | 10.761 ns |  1.03 |    0.01 |     - |     - |     - |         - |    
|                                |       |           |           |           |           |       |         |       |       |       |           |    
|              ImHashMap_TryFind |  1000 | 14.397 ns | 0.2039 ns | 0.1907 ns | 14.369 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |  1000 | 30.810 ns | 1.0842 ns | 2.9681 ns | 29.878 ns |  2.25 |    0.38 |     - |     - |     - |         - |    
|           ImHashMap234_TryFind |  1000 | 19.953 ns | 0.4996 ns | 0.4429 ns | 19.772 ns |  1.39 |    0.03 |     - |     - |     - |         - |    

### Leaf3Plus1

|                         Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|              ImHashMap_TryFind |     1 |  5.611 ns | 0.1067 ns | 0.0998 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |     1 |  6.348 ns | 0.2010 ns | 0.2150 ns |  1.14 |    0.05 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |     1 |  4.572 ns | 0.0966 ns | 0.0904 ns |  0.82 |    0.02 |     - |     - |     - |         - |
|                                |       |           |           |           |       |         |       |       |       |           |
|              ImHashMap_TryFind |    10 |  7.582 ns | 0.0703 ns | 0.0587 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |    10 | 10.133 ns | 0.1315 ns | 0.1098 ns |  1.34 |    0.02 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |    10 |  5.763 ns | 0.0670 ns | 0.0594 ns |  0.76 |    0.01 |     - |     - |     - |         - |
|                                |       |           |           |           |       |         |       |       |       |           |
|              ImHashMap_TryFind |   100 | 10.642 ns | 0.2276 ns | 0.1901 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |   100 | 13.526 ns | 0.1563 ns | 0.1385 ns |  1.27 |    0.03 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |   100 |  9.271 ns | 0.1695 ns | 0.1503 ns |  0.87 |    0.02 |     - |     - |     - |         - |
|                                |       |           |           |           |       |         |       |       |       |           |
|              ImHashMap_TryFind |  1000 | 14.909 ns | 0.2118 ns | 0.1981 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |  1000 | 37.315 ns | 1.4352 ns | 4.2316 ns |  2.76 |    0.21 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |  1000 | 24.536 ns | 0.6190 ns | 0.7602 ns |  1.66 |    0.06 |     - |     - |     - |         - |

### Leaf5Plus1 + Leaf3Plus1

|                         Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|              ImHashMap_TryFind |     1 |  6.659 ns | 0.1975 ns | 0.1751 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |     1 |  8.220 ns | 0.1876 ns | 0.1842 ns |  1.23 |    0.05 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |     1 |  5.644 ns | 0.1702 ns | 0.1592 ns |  0.85 |    0.03 |     - |     - |     - |         - |
|                                |       |           |           |           |       |         |       |       |       |           |
|              ImHashMap_TryFind |     5 |  7.223 ns | 0.2332 ns | 0.2068 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |     5 |  9.832 ns | 0.2499 ns | 0.2337 ns |  1.36 |    0.05 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |     5 |  5.353 ns | 0.1591 ns | 0.1410 ns |  0.74 |    0.04 |     - |     - |     - |         - |
|                                |       |           |           |           |       |         |       |       |       |           |
|              ImHashMap_TryFind |    10 |  8.325 ns | 0.1680 ns | 0.1403 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |    10 | 12.221 ns | 0.3406 ns | 0.5098 ns |  1.50 |    0.08 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |    10 |  7.012 ns | 0.2045 ns | 0.2273 ns |  0.85 |    0.03 |     - |     - |     - |         - |
|                                |       |           |           |           |       |         |       |       |       |           |
|              ImHashMap_TryFind |   100 | 12.118 ns | 0.3031 ns | 0.4629 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |   100 | 18.145 ns | 0.4551 ns | 0.8321 ns |  1.50 |    0.08 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |   100 | 11.908 ns | 0.2829 ns | 0.3679 ns |  0.97 |    0.04 |     - |     - |     - |         - |
|                                |       |           |           |           |       |         |       |       |       |           |
|              ImHashMap_TryFind |  1000 | 17.113 ns | 0.3848 ns | 0.3411 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| Experimental_ImHashMap_TryFind |  1000 | 26.034 ns | 0.6183 ns | 0.8255 ns |  1.52 |    0.07 |     - |     - |     - |         - |
|           ImHashMap234_TryFind |  1000 | 16.927 ns | 0.3743 ns | 0.5828 ns |  0.99 |    0.05 |     - |     - |     - |         - |

### V3 candidate

|                                   Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|                 V2_ImHashMap_AVL_TryFind |     1 |  6.084 ns | 0.1295 ns | 0.1082 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  V2_ImHashMap_AVLOptimizedForAdd_TryFind |     1 |  7.319 ns | 0.2009 ns | 0.1879 ns |  1.20 |    0.03 |     - |     - |     - |         - |
|             V3_ImHashMap_23Tree_TryFind |     1 |  5.372 ns | 0.1142 ns | 0.0954 ns |  0.88 |    0.02 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_23Tree_TryFind |     1 |  6.578 ns | 0.1517 ns | 0.1345 ns |  1.08 |    0.03 |     - |     - |     - |         - |
|                                          |       |           |           |           |       |         |       |       |       |           |
|                 V2_ImHashMap_AVL_TryFind |     5 |  6.867 ns | 0.1035 ns | 0.0918 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  V2_ImHashMap_AVLOptimizedForAdd_TryFind |     5 |  9.599 ns | 0.1509 ns | 0.1411 ns |  1.40 |    0.03 |     - |     - |     - |         - |
|             V3_ImHashMap_23Tree_TryFind |     5 |  5.319 ns | 0.1028 ns | 0.1142 ns |  0.77 |    0.02 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_23Tree_TryFind |     5 |  6.550 ns | 0.0856 ns | 0.0801 ns |  0.95 |    0.01 |     - |     - |     - |         - |
|                                          |       |           |           |           |       |         |       |       |       |           |
|                 V2_ImHashMap_AVL_TryFind |    10 |  7.646 ns | 0.0905 ns | 0.0802 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  V2_ImHashMap_AVLOptimizedForAdd_TryFind |    10 | 11.002 ns | 0.2011 ns | 0.1881 ns |  1.44 |    0.03 |     - |     - |     - |         - |
|             V3_ImHashMap_23Tree_TryFind |    10 |  6.986 ns | 0.2160 ns | 0.2122 ns |  0.91 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_23Tree_TryFind |    10 |  6.714 ns | 0.2153 ns | 0.1798 ns |  0.88 |    0.03 |     - |     - |     - |         - |
|                                          |       |           |           |           |       |         |       |       |       |           |
|                 V2_ImHashMap_AVL_TryFind |   100 | 12.413 ns | 0.3100 ns | 0.2748 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  V2_ImHashMap_AVLOptimizedForAdd_TryFind |   100 | 15.134 ns | 0.2375 ns | 0.2917 ns |  1.23 |    0.04 |     - |     - |     - |         - |
|             V3_ImHashMap_23Tree_TryFind |   100 | 11.147 ns | 0.2990 ns | 0.3443 ns |  0.90 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_23Tree_TryFind |   100 |  8.245 ns | 0.1473 ns | 0.1150 ns |  0.67 |    0.01 |     - |     - |     - |         - |
|                                          |       |           |           |           |       |         |       |       |       |           |
|                 V2_ImHashMap_AVL_TryFind |  1000 | 16.528 ns | 0.1799 ns | 0.1502 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|  V2_ImHashMap_AVLOptimizedForAdd_TryFind |  1000 | 22.341 ns | 0.2584 ns | 0.2291 ns |  1.35 |    0.02 |     - |     - |     - |         - |
|             V3_ImHashMap_23Tree_TryFind |  1000 | 13.178 ns | 0.2153 ns | 0.2014 ns |  0.80 |    0.01 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_23Tree_TryFind |  1000 | 11.198 ns | 0.2784 ns | 0.3094 ns |  0.68 |    0.02 |     - |     - |     - |         - |


|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|     V3_ImHashMap_23Tree_TryFind |     1 |  4.467 ns | 0.0977 ns | 0.0816 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     1 | 19.103 ns | 0.2302 ns | 0.2040 ns |  4.28 |    0.08 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|     V3_ImHashMap_23Tree_TryFind |     5 |  4.588 ns | 0.1057 ns | 0.0937 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     5 | 19.000 ns | 0.1738 ns | 0.1625 ns |  4.15 |    0.09 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|     V3_ImHashMap_23Tree_TryFind |    10 |  5.948 ns | 0.1194 ns | 0.1116 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    10 | 19.598 ns | 0.4600 ns | 0.4303 ns |  3.30 |    0.11 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|     V3_ImHashMap_23Tree_TryFind |   100 |  9.838 ns | 0.1607 ns | 0.1342 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   100 | 20.336 ns | 0.3591 ns | 0.2998 ns |  2.07 |    0.03 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|     V3_ImHashMap_23Tree_TryFind |  1000 | 11.980 ns | 0.2353 ns | 0.2201 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  1000 | 19.939 ns | 0.4273 ns | 0.8824 ns |  1.73 |    0.10 |     - |     - |     - |         - |

|                       Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
| V3_ImHashMap_23Tree_TryFind |     1 |  4.647 ns | 0.0516 ns | 0.0431 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|         ImmutableDict_TryGet |     1 | 23.396 ns | 0.3703 ns | 0.3092 ns |  5.04 |    0.10 |     - |     - |     - |         - |
|                              |       |           |           |           |       |         |       |       |       |           |
| V3_ImHashMap_23Tree_TryFind |     5 |  5.242 ns | 0.1822 ns | 0.3143 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|         ImmutableDict_TryGet |     5 | 23.739 ns | 0.1941 ns | 0.1816 ns |  4.42 |    0.32 |     - |     - |     - |         - |
|                              |       |           |           |           |       |         |       |       |       |           |
| V3_ImHashMap_23Tree_TryFind |    10 |  5.749 ns | 0.1206 ns | 0.1128 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|         ImmutableDict_TryGet |    10 | 24.769 ns | 0.3152 ns | 0.2949 ns |  4.31 |    0.11 |     - |     - |     - |         - |
|                              |       |           |           |           |       |         |       |       |       |           |
| V3_ImHashMap_23Tree_TryFind |   100 |  8.252 ns | 0.1754 ns | 0.1465 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|         ImmutableDict_TryGet |   100 | 27.288 ns | 0.6118 ns | 0.6009 ns |  3.31 |    0.10 |     - |     - |     - |         - |
|                              |       |           |           |           |       |         |       |       |       |           |
| V3_ImHashMap_23Tree_TryFind |  1000 | 12.053 ns | 0.1955 ns | 0.1829 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|         ImmutableDict_TryGet |  1000 | 30.762 ns | 0.3980 ns | 0.3723 ns |  2.55 |    0.04 |     - |     - |     - |         - |

### Branch3 as two Branch2 -- looks ok

|                       Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|     V2_ImHashMap_AVL_TryFind |     1 |  6.236 ns | 0.2150 ns | 0.3084 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V3_ImHashMap_23Tree_TryFind |     1 |  5.467 ns | 0.1410 ns | 0.1250 ns |  0.85 |    0.05 |     - |     - |     - |         - |
|                              |       |           |           |           |       |         |       |       |       |           |
|     V2_ImHashMap_AVL_TryFind |     5 |  7.390 ns | 0.2037 ns | 0.1701 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V3_ImHashMap_23Tree_TryFind |     5 |  5.552 ns | 0.1412 ns | 0.1252 ns |  0.75 |    0.02 |     - |     - |     - |         - |
|                              |       |           |           |           |       |         |       |       |       |           |
|     V2_ImHashMap_AVL_TryFind |    10 |  9.402 ns | 0.1381 ns | 0.1153 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V3_ImHashMap_23Tree_TryFind |    10 |  6.820 ns | 0.1766 ns | 0.1652 ns |  0.73 |    0.03 |     - |     - |     - |         - |
|                              |       |           |           |           |       |         |       |       |       |           |
|     V2_ImHashMap_AVL_TryFind |   100 | 11.085 ns | 0.2337 ns | 0.2186 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V3_ImHashMap_23Tree_TryFind |   100 | 10.257 ns | 0.2037 ns | 0.1905 ns |  0.93 |    0.02 |     - |     - |     - |         - |
|                              |       |           |           |           |       |         |       |       |       |           |
|     V2_ImHashMap_AVL_TryFind |  1000 | 16.883 ns | 0.2438 ns | 0.2280 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V3_ImHashMap_23Tree_TryFind |  1000 | 15.516 ns | 0.3248 ns | 0.4554 ns |  0.93 |    0.04 |     - |     - |     - |         - |

### V3 RTM

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT


|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|         V2_ImHashMap_AVL_TryFind |     1 |  5.100 ns | 0.1212 ns | 0.1134 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|      V3_ImHashMap_23Tree_TryFind |     1 |  5.539 ns | 0.1676 ns | 0.1568 ns |  1.09 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |     1 |  5.813 ns | 0.1844 ns | 0.1973 ns |  1.13 |    0.04 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     1 |  6.614 ns | 0.0839 ns | 0.0744 ns |  1.29 |    0.03 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     1 | 16.495 ns | 0.1270 ns | 0.1126 ns |  3.23 |    0.06 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     1 | 12.715 ns | 0.1584 ns | 0.1323 ns |  2.49 |    0.05 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |     1 | 22.615 ns | 0.3097 ns | 0.2418 ns |  4.42 |    0.08 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|         V2_ImHashMap_AVL_TryFind |    10 |  6.270 ns | 0.1421 ns | 0.1187 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|      V3_ImHashMap_23Tree_TryFind |    10 |  6.597 ns | 0.1095 ns | 0.1024 ns |  1.06 |    0.02 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |    10 |  5.416 ns | 0.0908 ns | 0.0805 ns |  0.87 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |    10 |  7.543 ns | 0.1274 ns | 0.1064 ns |  1.20 |    0.02 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    10 | 16.907 ns | 0.1892 ns | 0.1769 ns |  2.70 |    0.05 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    10 | 12.694 ns | 0.1740 ns | 0.1453 ns |  2.03 |    0.04 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |    10 | 24.049 ns | 0.3034 ns | 0.2838 ns |  3.84 |    0.07 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|         V2_ImHashMap_AVL_TryFind |   100 |  9.459 ns | 0.2667 ns | 0.2739 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|      V3_ImHashMap_23Tree_TryFind |   100 |  9.526 ns | 0.1814 ns | 0.1697 ns |  1.01 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |   100 |  5.827 ns | 0.0867 ns | 0.0768 ns |  0.62 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |   100 |  6.897 ns | 0.1033 ns | 0.0967 ns |  0.73 |    0.02 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   100 | 18.189 ns | 0.2100 ns | 0.1640 ns |  1.92 |    0.06 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   100 | 13.412 ns | 0.2041 ns | 0.1909 ns |  1.42 |    0.04 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |   100 | 26.023 ns | 0.3854 ns | 0.3417 ns |  2.76 |    0.08 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|         V2_ImHashMap_AVL_TryFind |  1000 | 15.172 ns | 0.2617 ns | 0.2320 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|      V3_ImHashMap_23Tree_TryFind |  1000 | 14.473 ns | 0.2030 ns | 0.1799 ns |  0.95 |    0.01 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |  1000 |  8.182 ns | 0.0809 ns | 0.0756 ns |  0.54 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |  1000 |  6.893 ns | 0.1590 ns | 0.1410 ns |  0.45 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  1000 | 18.230 ns | 0.2012 ns | 0.1882 ns |  1.20 |    0.02 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  1000 | 14.920 ns | 0.2296 ns | 0.2035 ns |  0.98 |    0.01 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |  1000 | 29.555 ns | 0.5926 ns | 0.5543 ns |  1.95 |    0.04 |     - |     - |     - |         - |


## Against static TypeDictionary by @rogeralsing (need to substract the cost of enumerating the array when adding items to ImHashMap and PartitionedHashMap)

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT

|                                  Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|          V3_ImHashMap_GetValueOrDefault |     5 |  4.497 ns | 0.1607 ns | 0.1578 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V3_PartitionedHashMap_GetValueOrDefault |     5 |  4.763 ns | 0.1675 ns | 0.1484 ns |  1.06 |    0.05 |     - |     - |     - |         - |
|                        TypeDict_TryFind |     5 |  3.198 ns | 0.1221 ns | 0.1406 ns |  0.71 |    0.03 |     - |     - |     - |         - |
|                  Dictionary_TryGetValue |     5 | 16.248 ns | 0.3500 ns | 0.3102 ns |  3.61 |    0.12 |     - |     - |     - |         - |

|          V3_ImHashMap_GetValueOrDefault |    10 |  5.569 ns | 0.1801 ns | 0.2751 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V3_PartitionedHashMap_GetValueOrDefault |    10 |  5.016 ns | 0.1509 ns | 0.1178 ns |  0.89 |    0.05 |     - |     - |     - |         - |
|                        TypeDict_TryFind |    10 |  2.513 ns | 0.0824 ns | 0.0688 ns |  0.45 |    0.03 |     - |     - |     - |         - |
|                  Dictionary_TryGetValue |    10 | 15.954 ns | 0.3234 ns | 0.3025 ns |  2.85 |    0.17 |     - |     - |     - |         - |

|          V3_ImHashMap_GetValueOrDefault |    20 |  6.747 ns | 0.1964 ns | 0.2017 ns |  1.00 |    0.00 |     - |     - |     - |         - |
| V3_PartitionedHashMap_GetValueOrDefault |    20 |  3.985 ns | 0.1034 ns | 0.0863 ns |  0.59 |    0.02 |     - |     - |     - |         - |
|                        TypeDict_TryFind |    20 |  2.476 ns | 0.1174 ns | 0.1257 ns |  0.37 |    0.02 |     - |     - |     - |         - |
|                  Dictionary_TryGetValue |    20 | 17.766 ns | 0.4131 ns | 0.3865 ns |  2.64 |    0.11 |     - |     - |     - |         - |


## V4

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                           Method | Count |      Mean |     Error |    StdDev | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------------- |------ |----------:|----------:|----------:|------:|--------:|------:|------:|------:|----------:|
|             V4_ImHashMap_TryFind |     1 |  8.915 ns | 0.1853 ns | 0.1733 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |     1 |  7.834 ns | 0.1769 ns | 0.1568 ns |  0.88 |    0.02 |     - |     - |     - |         - |
|    V4_PartitionedHashMap_TryFind |     1 |  8.292 ns | 0.1082 ns | 0.0959 ns |  0.93 |    0.02 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |     1 |  7.681 ns | 0.1245 ns | 0.1039 ns |  0.86 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |     1 |  8.861 ns | 0.1423 ns | 0.1188 ns |  0.99 |    0.02 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |     1 | 17.914 ns | 0.3447 ns | 0.3055 ns |  2.01 |    0.05 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |     1 | 13.381 ns | 0.2709 ns | 0.2401 ns |  1.50 |    0.04 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |     1 | 19.040 ns | 0.3068 ns | 0.2870 ns |  2.14 |    0.06 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|             V4_ImHashMap_TryFind |    10 |  9.462 ns | 0.2690 ns | 0.2246 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |    10 |  9.038 ns | 0.1650 ns | 0.1463 ns |  0.96 |    0.02 |     - |     - |     - |         - |
|    V4_PartitionedHashMap_TryFind |    10 |  8.575 ns | 0.2031 ns | 0.1900 ns |  0.91 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |    10 |  5.869 ns | 0.1026 ns | 0.1743 ns |  0.63 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |    10 |  7.384 ns | 0.1882 ns | 0.1668 ns |  0.78 |    0.03 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |    10 | 14.082 ns | 0.2168 ns | 0.1921 ns |  1.49 |    0.04 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |    10 | 11.436 ns | 0.1398 ns | 0.1239 ns |  1.21 |    0.03 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |    10 | 17.927 ns | 0.4467 ns | 0.4780 ns |  1.90 |    0.09 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|             V4_ImHashMap_TryFind |   100 | 12.147 ns | 0.1475 ns | 0.1308 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |   100 | 11.357 ns | 0.1695 ns | 0.1323 ns |  0.94 |    0.01 |     - |     - |     - |         - |
|    V4_PartitionedHashMap_TryFind |   100 |  8.520 ns | 0.2142 ns | 0.1899 ns |  0.70 |    0.02 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |   100 |  7.785 ns | 0.1374 ns | 0.1147 ns |  0.64 |    0.01 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |   100 |  6.918 ns | 0.1544 ns | 0.1289 ns |  0.57 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |   100 | 13.804 ns | 0.3474 ns | 0.3717 ns |  1.14 |    0.04 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |   100 | 11.432 ns | 0.2194 ns | 0.2052 ns |  0.94 |    0.02 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |   100 | 21.593 ns | 0.4048 ns | 0.3786 ns |  1.78 |    0.03 |     - |     - |     - |         - |
|                                  |       |           |           |           |       |         |       |       |       |           |
|             V4_ImHashMap_TryFind |  1000 | 15.492 ns | 0.3859 ns | 0.4881 ns |  1.00 |    0.00 |     - |     - |     - |         - |
|             V3_ImHashMap_TryFind |  1000 | 14.339 ns | 0.2612 ns | 0.2444 ns |  0.92 |    0.03 |     - |     - |     - |         - |
|    V4_PartitionedHashMap_TryFind |  1000 | 11.508 ns | 0.1839 ns | 0.1630 ns |  0.74 |    0.03 |     - |     - |     - |         - |
|    V3_PartitionedHashMap_TryFind |  1000 |  9.508 ns | 0.2340 ns | 0.2074 ns |  0.61 |    0.02 |     - |     - |     - |         - |
|       DictionarySlim_TryGetValue |  1000 |  7.099 ns | 0.1984 ns | 0.2037 ns |  0.46 |    0.01 |     - |     - |     - |         - |
|           Dictionary_TryGetValue |  1000 | 13.696 ns | 0.1341 ns | 0.1120 ns |  0.88 |    0.03 |     - |     - |     - |         - |
| ConcurrentDictionary_TryGetValue |  1000 | 11.358 ns | 0.1464 ns | 0.1222 ns |  0.73 |    0.03 |     - |     - |     - |         - |
|             ImmutableDict_TryGet |  1000 | 25.875 ns | 0.3752 ns | 0.3510 ns |  1.67 |    0.06 |     - |     - |     - |         - |

*/
            [Params(1, 10, 100, 1_000)]// the 1000 does not add anything as the LookupKey stored higher in the tree, 1000)]
            // [Params(20)]// the 1000 does not add anything as the LookupKey stored higher in the tree, 1000)]
            public int Count;

            [GlobalSetup]
            public void Populate()
            {
                _mapV4 = V4_ImHashMap_AddOrUpdate();
                _mapV3 = V3_ImHashMap_AddOrUpdate();
                _mapV2 = V2_AddOrUpdate();
                _partMapV4 = V4_PartitionedHashMap_AddOrUpdate();
                _partMapV3 = V3_PartitionedHashMap_AddOrUpdate();
                _typeDict = TypeDictionary_Add();
                _dict = Dict();
                _dictSlim = DictSlim();
                _concurrentDict = ConcurrentDict();
                _immutableDict = ImmutableDict();
            }

            #region Population

            private ImTools.V2.ImHashMap<Type, string> _mapV2;
            public ImTools.V2.ImHashMap<Type, string> V2_AddOrUpdate()
            {
                var map = ImTools.V2.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key, "a");

                map = map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            private ImTools.V2.ImHashMap<Type, string>[] _mapSlots;
            public ImTools.V2.ImHashMap<Type, string>[] ImHashMapSlots_AddOrUpdate()
            {
                var map = ImHashMapSlots.CreateWithEmpty<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.AddOrUpdate(key, "a");

                map.AddOrUpdate(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            private ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>> _mapExp;
            public ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>> Experimental_ImHashMap_AddOrUpdate()
            {
                var map = ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!");
            }

            private ImTools.ImHashMap<Type, string> _mapV4;
            public ImTools.ImHashMap<Type, string> V4_ImHashMap_AddOrUpdate()
            {
                var map = ImTools.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!");
            }

            private ImToolsV3.ImHashMap<Type, string> _mapV3;
            public ImToolsV3.ImHashMap<Type, string> V3_ImHashMap_AddOrUpdate()
            {
                var map = ImToolsV3.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!");
            }

            private ImTools.ImHashMap<Type, string>[] _partMapV4;
            public ImTools.ImHashMap<Type, string>[] V4_PartitionedHashMap_AddOrUpdate()
            {
                var map = ImTools.PartitionedHashMap.CreateEmpty<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.AddOrUpdate(key.GetHashCode(), key, "a");

                map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            private ImToolsV3.ImHashMap<Type, string>[] _partMapV3;
            public ImToolsV3.ImHashMap<Type, string>[] V3_PartitionedHashMap_AddOrUpdate()
            {
                var map = ImToolsV3.PartitionedHashMap.CreateEmpty<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.AddOrUpdate(key.GetHashCode(), key, "a");

                map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            private ImToolsV3.ImHashMap<Type, string>[] _partMap23_32;
            public ImToolsV3.ImHashMap<Type, string>[] V3_PartitionedHashMap32_AddOrUpdate()
            {
                var maps = ImToolsV3.PartitionedHashMap.CreateEmpty<Type, string>(32);

                foreach (var key in _keys.Take(Count))
                    maps.AddOrUpdate(key.GetHashCode(), key, "a", 31);

                maps.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), typeof(ImHashMapBenchmarks), "!", 31);
                return maps;
            }

            public ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>>[] Experimental_ImHashMapSlots32_AddOrUpdate()
            {
                var map = ImTools.V2.Experimental.ImMapSlots.CreateWithEmpty<ImTools.V2.Experimental.ImMap.KValue<Type>>(32);

                foreach (var key in _keys.Take(Count))
                    map.AddOrUpdate(key.GetHashCode(), new ImTools.V2.Experimental.ImMap.KValue<Type>(key, "a"), 31);

                map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), new ImTools.V2.Experimental.ImMap.KValue<Type>(typeof(ImHashMapBenchmarks), "!"), 31);
                return map;
            }
            
            private ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>>[] _mapSlotsExp32;

            public ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>>[] Experimental_ImHashMapSlots64_AddOrUpdate()
            {
                var map = ImTools.V2.Experimental.ImMapSlots.CreateWithEmpty<ImTools.V2.Experimental.ImMap.KValue<Type>>(64);

                foreach (var key in _keys.Take(Count))
                    map.AddOrUpdate(key.GetHashCode(), new ImTools.V2.Experimental.ImMap.KValue<Type>(key, "a"), 63);

                map.AddOrUpdate(typeof(ImHashMapBenchmarks).GetHashCode(), new ImTools.V2.Experimental.ImMap.KValue<Type>(typeof(ImHashMapBenchmarks), "!"), 63);
                return map;
            }

            private ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>>[] _mapSlotsExp64;

            public Dictionary<Type, string> Dict()
            {
                var map = new Dictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");

                return map;
            }

            private Dictionary<Type, string> _dict;

            public DictionarySlim<TypeVal, string> DictSlim()
            {
                var dict = new DictionarySlim<TypeVal, string>();

                foreach (var key in _keys.Take(Count))
                    dict.GetOrAddValueRef(key) = "a";

                dict.GetOrAddValueRef(typeof(ImHashMapBenchmarks)) = "!";
                return dict;
            }

            private DictionarySlim<TypeVal, string> _dictSlim;

            public ConcurrentDictionary<Type, string> ConcurrentDict()
            {
                var map = new ConcurrentDictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                map.TryAdd(typeof(ImHashMapBenchmarks), "!");
                return map;
            }

            private ConcurrentDictionary<Type, string> _concurrentDict;

            public ImmutableDictionary<Type, string> ImmutableDict()
            {
                var builder = ImmutableDictionary.CreateBuilder<Type, string>();

                foreach (var key in _keys.Take(Count))
                    builder.Add(key, "a");
                builder.Add(typeof(ImHashMapBenchmarks), "!");
                return builder.ToImmutable();
            }

            private ImmutableDictionary<Type, string> _immutableDict;

            public TypeDictionary<string> TypeDictionary_Add()
            {
                var map = new TypeDictionary<string>();

                map.Add<A1>("a");
                map.Add<A2>("a");
                map.Add<A3>("a");
                map.Add<A4>("a");
                map.Add<A5>("a");
                // map.Add<A6>("a");
                // map.Add<A7>("a");
                // map.Add<A8>("a");
                // map.Add<A9>("a");
                // map.Add<A10>("a");
                // map.Add<B1>("a");
                // map.Add<B2>("a");
                // map.Add<B3>("a");
                // map.Add<B4>("a");
                // map.Add<B5>("a");
                // map.Add<B6>("a");
                // map.Add<B7>("a");
                // map.Add<B8>("a");
                // map.Add<B9>("a");
                // map.Add<B10>("a");

                map.Add<ImHashMapBenchmarks>("!");

                return map;
            }

            private TypeDictionary<string> _typeDict;

            #endregion

            public static Type LookupKey = typeof(ImHashMapBenchmarks);

            // [Benchmark(Baseline = true)]
            public string V2_ImHashMap_AVL_TryFind()
            {
                _mapV2.TryFind(LookupKey, out var result);
                return result;
            }

            // [Benchmark]
            public string ImHashMapSlots32_TryFind()
            {
                var hash = LookupKey.GetHashCode();
                _mapSlots[hash & ImHashMapSlots.HASH_MASK_TO_FIND_SLOT].TryFind(hash, LookupKey, out var result);
                return result;
            }

            // [Benchmark]
            public string V2_ImHashMap_AVLOptimizedForAdd_TryFind()
            {
                _mapExp.TryFind(LookupKey.GetHashCode(), LookupKey, out var result);
                return (string)result;
            }

            [Benchmark(Baseline = true)]
            public string V4_ImHashMap_TryFind()
            {
                _mapV4.TryFind(LookupKey.GetHashCode(), LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string V3_ImHashMap_TryFind()
            {
                _mapV3.TryFind(LookupKey.GetHashCode(), LookupKey, out var result);
                return result;
            }

            // [Benchmark]
            public string V4_ImMap_GetValueOrDefault() =>
                _mapV4.GetValueOrDefaultByReferenceEquals(LookupKey.GetHashCode(), LookupKey);

            // [Benchmark]
            public string V3_ImHashMap_GetValueOrDefault() =>
                _mapV3.GetValueOrDefaultByReferenceEquals(LookupKey.GetHashCode(), LookupKey);

            // [Benchmark]
            public string V3_PartitionedHashMap_GetValueOrDefault() =>
                _partMapV3.GetValueOrDefaultByReferenceEquals(LookupKey);

            // [Benchmark]
            public string V3_PartitionedHashMap32_GetValueOrDefault() =>
                _partMap23_32.GetValueOrDefaultByReferenceEquals(LookupKey, 31);

            // [Benchmark]
            public string TypeDict_TryFind() =>
                _typeDict.Get<ImHashMapBenchmarks>();

            // [Benchmark]
            // public string V3_ImHashMap_23Tree_TryFind_SIMPLIFIED()
            // {
            //     var entry = _map234.GetEntryOrDefault(LookupKey.GetHashCode());
            //     if (entry == null)
            //         return null;
            //     return ((ImTools.Experimental.ImHashMap234<Type, string>.KeyValueEntry)entry).Value;
            // }

            [Benchmark]
            public string V4_PartitionedHashMap_TryFind()
            {
                var hash = LookupKey.GetHashCode();
                _partMapV4[hash & ImToolsV3.PartitionedHashMap.PARTITION_HASH_MASK].TryFind(hash, LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string V3_PartitionedHashMap_TryFind()
            {
                var hash = LookupKey.GetHashCode();
                _partMapV3[hash & ImToolsV3.PartitionedHashMap.PARTITION_HASH_MASK].TryFind(hash, LookupKey, out var result);
                return result;
            }

            // [Benchmark]
            public string Experimental_ImHashMapSlots32_TryFind()
            {
                var hash = LookupKey.GetHashCode();
                _mapSlotsExp32[hash & ImHashMapSlots.HASH_MASK_TO_FIND_SLOT].TryFind(hash, LookupKey, out var result);
                return (string)result;
            }

            // [Benchmark]
            public string Experimental_ImHashMapSlots64_TryFind()
            {
                var hash = LookupKey.GetHashCode();
                _mapSlotsExp64[hash & 63].TryFind(hash, LookupKey, out var result);
                return (string)result;
            }

            [Benchmark]
            public string DictionarySlim_TryGetValue()
            {
                _dictSlim.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string Dictionary_TryGetValue()
            {
                _dict.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ConcurrentDictionary_TryGetValue()
            {
                _concurrentDict.TryGetValue(LookupKey, out var result);
                return result;
            }

            [Benchmark]
            public string ImmutableDict_TryGet()
            {
                _immutableDict.TryGetValue(LookupKey, out var result);
                return result;
            }
        }

        [MemoryDiagnoser]
        public class Enumerate
        {
            /*
            ## V2:

            BenchmarkDotNet=v0.12.0, OS=Windows 10.0.18362
            Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
            .NET Core SDK=3.0.100
              [Host]     : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT
              DefaultJob : .NET Core 3.0.0 (CoreCLR 4.700.19.46205, CoreFX 4.700.19.46214), X64 RyuJIT

|                        Method | Count |          Mean |      Error |     StdDev | Ratio | RatioSD |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------------------------ |------ |--------------:|-----------:|-----------:|------:|--------:|--------:|-------:|------:|----------:|
|    ImHashMap_EnumerateToArray |     1 |     147.31 ns |   0.567 ns |   0.531 ns |  1.00 |    0.00 |  0.0441 |      - |     - |     208 B |
| ImHashMap_V1_EnumerateToArray |     1 |     160.45 ns |   0.856 ns |   0.801 ns |  1.09 |    0.01 |  0.0560 |      - |     - |     264 B |
|         ImHashMap_FoldToArray |     1 |      55.05 ns |   0.436 ns |   0.387 ns |  0.37 |    0.00 |  0.0356 |      - |     - |     168 B |
|    ImHashMapSlots_FoldToArray |     1 |      89.21 ns |   1.473 ns |   1.378 ns |  0.61 |    0.01 |  0.0271 |      - |     - |     128 B |
|        DictionarySlim_ToArray |     1 |     150.88 ns |   1.424 ns |   1.189 ns |  1.02 |    0.01 |  0.0408 |      - |     - |     192 B |
|            Dictionary_ToArray |     1 |      40.47 ns |   0.864 ns |   1.093 ns |  0.28 |    0.01 |  0.0119 |      - |     - |      56 B |
|  ConcurrentDictionary_ToArray |     1 |     232.31 ns |   1.981 ns |   1.654 ns |  1.58 |    0.01 |  0.0114 |      - |     - |      56 B |
|         ImmutableDict_ToArray |     1 |     618.68 ns |  11.308 ns |  10.578 ns |  4.20 |    0.07 |  0.0114 |      - |     - |      56 B |
|                               |       |               |            |            |       |         |         |        |       |           |
|    ImHashMap_EnumerateToArray |    10 |     423.85 ns |   8.425 ns |   9.364 ns |  1.00 |    0.00 |  0.1001 |      - |     - |     472 B |
| ImHashMap_V1_EnumerateToArray |    10 |     492.81 ns |   4.461 ns |   4.173 ns |  1.16 |    0.03 |  0.1726 |      - |     - |     816 B |
|         ImHashMap_FoldToArray |    10 |     213.14 ns |   4.054 ns |   3.981 ns |  0.50 |    0.02 |  0.1054 |      - |     - |     496 B |
|    ImHashMapSlots_FoldToArray |    10 |     255.16 ns |   1.863 ns |   1.743 ns |  0.60 |    0.01 |  0.1016 |      - |     - |     480 B |
|        DictionarySlim_ToArray |    10 |     450.09 ns |   8.918 ns |  10.616 ns |  1.06 |    0.04 |  0.1354 |      - |     - |     640 B |
|            Dictionary_ToArray |    10 |      87.40 ns |   1.696 ns |   1.586 ns |  0.21 |    0.01 |  0.0424 |      - |     - |     200 B |
|  ConcurrentDictionary_ToArray |    10 |     499.22 ns |   4.804 ns |   4.494 ns |  1.17 |    0.03 |  0.0420 |      - |     - |     200 B |
|         ImmutableDict_ToArray |    10 |   1,954.98 ns |  10.732 ns |  10.038 ns |  4.60 |    0.11 |  0.0381 |      - |     - |     200 B |
|                               |       |               |            |            |       |         |         |        |       |           |
|    ImHashMap_EnumerateToArray |   100 |   2,735.61 ns |  34.407 ns |  32.185 ns |  1.00 |    0.00 |  0.4768 |      - |     - |    2248 B |
| ImHashMap_V1_EnumerateToArray |   100 |   3,368.76 ns |   6.822 ns |   6.048 ns |  1.23 |    0.02 |  1.1597 | 0.0267 |     - |    5472 B |
|         ImHashMap_FoldToArray |   100 |   1,433.97 ns |  14.981 ns |  14.013 ns |  0.52 |    0.01 |  0.6599 | 0.0038 |     - |    3112 B |
|    ImHashMapSlots_FoldToArray |   100 |   1,541.62 ns |   8.594 ns |   8.039 ns |  0.56 |    0.01 |  0.6714 | 0.0038 |     - |    3168 B |
|        DictionarySlim_ToArray |   100 |   2,505.97 ns |  44.927 ns |  37.516 ns |  0.92 |    0.02 |  0.8469 | 0.0076 |     - |    4000 B |
|            Dictionary_ToArray |   100 |     549.34 ns |   6.559 ns |   6.136 ns |  0.20 |    0.00 |  0.3481 | 0.0019 |     - |    1640 B |
|  ConcurrentDictionary_ToArray |   100 |   2,236.03 ns |  10.044 ns |   9.395 ns |  0.82 |    0.01 |  0.3471 |      - |     - |    1640 B |
|         ImmutableDict_ToArray |   100 |  15,683.87 ns |  68.105 ns |  60.373 ns |  5.74 |    0.08 |  0.3357 |      - |     - |    1640 B |
|                               |       |               |            |            |       |         |         |        |       |           |
|    ImHashMap_EnumerateToArray |  1000 |  25,723.37 ns | 504.158 ns | 560.370 ns |  1.00 |    0.00 |  3.5706 | 0.1526 |     - |   16808 B |
| ImHashMap_V1_EnumerateToArray |  1000 |  34,316.66 ns | 583.573 ns | 545.874 ns |  1.34 |    0.04 | 10.3149 | 1.8921 |     - |   48833 B |
|         ImHashMap_FoldToArray |  1000 |  16,277.05 ns |  38.330 ns |  33.979 ns |  0.64 |    0.02 |  5.2490 | 0.3052 |     - |   24752 B |
|    ImHashMapSlots_FoldToArray |  1000 |  15,167.14 ns | 261.927 ns | 218.721 ns |  0.59 |    0.02 |  5.2490 | 0.2899 |     - |   24784 B |
|        DictionarySlim_ToArray |  1000 |  22,273.30 ns | 384.716 ns | 359.864 ns |  0.87 |    0.01 |  6.9885 | 0.6714 |     - |   32896 B |
|            Dictionary_ToArray |  1000 |   4,997.89 ns |  20.869 ns |  18.500 ns |  0.20 |    0.00 |  3.3951 | 0.1831 |     - |   16040 B |
|  ConcurrentDictionary_ToArray |  1000 |  36,859.40 ns | 193.536 ns | 181.034 ns |  1.44 |    0.04 |  3.3569 | 0.1831 |     - |   16040 B |
|         ImmutableDict_ToArray |  1000 | 155,798.41 ns | 484.101 ns | 452.828 ns |  6.08 |    0.14 |  3.1738 |      - |     - |   16040 B |

|                             Method | Count |         Mean |      Error |     StdDev | Ratio |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|----------------------------------- |------ |-------------:|-----------:|-----------:|------:|-------:|-------:|------:|----------:|
|         ImHashMap_EnumerateToArray |     1 |    153.03 ns |   0.774 ns |   0.686 ns |  1.00 | 0.0441 |      - |     - |     208 B |
| Experimental_ImHashMap_FoldToArray |     1 |     62.26 ns |   0.180 ns |   0.159 ns |  0.41 | 0.0271 |      - |     - |     128 B |
|                                    |       |              |            |            |       |        |        |       |           |
|         ImHashMap_EnumerateToArray |    10 |    417.19 ns |   4.075 ns |   3.403 ns |  1.00 | 0.1001 |      - |     - |     472 B |
| Experimental_ImHashMap_FoldToArray |    10 |    237.62 ns |   1.079 ns |   1.009 ns |  0.57 | 0.1016 |      - |     - |     480 B |
|                                    |       |              |            |            |       |        |        |       |           |
|         ImHashMap_EnumerateToArray |   100 |  2,693.86 ns |   7.426 ns |   6.946 ns |  1.00 | 0.4768 |      - |     - |    2248 B |
| Experimental_ImHashMap_FoldToArray |   100 |  1,517.72 ns |   6.061 ns |   5.669 ns |  0.56 | 0.6561 | 0.0038 |     - |    3096 B |
|                                    |       |              |            |            |       |        |        |       |           |
|         ImHashMap_EnumerateToArray |  1000 | 26,018.52 ns | 175.262 ns | 146.352 ns |  1.00 | 3.5706 | 0.1831 |     - |   16808 B |
| Experimental_ImHashMap_FoldToArray |  1000 | 15,321.95 ns |  69.421 ns |  64.937 ns |  0.59 | 5.2490 | 0.2747 |     - |   24736 B |

## V3 baseline

|                                   Method | Count |         Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------- |------ |-------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|     V2_ImHashMap_AVL_EnumerateAndToArray |     1 |     198.4 ns |   3.64 ns |   3.04 ns |  1.00 |    0.00 | 0.0496 |     - |     - |     208 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |     1 |     158.0 ns |   3.23 ns |   3.17 ns |  0.80 |    0.02 | 0.0362 |     - |     - |     152 B |
|                                          |       |              |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |    10 |     535.6 ns |   7.97 ns |   8.86 ns |  1.00 |    0.00 | 0.1125 |     - |     - |     472 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |    10 |     558.9 ns |   5.35 ns |   5.25 ns |  1.04 |    0.02 | 0.1354 |     - |     - |     568 B |
|                                          |       |              |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |   100 |   3,601.3 ns |  54.44 ns |  50.92 ns |  1.00 |    0.00 | 0.5341 |     - |     - |    2248 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |   100 |   9,800.0 ns | 110.20 ns |  97.69 ns |  2.72 |    0.04 | 1.0529 |     - |     - |    4424 B |
|                                          |       |              |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |  1000 |  34,261.7 ns | 371.47 ns | 310.20 ns |  1.00 |    0.00 | 3.9673 |     - |     - |   16808 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |  1000 | 128,626.8 ns | 988.46 ns | 924.61 ns |  3.76 |    0.04 | 9.2773 |     - |     - |   38912 B |

### Static Enumerate - incomplete

|                                   Method | Count |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------- |------ |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|     V2_ImHashMap_AVL_EnumerateAndToArray |     1 |    164.7 ns |   2.23 ns |   1.98 ns |  1.00 |    0.00 | 0.0496 |     - |     - |     208 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |     1 |    245.0 ns |   4.92 ns |   5.66 ns |  1.49 |    0.04 | 0.0567 |     - |     - |     240 B |
|                                          |       |             |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |     5 |    295.6 ns |   5.87 ns |   5.77 ns |  1.00 |    0.00 | 0.0801 |     - |     - |     336 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |     5 |    497.5 ns |   5.77 ns |   5.12 ns |  1.68 |    0.04 | 0.0849 |     - |     - |     360 B |
|                                          |       |             |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |    10 |    459.0 ns |   4.86 ns |   4.55 ns |  1.00 |    0.00 | 0.1116 |     - |     - |     472 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |    10 |    683.6 ns |   9.36 ns |   8.30 ns |  1.49 |    0.02 | 0.1335 |     - |     - |     560 B |
|                                          |       |             |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |   100 |  3,219.3 ns |  36.20 ns |  30.23 ns |  1.00 |    0.00 | 0.5341 |     - |     - |    2248 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |   100 |  5,009.4 ns |  57.55 ns |  51.02 ns |  1.56 |    0.02 | 0.5264 |     - |     - |    2208 B |
|                                          |       |             |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |  1000 | 29,919.7 ns | 363.29 ns | 322.05 ns |  1.00 |    0.00 | 3.9673 |     - |     - |   16808 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |  1000 | 21,530.8 ns | 292.37 ns | 259.18 ns |  0.72 |    0.01 | 1.8921 |     - |     - |    8016 B |

### Struct enumerator for leafs

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.630 (2004/?/20H1)
Intel Core i7-8565U CPU 1.80GHz (Whiskey Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.100
  [Host]     : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT
  DefaultJob : .NET Core 5.0.0 (CoreCLR 5.0.20.51904, CoreFX 5.0.20.51904), X64 RyuJIT


|                                   Method | Count |         Mean |       Error |      StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------- |------ |-------------:|------------:|------------:|------:|--------:|-------:|------:|------:|----------:|
|     V2_ImHashMap_AVL_EnumerateAndToArray |     1 |     131.8 ns |     2.43 ns |     2.27 ns |  1.00 |    0.00 | 0.0458 |     - |     - |     192 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |     1 |     107.7 ns |     0.56 ns |     0.44 ns |  0.82 |    0.01 | 0.0305 |     - |     - |     128 B |
|                                          |       |              |             |             |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |     5 |     251.5 ns |     3.12 ns |     2.77 ns |  1.00 |    0.00 | 0.0782 |     - |     - |     328 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |     5 |     214.5 ns |     4.37 ns |     4.86 ns |  0.86 |    0.02 | 0.0591 |     - |     - |     248 B |
|                                          |       |              |             |             |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |    10 |     402.3 ns |     2.40 ns |     2.00 ns |  1.00 |    0.00 | 0.1106 |     - |     - |     464 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |    10 |     469.7 ns |     8.19 ns |    14.13 ns |  1.19 |    0.04 | 0.1144 |     - |     - |     480 B |
|                                          |       |              |             |             |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |   100 |   2,987.6 ns |    34.44 ns |    28.76 ns |  1.00 |    0.00 | 0.5341 |     - |     - |    2240 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |   100 |   8,085.2 ns |    54.39 ns |    50.88 ns |  2.71 |    0.03 | 0.9308 |     - |     - |    3904 B |
|                                          |       |              |             |             |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |  1000 |  29,107.9 ns |   372.54 ns |   330.25 ns |  1.00 |    0.00 | 3.9978 |     - |     - |   16800 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |  1000 | 117,098.5 ns | 1,388.31 ns | 1,298.62 ns |  4.02 |    0.07 | 8.0566 |     - |     - |   33992 B |


### Static iterative enumerator with List as a stack

|                                   Method | Count |        Mean |     Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------- |------ |------------:|----------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|     V2_ImHashMap_AVL_EnumerateAndToArray |     1 |    130.0 ns |   1.58 ns |   1.76 ns |  1.00 |    0.00 | 0.0458 |     - |     - |     192 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |     1 |    129.1 ns |   1.55 ns |   1.45 ns |  0.99 |    0.02 | 0.0610 |     - |     - |     256 B |
|                                          |       |             |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |     5 |    251.6 ns |   3.58 ns |   3.35 ns |  1.00 |    0.00 | 0.0782 |     - |     - |     328 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |     5 |    241.4 ns |   2.97 ns |   2.78 ns |  0.96 |    0.01 | 0.0896 |     - |     - |     376 B |
|                                          |       |             |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |    10 |    414.8 ns |   8.12 ns |  10.84 ns |  1.00 |    0.00 | 0.1106 |     - |     - |     464 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |    10 |    403.7 ns |   6.25 ns |   5.84 ns |  0.98 |    0.03 | 0.1373 |     - |     - |     576 B |
|                                          |       |             |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |   100 |  2,947.2 ns |  23.48 ns |  20.81 ns |  1.00 |    0.00 | 0.5341 |     - |     - |    2240 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |   100 |  2,822.2 ns |  16.96 ns |  15.87 ns |  0.96 |    0.01 | 0.5646 |     - |     - |    2376 B |
|                                          |       |             |           |           |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |  1000 | 28,594.2 ns | 307.61 ns | 287.73 ns |  1.00 |    0.00 | 3.9673 |     - |     - |   16800 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |  1000 | 27,555.7 ns | 318.08 ns | 281.97 ns |  0.96 |    0.01 | 4.0588 |     - |     - |   16992 B |

### Branch3 is a Branch2

|                                   Method | Count |       Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------------------------- |------ |-----------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|     V2_ImHashMap_AVL_EnumerateAndToArray |     5 |   329.7 ns |  6.40 ns |  5.99 ns |  1.00 |    0.00 | 0.0782 |     - |     - |     328 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |     5 |   313.1 ns |  5.69 ns |  5.59 ns |  0.95 |    0.02 | 0.0877 |     - |     - |     368 B |
|                                          |       |            |          |          |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |    10 |   513.4 ns |  4.55 ns |  4.26 ns |  1.00 |    0.00 | 0.1106 |     - |     - |     464 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |    10 |   525.6 ns |  7.40 ns |  6.92 ns |  1.02 |    0.02 | 0.1354 |     - |     - |     568 B |
|                                          |       |            |          |          |       |         |        |       |       |           |
|     V2_ImHashMap_AVL_EnumerateAndToArray |   100 | 3,658.7 ns | 53.67 ns | 47.58 ns |  1.00 |    0.00 | 0.5341 |     - |     - |    2240 B |
| V3_ImHashMap_23Tree_EnumerateAndToArray |   100 | 3,388.2 ns | 67.71 ns | 63.34 ns |  0.93 |    0.02 | 0.5646 |     - |     - |    2368 B |

### V3 RTM

|                        Method | Count |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------ |------ |-------------:|-------------:|-------------:|-------------:|------:|--------:|-------:|------:|------:|----------:|
|          V2_ImHashMap_foreach |     1 |     53.16 ns |     1.107 ns |     1.317 ns |     53.11 ns |  1.00 |    0.00 | 0.0166 |     - |     - |     104 B |
|          V3_ImHashMap_foreach |     1 |     62.12 ns |     1.327 ns |     1.986 ns |     61.59 ns |  1.16 |    0.05 | 0.0267 |     - |     - |     168 B |
| V3_PartitionedHashMap_foreach |     1 |    238.62 ns |     4.811 ns |     6.084 ns |    235.92 ns |  4.50 |    0.13 | 0.0534 |     - |     - |     336 B |
|        DictionarySlim_foreach |     1 |     12.90 ns |     0.167 ns |     0.156 ns |     12.90 ns |  0.24 |    0.01 |      - |     - |     - |         - |
|            Dictionary_foreach |     1 |     14.19 ns |     0.217 ns |     0.181 ns |     14.14 ns |  0.27 |    0.01 |      - |     - |     - |         - |
|  ConcurrentDictionary_foreach |     1 |    153.40 ns |     2.768 ns |     4.142 ns |    151.56 ns |  2.89 |    0.11 | 0.0100 |     - |     - |      64 B |
|         ImmutableDict_foreach |     1 |    268.98 ns |     5.361 ns |     9.528 ns |    268.86 ns |  5.01 |    0.26 |      - |     - |     - |         - |
|                               |       |              |              |              |              |       |         |        |       |       |           |
|          V2_ImHashMap_foreach |    10 |    233.42 ns |     4.541 ns |     4.859 ns |    232.71 ns |  1.00 |    0.00 | 0.0200 |     - |     - |     128 B |
|          V3_ImHashMap_foreach |    10 |    249.12 ns |     4.915 ns |     5.852 ns |    246.87 ns |  1.07 |    0.03 | 0.0391 |     - |     - |     248 B |
| V3_PartitionedHashMap_foreach |    10 |    746.26 ns |    14.990 ns |    18.409 ns |    748.53 ns |  3.20 |    0.13 | 0.1602 |     - |     - |    1008 B |
|        DictionarySlim_foreach |    10 |     72.54 ns |     0.970 ns |     0.907 ns |     72.42 ns |  0.31 |    0.01 |      - |     - |     - |         - |
|            Dictionary_foreach |    10 |     58.52 ns |     0.938 ns |     0.733 ns |     58.58 ns |  0.25 |    0.01 |      - |     - |     - |         - |
|  ConcurrentDictionary_foreach |    10 |    468.65 ns |     9.252 ns |    12.351 ns |    464.12 ns |  2.01 |    0.06 | 0.0095 |     - |     - |      64 B |
|         ImmutableDict_foreach |    10 |  1,127.30 ns |    15.601 ns |    14.593 ns |  1,123.20 ns |  4.82 |    0.10 |      - |     - |     - |         - |
|                               |       |              |              |              |              |       |         |        |       |       |           |
|          V2_ImHashMap_foreach |   100 |  2,355.54 ns |    46.224 ns |    63.271 ns |  2,337.40 ns |  1.00 |    0.00 | 0.0229 |     - |     - |     160 B |
|          V3_ImHashMap_foreach |   100 |  2,423.13 ns |    31.652 ns |    46.395 ns |  2,412.92 ns |  1.03 |    0.04 | 0.0496 |     - |     - |     320 B |
| V3_PartitionedHashMap_foreach |   100 |  4,268.51 ns |    25.429 ns |    22.542 ns |  4,266.90 ns |  1.81 |    0.05 | 0.4501 |     - |     - |    2856 B |
|        DictionarySlim_foreach |   100 |    570.39 ns |     5.827 ns |     4.866 ns |    570.93 ns |  0.24 |    0.01 |      - |     - |     - |         - |
|            Dictionary_foreach |   100 |    548.46 ns |     8.579 ns |     7.605 ns |    547.90 ns |  0.23 |    0.01 |      - |     - |     - |         - |
|  ConcurrentDictionary_foreach |   100 |  2,967.70 ns |    45.435 ns |    44.623 ns |  2,958.11 ns |  1.26 |    0.04 | 0.0076 |     - |     - |      64 B |
|         ImmutableDict_foreach |   100 |  9,988.48 ns |   198.973 ns |   297.813 ns |  9,821.03 ns |  4.24 |    0.14 |      - |     - |     - |         - |
|                               |       |              |              |              |              |       |         |        |       |       |           |
|          V2_ImHashMap_foreach |  1000 | 23,828.30 ns |   433.708 ns |   362.166 ns | 23,743.77 ns |  1.00 |    0.00 | 0.0305 |     - |     - |     192 B |
|          V3_ImHashMap_foreach |  1000 | 26,014.69 ns |   294.125 ns |   245.608 ns | 25,965.82 ns |  1.09 |    0.02 | 0.0610 |     - |     - |     552 B |
| V3_PartitionedHashMap_foreach |  1000 | 36,582.53 ns |   709.641 ns |   897.469 ns | 36,594.84 ns |  1.54 |    0.04 | 0.4883 |     - |     - |    3240 B |
|        DictionarySlim_foreach |  1000 |  5,591.13 ns |    43.627 ns |    40.809 ns |  5,602.25 ns |  0.23 |    0.00 |      - |     - |     - |         - |
|            Dictionary_foreach |  1000 |  5,319.86 ns |    51.684 ns |    45.817 ns |  5,308.36 ns |  0.22 |    0.00 |      - |     - |     - |         - |
|  ConcurrentDictionary_foreach |  1000 | 38,718.40 ns |   466.979 ns |   389.949 ns | 38,728.64 ns |  1.63 |    0.03 |      - |     - |     - |      64 B |
|         ImmutableDict_foreach |  1000 | 99,156.35 ns | 1,962.968 ns | 2,181.834 ns | 98,166.03 ns |  4.17 |    0.11 |      - |     - |     - |         - |

## V3.2

|               Method | Count |         Mean |      Error |     StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------- |------ |-------------:|-----------:|-----------:|------:|--------:|-------:|------:|------:|----------:|
| V3_ImHashMap_foreach |     1 |     49.59 ns |   0.758 ns |   0.633 ns |  1.00 |    0.00 | 0.0255 |     - |     - |     160 B |
| V2_ImHashMap_foreach |     1 |     45.96 ns |   0.273 ns |   0.242 ns |  0.93 |    0.01 | 0.0166 |     - |     - |     104 B |
|                      |       |              |            |            |       |         |        |       |       |           |
| V3_ImHashMap_foreach |    10 |    214.40 ns |   3.915 ns |   3.662 ns |  1.00 |    0.00 | 0.0381 |     - |     - |     240 B |
| V2_ImHashMap_foreach |    10 |    204.50 ns |   0.963 ns |   0.854 ns |  0.95 |    0.02 | 0.0203 |     - |     - |     128 B |
|                      |       |              |            |            |       |         |        |       |       |           |
| V3_ImHashMap_foreach |   100 |  2,072.90 ns |  40.826 ns |  41.926 ns |  1.00 |    0.00 | 0.0496 |     - |     - |     328 B |
| V2_ImHashMap_foreach |   100 |  2,206.31 ns |  29.262 ns |  25.940 ns |  1.07 |    0.03 | 0.0229 |     - |     - |     160 B |
|                      |       |              |            |            |       |         |        |       |       |           |
| V3_ImHashMap_foreach |  1000 | 21,043.39 ns | 298.240 ns | 232.846 ns |  1.00 |    0.00 | 0.0305 |     - |     - |     328 B |
| V2_ImHashMap_foreach |  1000 | 21,484.35 ns | 168.389 ns | 149.272 ns |  1.02 |    0.01 | 0.0305 |     - |     - |     192 B |

## V4.0 - baseline

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i5-8350U CPU 1.70GHz (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=6.0.102
  [Host]     : .NET Core 6.0.2 (CoreCLR 6.0.222.6406, CoreFX 6.0.222.6406), X64 RyuJIT
  DefaultJob : .NET Core 6.0.2 (CoreCLR 6.0.222.6406, CoreFX 6.0.222.6406), X64 RyuJIT


|                 Method | Count |         Mean |        Error |       StdDev |       Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------- |------ |-------------:|-------------:|-------------:|-------------:|------:|--------:|-------:|------:|------:|----------:|
| V3_ImHashMap_Enumerate |     1 |     78.29 ns |     3.211 ns |     9.417 ns |     74.97 ns |  1.00 |    0.00 | 0.0509 |     - |     - |     160 B |
|   Dictionary_Enumerate |     1 |     23.15 ns |     0.712 ns |     2.008 ns |     22.54 ns |  0.30 |    0.04 |      - |     - |     - |         - |
|                        |       |              |              |              |              |       |         |        |       |       |           |
| V3_ImHashMap_Enumerate |    10 |    351.19 ns |     7.127 ns |    20.791 ns |    344.93 ns |  1.00 |    0.00 | 0.0763 |     - |     - |     240 B |
|   Dictionary_Enumerate |    10 |     92.85 ns |     1.957 ns |     4.128 ns |     91.81 ns |  0.27 |    0.02 |      - |     - |     - |         - |
|                        |       |              |              |              |              |       |         |        |       |       |           |
| V3_ImHashMap_Enumerate |   100 |  3,049.03 ns |    64.798 ns |   186.958 ns |  2,987.63 ns |  1.00 |    0.00 | 0.0763 |     - |     - |     240 B |
|   Dictionary_Enumerate |   100 |    844.11 ns |    16.992 ns |    38.698 ns |    837.12 ns |  0.28 |    0.02 |      - |     - |     - |         - |
|                        |       |              |              |              |              |       |         |        |       |       |           |
| V3_ImHashMap_Enumerate |  1000 | 34,113.03 ns | 1,105.023 ns | 3,116.738 ns | 33,509.27 ns |  1.00 |    0.00 | 0.1221 |     - |     - |     480 B |
|   Dictionary_Enumerate |  1000 | 14,166.55 ns |   237.173 ns |   221.852 ns | 14,124.19 ns |  0.43 |    0.03 |      - |     - |     - |         - |

## V4 baseline

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19043
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=6.0.202
  [Host]     : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT
  DefaultJob : .NET Core 6.0.4 (CoreCLR 6.0.422.16404, CoreFX 6.0.422.16404), X64 RyuJIT

|                          Method | Count |         Mean |      Error |     StdDev |       Median | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |------ |-------------:|-----------:|-----------:|-------------:|------:|--------:|-------:|------:|------:|----------:|
|          V4_ImHashMap_Enumerate |     1 |     39.51 ns |   0.865 ns |   0.996 ns |     39.43 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|          V3_ImHashMap_Enumerate |     1 |     44.11 ns |   0.879 ns |   0.822 ns |     44.28 ns |  1.12 |    0.04 | 0.0255 |     - |     - |     160 B |
| V4_PartitionedHashMap_Enumerate |     1 |    118.94 ns |   1.644 ns |   1.538 ns |    118.91 ns |  3.01 |    0.09 |      - |     - |     - |         - |
| V3_PartitionedHashMap_Enumerate |     1 |    180.82 ns |   3.462 ns |   3.238 ns |    181.09 ns |  4.57 |    0.14 | 0.0522 |     - |     - |     328 B |
|        DictionarySlim_Enumerate |     1 |     12.77 ns |   0.254 ns |   0.238 ns |     12.76 ns |  0.32 |    0.01 |      - |     - |     - |         - |
|            Dictionary_Enumerate |     1 |     15.24 ns |   0.652 ns |   1.817 ns |     14.36 ns |  0.46 |    0.05 |      - |     - |     - |         - |
|    ConcurrentDictionary_foreach |     1 |    177.04 ns |   2.073 ns |   1.939 ns |    176.68 ns |  4.47 |    0.12 | 0.0100 |     - |     - |      64 B |
|         ImmutableDict_Enumerate |     1 |    161.96 ns |   0.760 ns |   0.635 ns |    161.98 ns |  4.08 |    0.12 |      - |     - |     - |         - |
|                                 |       |              |            |            |              |       |         |        |       |       |           |
|          V4_ImHashMap_Enumerate |    10 |    191.91 ns |   3.010 ns |   2.668 ns |    192.57 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|          V3_ImHashMap_Enumerate |    10 |    223.10 ns |   1.885 ns |   1.574 ns |    223.12 ns |  1.16 |    0.02 | 0.0381 |     - |     - |     240 B |
| V4_PartitionedHashMap_Enumerate |    10 |    379.75 ns |   4.543 ns |   4.027 ns |    379.74 ns |  1.98 |    0.03 |      - |     - |     - |         - |
| V3_PartitionedHashMap_Enumerate |    10 |    604.47 ns |   4.257 ns |   3.774 ns |    603.87 ns |  3.15 |    0.05 | 0.1793 |     - |     - |    1128 B |
|        DictionarySlim_Enumerate |    10 |     73.15 ns |   1.438 ns |   1.345 ns |     72.82 ns |  0.38 |    0.01 |      - |     - |     - |         - |
|            Dictionary_Enumerate |    10 |     57.95 ns |   0.749 ns |   0.701 ns |     57.86 ns |  0.30 |    0.01 |      - |     - |     - |         - |
|    ConcurrentDictionary_foreach |    10 |    505.36 ns |   7.959 ns |   7.445 ns |    504.00 ns |  2.64 |    0.05 | 0.0095 |     - |     - |      64 B |
|         ImmutableDict_Enumerate |    10 |    556.35 ns |  10.730 ns |   9.512 ns |    558.53 ns |  2.90 |    0.06 |      - |     - |     - |         - |
|                                 |       |              |            |            |              |       |         |        |       |       |           |
|          V4_ImHashMap_Enumerate |   100 |  2,023.65 ns |  33.506 ns |  29.702 ns |  2,031.78 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|          V3_ImHashMap_Enumerate |   100 |  1,992.46 ns |  23.400 ns |  20.744 ns |  1,992.05 ns |  0.98 |    0.02 | 0.0381 |     - |     - |     240 B |
| V4_PartitionedHashMap_Enumerate |   100 |  2,626.85 ns |  42.089 ns |  37.311 ns |  2,616.34 ns |  1.30 |    0.02 |      - |     - |     - |         - |
| V3_PartitionedHashMap_Enumerate |   100 |  3,469.16 ns |  30.415 ns |  26.962 ns |  3,469.32 ns |  1.71 |    0.03 | 0.4349 |     - |     - |    2728 B |
|        DictionarySlim_Enumerate |   100 |    615.29 ns |  10.217 ns |   9.057 ns |    617.89 ns |  0.30 |    0.01 |      - |     - |     - |         - |
|            Dictionary_Enumerate |   100 |    578.30 ns |   3.310 ns |   2.764 ns |    578.14 ns |  0.29 |    0.00 |      - |     - |     - |         - |
|    ConcurrentDictionary_foreach |   100 |  3,444.60 ns |  55.779 ns |  52.176 ns |  3,425.75 ns |  1.70 |    0.03 | 0.0076 |     - |     - |      64 B |
|         ImmutableDict_Enumerate |   100 |  4,557.81 ns |  71.187 ns |  63.105 ns |  4,552.69 ns |  2.25 |    0.03 |      - |     - |     - |         - |
|                                 |       |              |            |            |              |       |         |        |       |       |           |
|          V4_ImHashMap_Enumerate |  1000 | 22,259.46 ns | 204.173 ns | 180.994 ns | 22,241.67 ns |  1.00 |    0.00 |      - |     - |     - |         - |
|          V3_ImHashMap_Enumerate |  1000 | 21,710.99 ns | 288.512 ns | 240.921 ns | 21,770.78 ns |  0.98 |    0.02 | 0.0610 |     - |     - |     480 B |
| V4_PartitionedHashMap_Enumerate |  1000 | 28,097.70 ns | 392.513 ns | 306.449 ns | 28,134.91 ns |  1.26 |    0.02 |      - |     - |     - |         - |
| V3_PartitionedHashMap_Enumerate |  1000 | 31,132.68 ns | 473.705 ns | 443.104 ns | 31,208.71 ns |  1.40 |    0.01 | 0.4272 |     - |     - |    2728 B |
|        DictionarySlim_Enumerate |  1000 |  6,472.19 ns |  53.546 ns |  47.467 ns |  6,482.73 ns |  0.29 |    0.00 |      - |     - |     - |         - |
|            Dictionary_Enumerate |  1000 |  5,700.68 ns |  65.696 ns |  61.452 ns |  5,698.15 ns |  0.26 |    0.00 |      - |     - |     - |         - |
|    ConcurrentDictionary_foreach |  1000 | 43,550.74 ns | 848.746 ns | 752.391 ns | 43,765.79 ns |  1.96 |    0.04 |      - |     - |     - |      64 B |
|         ImmutableDict_Enumerate |  1000 | 46,089.57 ns | 524.157 ns | 464.651 ns | 46,183.41 ns |  2.07 |    0.03 |      - |     - |     - |         - |
*/

            [Params(1, 10, 100, 1_000)]
            // [Params(100, 1_000)]
            public int Count;

            [GlobalSetup]
            public void Populate()
            {
                _mapV2 = V2_AddOrUpdate();
                // _mapExp = Experimental_ImHashMap_AddOrUpdate();
                _mapV4 = V4_ImMap_AddOrUpdate();
                _mapV3 = V3_ImHashMap_AddOrUpdate();
                _mapPartV4 = V4_PartionedHashMap_AddOrUpdate();
                _mapPartV3 = V3_PartionedHashMap_AddOrUpdate();
                _dict = Dict();
                _dictSlim = DictSlim();
                _concurrentDict = ConcurrentDict();
                _immutableDict = ImmutableDict();
            }

            #region Population

            public ImTools.V2.ImHashMap<Type, string> V2_AddOrUpdate()
            {
                var map = ImTools.V2.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key, "a");

                return map;
            }

            private ImTools.V2.ImHashMap<Type, string> _mapV2;

            private ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>> _mapExp;

            public ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>> Experimental_ImHashMap_AddOrUpdate()
            {
                var map = ImTools.V2.Experimental.ImMap<ImTools.V2.Experimental.ImMap.KValue<Type>>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map;
            }

            private ImToolsV3.ImHashMap<Type, string> _mapV3;
            public ImToolsV3.ImHashMap<Type, string> V3_ImHashMap_AddOrUpdate()
            {
                var map = ImToolsV3.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map;
            }

            private ImTools.ImHashMap<Type, string> _mapV4;

            public ImTools.ImHashMap<Type, string> V4_ImMap_AddOrUpdate()
            {
                var map = ImTools.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map;
            }

            private ImTools.ImHashMap<Type, string>[] _mapPartV4;
            public ImTools.ImHashMap<Type, string>[] V4_PartionedHashMap_AddOrUpdate()
            {
                var map = ImTools.PartitionedHashMap.CreateEmpty<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.AddOrUpdate(key, "a");

                return map;
            }

            private ImToolsV3.ImHashMap<Type, string>[] _mapPartV3;
            public ImToolsV3.ImHashMap<Type, string>[] V3_PartionedHashMap_AddOrUpdate()
            {
                var map = ImToolsV3.PartitionedHashMap.CreateEmpty<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.AddOrUpdate(key, "a");

                return map;
            }


            public Dictionary<Type, string> Dict()
            {
                var map = new Dictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                return map;
            }

            private Dictionary<Type, string> _dict;

            public DictionarySlim<TypeVal, string> DictSlim()
            {
                var dict = new DictionarySlim<TypeVal, string>();

                foreach (var key in _keys.Take(Count))
                    dict.GetOrAddValueRef(key) = "a";

                return dict;
            }

            private DictionarySlim<TypeVal, string> _dictSlim;

            public ConcurrentDictionary<Type, string> ConcurrentDict()
            {
                var map = new ConcurrentDictionary<Type, string>();

                foreach (var key in _keys.Take(Count))
                    map.TryAdd(key, "a");

                return map;
            }

            private ConcurrentDictionary<Type, string> _concurrentDict;

            public ImmutableDictionary<Type, string> ImmutableDict()
            {
                var builder = ImmutableDictionary.CreateBuilder<Type, string>();

                foreach (var key in _keys.Take(Count))
                    builder.Add(key, "a");

                return builder.ToImmutable();
            }

            private ImmutableDictionary<Type, string> _immutableDict;

            #endregion

            [Benchmark(Baseline = true)]
            public object V4_ImHashMap_Enumerate()
            {
                var s = "";
                foreach (var x in _mapV4.Enumerate())
                    s = x.Value;
                return s;
            }

            [Benchmark]
            public object V3_ImHashMap_Enumerate()
            {
                var s = "";
                foreach (var x in _mapV3.Enumerate())
                    s = x.Value;
                return s;
            }

            [Benchmark]
            public object V4_PartitionedHashMap_Enumerate()
            {
                var s = "";
                foreach (var x in _mapPartV4.Enumerate())
                    s = x.Value;
                return s;
            }

            [Benchmark]
            public object V3_PartitionedHashMap_Enumerate()
            {
                var s = "";
                foreach (var x in _mapPartV3.Enumerate())
                    s = x.Value;
                return s;
            }

            // [Benchmark]
            public object V2_ImHashMap_Enumerate() 
            {
                var s = "";
                foreach (var x in _mapV2.Enumerate())
                    s = x.Value;
                return s;
            }

            [Benchmark]
            public object DictionarySlim_Enumerate()
            {
                var s = "";
                foreach (var x in _dictSlim)
                    s = x.Value;
                return s;
            }

            [Benchmark]
            public object Dictionary_Enumerate()
            {
                var s = "";
                foreach (var x in _dict)
                    s = x.Value;
                return s;
            }

            [Benchmark]
            public object ConcurrentDictionary_foreach()
            {
                var s = "";
                foreach (var x in _concurrentDict)
                    s = x.Value;
                return s;
            }

            [Benchmark]
            public object ImmutableDict_Enumerate()
            {
                var s = "";
                foreach (var x in _immutableDict)
                    s = x.Value;
                return s;
            }
        }

        [MemoryDiagnoser]
        public class ToArray
        {
            /*
            BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
            Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
            .NET Core SDK=5.0.202
            [Host]     : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT
            DefaultJob : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT


            |                Method | Count |      Mean |    Error |    StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
            |---------------------- |------ |----------:|---------:|----------:|------:|--------:|-------:|------:|------:|----------:|
            |           UsingLambda |     1 |  29.07 ns | 0.672 ns |  1.722 ns |  1.00 |    0.00 | 0.0051 |     - |     - |      32 B |
            |    UsingGenericStruct |     1 |  27.87 ns | 0.657 ns |  1.660 ns |  0.96 |    0.08 | 0.0051 |     - |     - |      32 B |
            | UsingNonGenericStruct |     1 |  15.11 ns | 0.391 ns |  0.930 ns |  0.52 |    0.05 | 0.0051 |     - |     - |      32 B |
            |                       |       |           |          |           |       |         |        |       |       |           |
            |           UsingLambda |    10 | 159.52 ns | 3.168 ns |  5.379 ns |  1.00 |    0.00 | 0.0293 |     - |     - |     184 B |
            |    UsingGenericStruct |    10 | 195.96 ns | 4.683 ns | 13.435 ns |  1.28 |    0.09 | 0.0293 |     - |     - |     184 B |
            | UsingNonGenericStruct |    10 | 134.08 ns | 2.785 ns |  7.578 ns |  0.85 |    0.06 | 0.0293 |     - |     - |     184 B |
            */
            [Params(1, 10)]//, 100, 1_000)]
            public int Count;

            [GlobalSetup]
            public void Populate()
            {
                _mapV3 = V3_ImHashMap_AddOrUpdate();
            }

            [Benchmark(Baseline = true)]
            public object UsingLambda() => _mapV3.ToArray();

            private ImToolsV3.ImHashMap<Type, string> _mapV3;
            public ImToolsV3.ImHashMap<Type, string> V3_ImHashMap_AddOrUpdate()
            {
                var map = ImToolsV3.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map;
            }
        }

        [MemoryDiagnoser]
        public class GetAndUpdate_vs_AddOrGetAndReplace
        {
/*
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i9-8950HK CPU 2.90GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET Core SDK=5.0.202
  [Host]     : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT
  DefaultJob : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT

## Initial results

|                                            Method | Count |     Mean |    Error |   StdDev | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------------------------- |------ |---------:|---------:|---------:|------:|--------:|-------:|------:|------:|----------:|
|                              Get_then_AddOrUpdate |     1 | 29.34 ns | 0.397 ns | 0.371 ns |  1.00 |    0.00 | 0.0114 |     - |     - |      72 B |
|                             AddOrGet_then_Replace |     1 | 25.17 ns | 0.568 ns | 0.504 ns |  0.86 |    0.02 | 0.0114 |     - |     - |      72 B |
| AddOrGet_then_Replace_inlined_without_lambda_cost |     1 | 24.05 ns | 0.316 ns | 0.296 ns |  0.82 |    0.01 | 0.0115 |     - |     - |      72 B |
|                                                   |       |          |          |          |       |         |        |       |       |           |
|                              Get_then_AddOrUpdate |     5 | 32.54 ns | 0.725 ns | 0.917 ns |  1.00 |    0.00 | 0.0114 |     - |     - |      72 B |
|                             AddOrGet_then_Replace |     5 | 26.16 ns | 0.584 ns | 0.546 ns |  0.80 |    0.03 | 0.0114 |     - |     - |      72 B |
| AddOrGet_then_Replace_inlined_without_lambda_cost |     5 | 25.00 ns | 0.266 ns | 0.236 ns |  0.76 |    0.02 | 0.0114 |     - |     - |      72 B |
|                                                   |       |          |          |          |       |         |        |       |       |           |
|                              Get_then_AddOrUpdate |    10 | 51.65 ns | 1.085 ns | 0.962 ns |  1.00 |    0.00 | 0.0178 |     - |     - |     112 B |
|                             AddOrGet_then_Replace |    10 | 48.35 ns | 0.521 ns | 0.487 ns |  0.94 |    0.02 | 0.0178 |     - |     - |     112 B |
| AddOrGet_then_Replace_inlined_without_lambda_cost |    10 | 38.43 ns | 0.861 ns | 2.144 ns |  0.79 |    0.05 | 0.0178 |     - |     - |     112 B |
|                                                   |       |          |          |          |       |         |        |       |       |           |
|                              Get_then_AddOrUpdate |    50 | 91.46 ns | 1.870 ns | 2.079 ns |  1.00 |    0.00 | 0.0370 |     - |     - |     232 B |
|                             AddOrGet_then_Replace |    50 | 85.13 ns | 1.755 ns | 2.573 ns |  0.94 |    0.03 | 0.0370 |     - |     - |     232 B |
| AddOrGet_then_Replace_inlined_without_lambda_cost |    50 | 85.02 ns | 1.775 ns | 1.823 ns |  0.93 |    0.03 | 0.0370 |     - |     - |     232 B |

*/
            [Params(1, 5, 10)]//, 50, 100, 1_000)]
            public int Count;

            [GlobalSetup]
            public void Populate()
            {
                _map = V3_ImHashMap_AddOrUpdate();
            }

            [Benchmark(Baseline = true)]
            public object Get_then_AddOrUpdate()
            {
                var key = typeof(GlobalSetupAttribute);
                var hash = key.GetHashCode();
                var val = "!";
                var m = _map;
                var s = m.GetValueOrDefault(hash, key);
                if (s != null)
                    s = Handle(s, val);
                else
                    s = val;

                return m.AddOrUpdate(hash, key, s); 
            }

            [Benchmark]
            public object AddOrGet_then_Replace()
            {
                var key = typeof(GlobalSetupAttribute);
                var hash = key.GetHashCode();
                var val = "!";

                return _map.AddOrUpdate(hash, key, val, (_, o, n) => Handle(o, n));
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static string Handle(string a, string b) => a + b;

            private ImToolsV3.ImHashMap<Type, string> _map;
            public ImToolsV3.ImHashMap<Type, string> V3_ImHashMap_AddOrUpdate()
            {
                var map = ImToolsV3.ImHashMap<Type, string>.Empty;

                foreach (var key in _keys.Take(Count))
                    map = map.AddOrUpdate(key.GetHashCode(), key, "a");

                return map;
            }
          }
    }
}
