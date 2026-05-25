using System.Linq;
using Fletched.Core;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit;

namespace Fletched.Performance.Tests;

public class VariableScopeSemanticTests
{
    [Test]
    public async Task SemanticAnalyzer_WithFactType_ClassifiesSourceVariable()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct SourceFact(string Value);

[Predicate]
public partial record struct SourcePredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> value) =>
        Logic.With<SourceFact>(row => row.Value == value);
}
""";

        (PredicateModel? model, DiagnosticReporter reporter) = Analyze("SourcePredicate", source);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(model).IsNotNull();

        var withExpr = (WithExpr)model!.Body;
        await Assert.That(withExpr.Variables[0].Kind).IsEqualTo(VariableKind.Source);
    }

    [Test]
    public async Task SemanticAnalyzer_WithNonFactType_ClassifiesFreshVariable()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct FreshPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> value) =>
        Logic.With<string>(fresh => fresh == value);
}
""";

        (PredicateModel? model, DiagnosticReporter reporter) = Analyze("FreshPredicate", source);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(model).IsNotNull();

        var withExpr = (WithExpr)model!.Body;
        await Assert.That(withExpr.Variables[0].Kind).IsEqualTo(VariableKind.Fresh);
    }

    [Test]
    public async Task IrLowerer_WithFreshVariable_DoesNotEmitFactEnumerationLoop()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct FreshOnlyPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> value) =>
        Logic.With<string>(fresh => fresh == value);
}
""";

        (PredicateModel? model, DiagnosticReporter reporter) = Analyze("FreshOnlyPredicate", source);
        PlanProgram? plan = Lower(model, reporter);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(plan).IsNotNull();

        int loopInstructionCount = new[] { plan!.Entry }
            .Concat(plan.Blocks)
            .SelectMany(block => block.Instructions)
            .Count(instruction => instruction is IndexInitInstr or LoopBindInstr or IndexIncrInstr);

        await Assert.That(loopInstructionCount).IsEqualTo(0);
    }

    [Test]
    public async Task IrLowerer_WithSourceAndFresh_EnumeratesOnlySourceVariable()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct SourceFact(string Value);

[Predicate]
public partial record struct MixedPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> value) =>
        Logic.With<SourceFact, string>((row, fresh) => row.Value == fresh && fresh == value);
}
""";

        (PredicateModel? model, DiagnosticReporter reporter) = Analyze("MixedPredicate", source);
        PlanProgram? plan = Lower(model, reporter);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(plan).IsNotNull();

        int loopBindCount = new[] { plan!.Entry }
            .Concat(plan.Blocks)
            .SelectMany(block => block.Instructions)
            .Count(instruction => instruction is LoopBindInstr);

        await Assert.That(loopBindCount).IsEqualTo(1);
    }

    [Test]
    public async Task IrLowerer_FreshVariable_BindsViaPredicateInvocationCopyOut()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct OneOrTwo
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        value == 1 || value == 2;
}

[Predicate]
public partial record struct FreshCopyOutPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<int>(fresh =>
            OneOrTwo(fresh) &&
            value == fresh);
}
""";

        (PredicateModel? model, DiagnosticReporter reporter) = Analyze("FreshCopyOutPredicate", source);
        PlanProgram? plan = Lower(model, reporter);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(model).IsNotNull();
        await Assert.That(plan).IsNotNull();

        var withExpr = (WithExpr)model!.Body;
        await Assert.That(withExpr.Variables[0].Kind).IsEqualTo(VariableKind.Fresh);

        PlanInstruction[] instructions = new[] { plan!.Entry }
            .Concat(plan.Blocks)
            .SelectMany(block => block.Instructions)
            .ToArray();

        await Assert.That(instructions.Any(instruction => instruction is CallInstr)).IsTrue();
        await Assert.That(instructions.Any(instruction => instruction is LoopBindInstr or IndexInitInstr or IndexIncrInstr)).IsFalse();
    }

    [Test]
    public async Task SemanticAnalyzer_FreshVariableInNegation_ReportsFLG0001()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct FreshNegationPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> value) =>
        Logic.With<string>(fresh => Logic.Not(fresh == value) && value == "ok");
}
""";

        (_, DiagnosticReporter reporter) = Analyze("FreshNegationPredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UngroundedNegation.Id)).IsTrue();
    }

    [Test]
    public async Task SemanticAnalyzer_SourceVariableInNegation_DoesNotReportFLG0001()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct SourceFact(string Value);

