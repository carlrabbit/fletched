using BenchmarkDotNet.Running;

var summaries = BenchmarkSwitcher.FromAssembly(typeof(Fletched.Benchmarks.GeneratorBench).Assembly).Run(args);

bool hasBenchmarkFailures = summaries.Any(s => s.HasCriticalValidationErrors || s.Reports.Any(r => !r.Success));
Environment.Exit(hasBenchmarkFailures ? 1 : 0);
