using System.Linq;
using Fletched.Core;
using Fletched.Roslyn.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit;

namespace Fletched.Performance.Tests;

public class SourceSymbolValidatorTests
{
    [Test]
    public async Task ValidateModuleType_RejectsNonPartialStaticModule()
    {
        const string source = """
using Fletched.Core;

[Module]
public static class IdentityModule
{
}
""";

        DiagnosticReporter reporter = Validate("IdentityModule", source, static (validator, symbol) => validator.ValidateModuleType(symbol));

        await Assert.That(reporter.HasErrors).IsTrue();
        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.InvalidModuleType.Id)).IsTrue();
    }

    [Test]
    public async Task ValidateFactType_RejectsNonRecordStructFact()
    {
        const string source = """
using Fletched.Core;

[Fact]
public partial class Person
{
}
""";

        DiagnosticReporter reporter = Validate("Person", source, static (validator, symbol) => validator.ValidateFactType(symbol));

        await Assert.That(reporter.HasErrors).IsTrue();
        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.InvalidFactType.Id)).IsTrue();
    }

    [Test]
    public async Task ValidatePredicateType_RejectsNonRecordStructPredicate()
    {
        const string source = """
using Fletched.Core;

[Predicate]
public partial class PersonLookup
{
}
""";

        DiagnosticReporter reporter = Validate("PersonLookup", source, static (validator, symbol) => validator.ValidatePredicateType(symbol));

        await Assert.That(reporter.HasErrors).IsTrue();
        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.InvalidPredicateType.Id)).IsTrue();
    }

    [Test]
    public async Task ValidatePredicateType_RejectsNonPartialContainingType()
    {
        const string source = """
using Fletched.Core;

public class QueryScopes
{
    [Predicate]
    public partial record struct PersonLookup
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> name) => name == "Alice";
    }
}
""";

        DiagnosticReporter reporter = Validate("QueryScopes+PersonLookup", source, static (validator, symbol) => validator.ValidatePredicateType(symbol));

        await Assert.That(reporter.HasErrors).IsTrue();
        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.InvalidContainingType.Id)).IsTrue();
    }

    [Test]
    public async Task ValidateModuleType_AllowsStaticPartialModule()
    {
        const string source = """
using Fletched.Core;

[Module]
public static partial class IdentityModule
{
}
""";

        DiagnosticReporter reporter = Validate("IdentityModule", source, static (validator, symbol) => validator.ValidateModuleType(symbol));

        await Assert.That(reporter.HasErrors).IsFalse();
    }

    [Test]
    public async Task ValidateTabledPredicateOptions_RejectsSubsumptiveMode()
    {
        const string source = """
using Fletched.Core;

[Predicate]
[Tabled(TablingMode.Subsumptive)]
public partial record struct Reachable
{
}
""";

        DiagnosticReporter reporter = Validate("Reachable", source, static (validator, symbol) =>
        {
            validator.ValidateTabledPredicateOptions(symbol);
            return true;
        });

        await Assert.That(reporter.HasErrors).IsTrue();
        await Assert.That(reporter.Diagnostics.Any(d => d.Id == DiagnosticsCatalog.UnsupportedSubsumptiveTabling.Id)).IsTrue();
    }

    [Test]
    public async Task ValidateTabledPredicateOptions_AllowsVariantMode()
    {
        const string source = """
using Fletched.Core;

[Predicate]
[Tabled]
public partial record struct Reachable
{
}
""";

        DiagnosticReporter reporter = Validate("Reachable", source, static (validator, symbol) =>
        {
            validator.ValidateTabledPredicateOptions(symbol);
            return true;
        });

        await Assert.That(reporter.HasErrors).IsFalse();
    }

    private static DiagnosticReporter Validate(
        string metadataName,
        string source,
        Func<SourceSymbolValidator, INamedTypeSymbol, bool> validate)
    {
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName(metadataName);
        if (symbol is null)
            throw new InvalidOperationException($"Type '{metadataName}' not found.");

        var reporter = new DiagnosticReporter();
        var validator = new SourceSymbolValidator(reporter);
        validate(validator, symbol);
        return reporter;
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.Preview));
        IReadOnlyList<MetadataReference> references = GetMetadataReferences();

        return CSharpCompilation.Create(
            "SourceSymbolValidatorTests",
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
