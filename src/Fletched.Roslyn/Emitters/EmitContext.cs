using System;
using System.Text;

namespace Fletched.Roslyn.Emitters;

/// <summary>Context passed through all emitter stages for a single predicate.</summary>
public sealed class EmitContext
{
    public StringBuilder Code { get; } = new();
    public int IndentLevel { get; set; }
    public string PredicateName { get; }
    public string GeneratedName { get; }
    public string StateTypeName { get; }
    public string SlotIdTypeName { get; }

    public EmitContext(string predicateName, string generatedName)
    {
        PredicateName = predicateName;
        GeneratedName = generatedName;
        StateTypeName = $"{generatedName}_State";
        SlotIdTypeName = $"{generatedName}_SlotId";
    }

    public void AppendLine(string line = "")
    {
        if (line.Length == 0)
        {
            Code.AppendLine();
            return;
        }
        Code.Append(new string(' ', IndentLevel * 4));
        Code.AppendLine(line);
    }

    /// <summary>
    /// Emits a preprocessor directive at column zero (no indentation), as required by the
    /// C# specification.
    /// </summary>
    public void AppendDirective(string directive) => Code.AppendLine(directive);

    /// <summary>
    /// Emits a <c>#if METRICS … #endif</c> block that increments a counter on
    /// <see cref="global::Fletched.Core.Performance.EngineMetrics"/>. Use this instead of
    /// repeating the three-line pattern throughout the emitter.
    /// </summary>
    public void AppendMetricIncrement(string counterName)
    {
        AppendDirective("#if METRICS");
        AppendLine($"global::Fletched.Core.Performance.EngineMetrics.{counterName}.Add(1);");
        AppendDirective("#endif");
    }

    public IDisposable Indent()
    {
        IndentLevel++;
        return new IndentScope(this);
    }

    private sealed class IndentScope : IDisposable
    {
        private readonly EmitContext _ctx;
        public IndentScope(EmitContext ctx) => _ctx = ctx;
        public void Dispose() => _ctx.IndentLevel--;
    }
}
