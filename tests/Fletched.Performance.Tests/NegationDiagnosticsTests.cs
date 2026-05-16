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

    [Test]
    public async Task SemanticAnalyzer_NegationVariableEscape_ReportsFLG0002()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct NegationEscapePredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> number, TerminalVar<int> candidate) =>
        Logic.Not(number == candidate) &&
        candidate == 5;
}
""";

        DiagnosticReporter reporter = Analyze("NegationEscapePredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.NegationVariableEscape.Id)).IsTrue();
    }

    [Test]
    public async Task SemanticAnalyzer_RecursiveNegation_ReportsFLG0003()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct RecursiveNegationPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> number) =>
        number == 5 &&
        Logic.Not(RecursiveNegationPredicate(number));
}
""";

        DiagnosticReporter reporter = Analyze("RecursiveNegationPredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UnsupportedRecursiveNegation.Id)).IsTrue();
    }

    [Test]
    public async Task SemanticAnalyzer_TabledRecursiveNegation_ReportsFLT2002()
    {
        const string source = """
using Fletched.Core;

[Tabled]
[Predicate]
public partial record struct RecursiveNegationPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> number) =>
        number == 5 &&
        Logic.Not(RecursiveNegationPredicate(number));
}
""";

        DiagnosticReporter reporter = Analyze("RecursiveNegationPredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.InvalidTabledNegationCycle.Id)).IsTrue();
        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UnsupportedRecursiveNegation.Id)).IsFalse();
    }

    [Test]
    public async Task SemanticAnalyzer_NegationInvocationPattern_ReportsFLG0004()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct NumberIsOne
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        value == 1;
}

[Predicate]
public partial record struct UnsupportedInvocationPatternPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> number, TerminalVar<int> candidate) =>
        Logic.Not(NumberIsOne(candidate)) &&
        number == 1;
}
""";

        DiagnosticReporter reporter = Analyze("UnsupportedInvocationPatternPredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UnsupportedInvocationPatternInNegation.Id)).IsTrue();
    }

    [Test]
    public async Task SemanticAnalyzer_GroundedInvocationPatternInNegation_DoesNotReportFLG0004()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct NumberIsOne
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        value == 1;
}

[Predicate]
public partial record struct GroundedInvocationPatternPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> number) =>
        number == 2 &&
        Logic.Not(NumberIsOne(number));
}
""";

        DiagnosticReporter reporter = Analyze("GroundedInvocationPatternPredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UnsupportedInvocationPatternInNegation.Id)).IsFalse();
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
