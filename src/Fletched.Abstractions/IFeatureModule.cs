using Microsoft.CodeAnalysis;
using Fletched.Core.Models;

namespace Fletched.Abstractions;

/// <summary>A pluggable feature module that contributes to the source generation pipeline.</summary>
public interface IFeatureModule
{
    /// <summary>Unique name identifying this feature.</summary>
    string Name { get; }

    /// <summary>Builds an incremental pipeline that produces <see cref="GenerationRequest"/> values for the given types.</summary>
    IncrementalValuesProvider<GenerationRequest> BuildPipeline(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<TypeModel> types);
}
