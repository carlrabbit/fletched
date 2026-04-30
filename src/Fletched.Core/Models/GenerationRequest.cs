namespace Fletched.Core.Models;

/// <summary>A request to generate source code for a specific feature on a given type.</summary>
public sealed record GenerationRequest(TypeModel Target, string Feature);
