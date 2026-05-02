using System;
using System.Text;

namespace Fletched.Roslyn.Emitters;

/// <summary>Context passed through all emitter stages for a single predicate.</summary>
public sealed class EmitContext
{
    public StringBuilder Code { get; } = new();
    public int IndentLevel { get; set; }
    public string PredicateName { get; }
    public string StateTypeName { get; }
    public string SlotIdTypeName { get; }

    public EmitContext(string predicateName)
    {
        PredicateName = predicateName;
        StateTypeName = $"{predicateName}_State";
        SlotIdTypeName = $"{predicateName}_SlotId";
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
