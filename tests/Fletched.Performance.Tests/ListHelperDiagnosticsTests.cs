using System.Linq;
using System.Reflection;
using Fletched.Core;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit;

namespace Fletched.Performance.Tests;

public class ListHelperDiagnosticsTests
{
    [Test]
    public async Task SemanticAnalyzer_LogicListHelper_LowersToListExpressions()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial record struct NumberSequence(string Name, LogicList<int> Numbers);

[Predicate]
public partial record struct PairSequence
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<NumberSequence>(ns =>
            ns.Name == name &&
            ns.Numbers == Logic.List(1, 2));
}
""";

        (PredicateModel? model, DiagnosticReporter reporter) = Analyze("PairSequence", source);

        await Assert.That(model).IsNotNull();
        await Assert.That(reporter.HasErrors).IsFalse();

        var withExpr = (WithExpr)model!.Body;
        var conjExpr = (ConjExpr)withExpr.Body;
        var unifyExpr = (UnifyExpr)conjExpr.Parts[1];
        await Assert.That(unifyExpr.Right).IsTypeOf<ListConsExpr>();
    }

    [Test]
    public async Task DiagnosticsCatalog_InvalidListExpression_UsesStableId()
    {
        await Assert.That(DiagnosticsCatalog.InvalidListExpression.Id).IsEqualTo("FLTCH010");
        await Assert.That(DiagnosticsCatalog.InvalidListExpression.Title.ToString()).IsEqualTo("Invalid list expression");
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

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.Preview));
        IReadOnlyList<MetadataReference> references = GetMetadataReferences();

        return CSharpCompilation.Create(
            "ListHelperTests",
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
