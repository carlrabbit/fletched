using System.Linq;
using Fletched.Core;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit;

namespace Fletched.Performance.Tests;

public class NegationDiagnosticsTests
{
    [Test]
    public async Task SemanticAnalyzer_UngroundedNegation_ReportsFLG0001()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct UngroundedNegationPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> number) =>
        Logic.Not(number == 5) &&
        number == 7;
}
""";

        DiagnosticReporter reporter = Analyze("UngroundedNegationPredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UngroundedNegation.Id)).IsTrue();
    }

    [Test]
    public async Task SemanticAnalyzer_GroundedNegation_DoesNotReportFLG0001()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct GroundedNegationPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> number) =>
        number == 5 &&
        Logic.Not(number == 7);
}
""";

        DiagnosticReporter reporter = Analyze("GroundedNegationPredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UngroundedNegation.Id)).IsFalse();
    }

    [Test]
    public async Task SemanticAnalyzer_GroundedNegationWithBitwiseAnd_DoesNotReportFLG0001()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct GroundedNegationBitwisePredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> number) =>
        (number == 5) &
        Logic.Not(number == 7);
}
""";

        DiagnosticReporter reporter = Analyze("GroundedNegationBitwisePredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UngroundedNegation.Id)).IsFalse();
    }

    private static DiagnosticReporter Analyze(string predicateName, string source)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? predicateSymbol = compilation.GetTypeByMetadataName(predicateName);
        if (predicateSymbol is null)
            throw new InvalidOperationException($"Type '{predicateName}' not found.");

        SyntaxTree syntaxTree = compilation.SyntaxTrees.Single();
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
        var reporter = new DiagnosticReporter();
        var analyzer = new SemanticAnalyzer(semanticModel, reporter);
        analyzer.Analyze(predicateSymbol);
        return reporter;
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.Preview));
        IReadOnlyList<MetadataReference> references = GetMetadataReferences();

        return CSharpCompilation.Create(
            "NegationDiagnosticsTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static IReadOnlyList<MetadataReference> GetMetadataReferences()
    {
        System.Reflection.Assembly[] assemblies =
        [
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(Logic).Assembly,
            System.Reflection.Assembly.Load("System.Runtime"),
            System.Reflection.Assembly.Load("netstandard"),
        ];

        return assemblies
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .ToList();
    }
}