[Predicate]
public partial record struct SourceNegationPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> value) =>
        Logic.With<SourceFact>(row => Logic.Not(row.Value == value));
}
""";

        (_, DiagnosticReporter reporter) = Analyze("SourceNegationPredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UngroundedNegation.Id)).IsFalse();
    }

    [Test]
    public async Task SemanticAnalyzer_ScopedVariableEscape_ReportsScopedEscapeDiagnostic()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct ScopedEscapePredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> value) =>
        Logic.With<string>(fresh => fresh == value) &&
        fresh == value;
}
""";

        (_, DiagnosticReporter reporter) = Analyze("ScopedEscapePredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.InvalidScopedVariableEscape.Id)).IsTrue();
    }

    [Test]
    public async Task SemanticAnalyzer_UnresolvedWithType_ReportsWithResolutionDiagnostic()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct UnresolvedWithTypePredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<MissingType>(fresh => fresh == value);
}
""";

        (_, DiagnosticReporter reporter) = Analyze("UnresolvedWithTypePredicate", source);

        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UnsupportedOrAmbiguousWithResolution.Id)).IsTrue();
    }

    [Test]
    public async Task SemanticAnalyzer_LogicOrInvocation_BuildsDisjunction()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct OrFactA(string Name);

[Fact]
public partial record struct OrFactB(string Name);

[Predicate]
public partial record struct OrPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.Or(
            () => Logic.With<OrFactA>(a => a.Name == name),
            () => Logic.With<OrFactB>(b => b.Name == name));
}
""";

        (PredicateModel? model, DiagnosticReporter reporter) = Analyze("OrPredicate", source);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(model).IsNotNull();
        await Assert.That(model!.Body).IsTypeOf<DisjExpr>();
    }

    [Test]
    public async Task SemanticAnalyzer_ExpandedArithmeticOperators_AreAccepted()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct NumberFact(int Value);

[Predicate]
public partial record struct ArithmeticPredicate
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<NumberFact>(n =>
            n.Value == value &&
            n.Value * 2 > 0 &&
            n.Value / 2 >= 0 &&
            n.Value % 2 == 0 &&
            -n.Value < 0);
}
""";

        (PredicateModel? model, DiagnosticReporter reporter) = Analyze("ArithmeticPredicate", source);
        PlanProgram? plan = Lower(model, reporter);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(model).IsNotNull();
        await Assert.That(plan).IsNotNull();
    }

    private static (PredicateModel? Model, DiagnosticReporter Reporter) Analyze(string predicateName, string source)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? predicateSymbol = compilation.GetTypeByMetadataName(predicateName);
        if (predicateSymbol is null)
            throw new InvalidOperationException($"Type '{predicateName}' not found.");

        SyntaxTree syntaxTree = compilation.SyntaxTrees.Single();
        SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
        var reporter = new DiagnosticReporter();
        var analyzer = new SemanticAnalyzer(semanticModel, reporter);
        PredicateModel? model = analyzer.Analyze(predicateSymbol);
        return (model, reporter);
    }

    private static PlanProgram? Lower(PredicateModel? model, DiagnosticReporter reporter)
    {
        if (model is null)
            return null;

        var lowerer = new IrLowerer(reporter);
        return lowerer.Lower(model);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.Preview));
        IReadOnlyList<MetadataReference> references = GetMetadataReferences();

        return CSharpCompilation.Create(
            "VariableScopeSemanticTests",
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
