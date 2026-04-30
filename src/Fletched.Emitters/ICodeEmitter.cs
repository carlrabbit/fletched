using Fletched.Core.Models;

namespace Fletched.Emitters;

/// <summary>Generates source code for a specific feature.</summary>
public interface ICodeEmitter
{
    /// <summary>The feature name this emitter handles.</summary>
    string Feature { get; }

    /// <summary>Emits the source code for the given generation request.</summary>
    string Emit(GenerationRequest request);
}
