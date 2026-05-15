using System;
using System.Collections.Generic;
using System.Linq;
using Fletched.Core;
using Fletched.Roslyn.Emitters;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit;

namespace Fletched.Performance.Tests;

public class RecursivePredicateAnalysisTests
{
    [Test]
    public async Task PredicateCallGraph_DirectRecursivePredicate_IsClassifiedAsDirectRecursion()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct ParentLink(string Parent, string Child);

[Predicate]
public partial record struct DirectParent
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<ParentLink>(link => link.Parent == parent && link.Child == child);
}

[Predicate]
public partial record struct Ancestor
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        DirectParent(parent, child) ||
        Logic.With<string>(middle =>
            DirectParent(parent, middle) &&
            Ancestor(middle, child));
}
""";

        (IReadOnlyList<PredicateModel> models, DiagnosticReporter reporter) = AnalyzeAll(source);
        PredicateCallGraph graph = PredicateCallGraph.Create(models);
        PredicateCallGraphNode? node = graph.TryGetNode(models.Single(model => model.Name == "Ancestor").Symbol, arity: 2);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(node).IsNotNull();
        await Assert.That(graph.IsRecursive(node!)).IsTrue();
        await Assert.That(graph.IsDirectRecursive(node!)).IsTrue();
        await Assert.That(graph.IsMutuallyRecursive(node!)).IsFalse();
    }

    [Test]
    public async Task PredicateCallGraph_MutualRecursivePredicates_AreClassifiedAsMutualRecursion()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct ParentLink(string Parent, string Child);

[Predicate]
public partial record struct DirectParent
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<ParentLink>(link => link.Parent == parent && link.Child == child);
}

[Predicate]
public partial record struct EvenGeneration
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> ancestor, TerminalVar<string> descendant) =>
        ancestor == descendant ||
        Logic.With<string>(child =>
            DirectParent(ancestor, child) &&
            OddGeneration(child, descendant));
}

[Predicate]
public partial record struct OddGeneration
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> ancestor, TerminalVar<string> descendant) =>
        Logic.With<string>(child =>
            DirectParent(ancestor, child) &&
            EvenGeneration(child, descendant));
}
""";

        (IReadOnlyList<PredicateModel> models, DiagnosticReporter reporter) = AnalyzeAll(source);
        PredicateCallGraph graph = PredicateCallGraph.Create(models);
        PredicateCallGraphNode evenNode = graph.TryGetNode(models.Single(model => model.Name == "EvenGeneration").Symbol, arity: 2)!;
        PredicateCallGraphNode oddNode = graph.TryGetNode(models.Single(model => model.Name == "OddGeneration").Symbol, arity: 2)!;

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(graph.IsRecursive(evenNode)).IsTrue();
        await Assert.That(graph.IsDirectRecursive(evenNode)).IsFalse();
        await Assert.That(graph.IsMutuallyRecursive(evenNode)).IsTrue();
        await Assert.That(graph.IsRecursive(oddNode)).IsTrue();
        await Assert.That(graph.IsDirectRecursive(oddNode)).IsFalse();
        await Assert.That(graph.IsMutuallyRecursive(oddNode)).IsTrue();
    }

    [Test]
    public async Task PredicateRecursionValidator_MutualRecursiveNegation_ReportsFLG0003WithCyclePath()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial record struct A
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        value == 1 &&
        Logic.Not(B(value));
}

[Predicate]
public partial record struct B
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        A(value);
}
""";

        (IReadOnlyList<PredicateModel> models, DiagnosticReporter reporter) = AnalyzeAll(source);
        PredicateCallGraph graph = PredicateCallGraph.Create(models);

        PredicateRecursionValidator.ReportMutualNegativeCycles(
            graph,
            models.Where(model => model.Name == "A"),
            reporter);

        Diagnostic? diagnostic = reporter.Diagnostics.SingleOrDefault(d => d.Id == DiagnosticsCatalog.UnsupportedRecursiveNegation.Id);

        await Assert.That(diagnostic).IsNotNull();
        await Assert.That(diagnostic!.GetMessage().Contains("A/1 -not-> B/1 -> A/1", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task PredicateEmitter_RecursivePredicate_UsesExistingExecuteArityInvocation()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct ParentLink(string Parent, string Child);

[Predicate]
public partial record struct DirectParent
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        Logic.With<ParentLink>(link => link.Parent == parent && link.Child == child);
}

[Predicate]
public partial record struct Ancestor
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> parent, TerminalVar<string> child) =>
        DirectParent(parent, child) ||
        Logic.With<string>(middle =>
            DirectParent(parent, middle) &&
            Ancestor(middle, child));
}
""";

        (IReadOnlyList<PredicateModel> models, DiagnosticReporter reporter) = AnalyzeAll(source);
        PredicateModel model = models.Single(predicate => predicate.Name == "Ancestor");
        var lowerer = new IrLowerer(reporter);
        PlanProgram? plan = lowerer.Lower(model);

        await Assert.That(reporter.HasErrors).IsFalse();
        await Assert.That(plan).IsNotNull();

        PlanInstruction[] instructions = new[] { plan!.Entry }
            .Concat(plan.Blocks)
            .SelectMany(block => block.Instructions)
            .ToArray();

        await Assert.That(instructions.Any(instruction => instruction is CallInstr)).IsTrue();

        string generatedSource = new PredicateEmitter(model, plan!, generateLegacyNames: true).Emit();

        await Assert.That(generatedSource.Contains("ExecuteArity2(ctx, observer).GetEnumerator()", StringComparison.Ordinal)).IsTrue();
    }

    private static (IReadOnlyList<PredicateModel> Models, DiagnosticReporter Reporter) AnalyzeAll(string source)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        var reporter = new DiagnosticReporter();
        var models = new List<PredicateModel>();

        foreach (INamedTypeSymbol predicateType in EnumeratePredicateTypes(compilation.GlobalNamespace))
        {
            SyntaxReference syntaxReference = predicateType.DeclaringSyntaxReferences.First();
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            var analyzer = new SemanticAnalyzer(semanticModel, reporter);
            models.AddRange(analyzer.AnalyzeAll(predicateType));
        }

        return (models, reporter);
    }

    private static IEnumerable<INamedTypeSymbol> EnumeratePredicateTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (INamedTypeSymbol type in EnumerateTypes(namespaceSymbol))
        {
            if (type.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "PredicateAttribute"))
                yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceOrTypeSymbol container)
    {
        foreach (INamedTypeSymbol type in container.GetTypeMembers())
        {
            yield return type;

            foreach (INamedTypeSymbol nestedType in EnumerateTypes(type))
                yield return nestedType;
        }

        if (container is INamespaceSymbol namespaceSymbol)
        {
            foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                foreach (INamedTypeSymbol nestedType in EnumerateTypes(nestedNamespace))
                    yield return nestedType;
            }
        }
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.Preview));
        IReadOnlyList<MetadataReference> references = GetMetadataReferences();

        return CSharpCompilation.Create(
            "RecursivePredicateAnalysisTests",
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
