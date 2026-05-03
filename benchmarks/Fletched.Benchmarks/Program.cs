using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Fletched.Benchmarks.GeneratorBench).Assembly).Run(args);
