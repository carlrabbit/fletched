using System.Linq;
using Fletched.Core;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit;

namespace Fletched.Performance.Tests;

public class PredicateOverloadingTests
{
    [Test]
    public async Task SemanticAnalyzer_AnalyzeAll_AllowsDistinctArities()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct Person(string Login, string Name);

[Predicate]
public partial record struct PersonLookup
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<Person>(person => person.Name == name);

    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login, TerminalVar<string> name) =>
        Logic.With<Person>(person => person.Login == login && person.Name == name);
}
""";

        (IReadOnlyList<PredicateModel> models, DiagnosticReporter reporter) = AnalyzeAll("PersonLookup", source);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(models.Count).IsEqualTo(2);
        await Assert.That(models[0].Arity).IsEqualTo(1);
        await Assert.That(models[1].Arity).IsEqualTo(2);
    }

    [Test]
    public async Task SemanticAnalyzer_AnalyzeAll_RejectsDuplicateArities()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct PersonLookup
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> first) => first == "Alice";

    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> second) => second == "Bob";
}
""";

        (IReadOnlyList<PredicateModel> _, DiagnosticReporter reporter) = AnalyzeAll("PersonLookup", source);

        await Assert.That(reporter.HasErrors).IsTrue();
        await Assert.That(reporter.Diagnostics.Any(d =>
            d.Id == DiagnosticsCatalog.InvalidPredicateBody.Id &&
            d.GetMessage().Contains("arity 1", StringComparison.Ordinal))).IsTrue();
    }

    private static (IReadOnlyList<PredicateModel> Models, DiagnosticReporter Reporter) AnalyzeAll(
        string predicateName,
        string source)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? predicateSymbol = compilation.GetTypeByMetadataName(predicateName);
        if (predicateSymbol is null)
            throw new InvalidOperationException($"Type '{predicateName}' not found.");

        SyntaxTree syntaxTree = compilation.SyntaxTrees.Single();
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
        var reporter = new DiagnosticReporter();
        var analyzer = new SemanticAnalyzer(semanticModel, reporter);
        IReadOnlyList<PredicateModel> models = analyzer.AnalyzeAll(predicateSymbol);
        return (models, reporter);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.Preview));
        IReadOnlyList<MetadataReference> references = GetMetadataReferences();

        return CSharpCompilation.Create(
            "PredicateOverloadingTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static IReadOnlyList<MetadataReference> GetMetadataReferences()
    {
        System.Reflection.Assembly[] assemblies =
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Logic).Assembly,
            System.Reflection.Assembly.Load("System.Runtime"),
            System.Reflection.Assembly.Load("netstandard"),
        };

        return assemblies
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .ToList();
    }
}
