using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Fletched.Roslyn.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fletched.Roslyn.Pipeline;

public sealed record PlanExplanation(
    QueryIdentity Query,
    SemanticSummary Semantic,
    IrSummary Ir,
    PlannedIrSummary PlannedIr,
    RecursivePlanningSummary RecursivePlanning,
    OptimizationSummary Optimization,
    CodeEmissionSummary CodeEmission,
    ImmutableArray<DiagnosticExplanation> Diagnostics,
    RuntimeMetricsExplanation? RuntimeMetrics = null);

public sealed record QueryIdentity(
    string ContainingType,
    string PredicateName,
    int Arity,
    string? ModuleName,
    string SourceFile,
    TextSpan SourceSpan);

public sealed record SemanticSummary(
    ImmutableArray<FactBindingExplanation> FactBindings,
    ImmutableArray<PredicateBindingExplanation> PredicateBindings,
    ImmutableArray<VariableExplanation> Variables,
    ImmutableArray<BuiltinExplanation> Builtins);

public sealed record FactBindingExplanation(
    string FactType,
    string StorageScope,
    string ContextType,
    SourceLocation? Source);

public sealed record PredicateBindingExplanation(
    string PredicateName,
    int Arity,
    string StorageScope,
    string ResolvedSymbol,
    SourceLocation? Source);

public sealed record VariableExplanation(
    string Name,
    string Type,
    string SlotId,
    bool IsTerminal,
    bool IsProjection,
    SourceLocation? Source);

public sealed record BuiltinExplanation(
    string Name,
    string Kind,
    ImmutableArray<string> Arguments,
    SourceLocation? Source);

public sealed record IrSummary(
    string NormalizedText,
    ImmutableArray<IrNodeExplanation> Nodes);

public sealed record IrNodeExplanation(
    string Id,
    string Kind,
    string Text,
    SourceLocation? Source,
    ImmutableArray<string> Reads,
    ImmutableArray<string> Writes);

public sealed record PlannedIrSummary(
    string PlanText,
    ImmutableArray<PlanBlockExplanation> Blocks,
    ImmutableArray<SlotExplanation> Slots,
    ImmutableArray<AccessPathExplanation> AccessPaths);

public sealed record PlanBlockExplanation(
    string BlockId,
    string Kind,
    ImmutableArray<PlanInstructionExplanation> Instructions);

public sealed record PlanInstructionExplanation(
    string InstructionId,
    string Kind,
    string Text,
    SourceLocation? Source,
    ImmutableArray<string> Reads,
    ImmutableArray<string> Writes,
    bool MayFail,
    bool MayProduceMultipleResults);

public enum SlotKind
{
    Terminal,
    Source,
    Fresh,
    Temporary
}

public sealed record SlotExplanation(
    string SlotId,
    string Name,
    string Type,
    SlotKind Kind,
    bool IsTerminal,
    bool MustBeBoundForProjection);

public enum AccessPathKind
{
    FullFactScan,
    EqualityIndex,
    CompositeEqualityIndex,
    RangeIndex,
    MagicSourceLookup,
    TableLookup
}

public sealed record AccessPathExplanation(
    string Source,
    AccessPathKind Kind,
    string? IndexName,
    ImmutableArray<string> BoundInputs,
    string Reason);

public sealed record RecursivePlanningSummary(
    ImmutableArray<RecursivePredicateExplanation> Predicates,
    ImmutableArray<TableExplanation> Tables,
    ImmutableArray<MagicSetExplanation> MagicSets,
    ImmutableArray<RecursiveAccessPathExplanation> AccessPaths);

public sealed record RecursivePredicateExplanation(
    string Predicate,
    int Arity,
    string RecursionKind,
    string Decision,
    string Reason);

public sealed record TableExplanation(
    string Predicate,
    int Arity,
    string VariantKey,
    string Reason);

public sealed record MagicSetExplanation(
    string Predicate,
    string Adornment,
    ImmutableArray<string> Seeds,
    ImmutableArray<string> MagicPredicates,
    ImmutableArray<string> ModifiedRules,
    string Decision,
    string Reason);

public sealed record RecursiveAccessPathExplanation(
    string Predicate,
    string Adornment,
    string AccessPathKind,
    ImmutableArray<string> BoundInputs,
    string Reason);

public sealed record OptimizationSummary(
    ImmutableArray<OptimizationPassExplanation> Passes);

public sealed record OptimizationPassExplanation(
    string PassName,
    string InputHash,
    string OutputHash,
    ImmutableArray<OptimizationChangeExplanation> Changes);

public sealed record OptimizationChangeExplanation(
    PlanChangeKind Kind,
    string Target,
    string Reason,
    SourceLocation? Source,
    string? Before,
    string? After);

public sealed record CodeEmissionSummary(
    string ResultTypeName,
    string StateTypeName,
    string SlotEnumName,
    ImmutableArray<GeneratedMemberExplanation> Members,
    ImmutableArray<CodegenDecisionExplanation> Decisions);

public sealed record GeneratedMemberExplanation(
    string Name,
    string Kind,
    string Reason);

public sealed record CodegenDecisionExplanation(
    string Decision,
    string Reason,
    SourceLocation? Source);

public sealed record DiagnosticExplanation(
    string Id,
    string Severity,
    string Message,
    SourceLocation Source,
    DiagnosticPhase Phase,
    string Reason,
    ImmutableArray<string> RelatedSymbols,
    ImmutableArray<string> SuggestedFixes);

public sealed record RuntimeMetricsExplanation(
    global::Fletched.Core.Runtime.QueryMetricsSnapshot Metrics,
    ImmutableArray<MetricInterpretation> Interpretations);

public sealed record MetricInterpretation(
    string Name,
    string Value,
    string Reason);

public enum DiagnosticPhase
{
    SyntaxDiscovery,
    SemanticBinding,
    DslValidation,
    IrLowering,
    Planning,
    RecursivePlanning,
    Optimization,
    CodeEmission
}

public sealed record SourceLocation(
    string SourceFile,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn,
    TextSpan Span);

public interface IPlanExplanationRenderer
{
    string RenderPlainText(PlanExplanation explanation);
    string RenderMarkdown(PlanExplanation explanation);
    string RenderJson(PlanExplanation explanation);
}

public sealed class PlanExplanationRenderer : IPlanExplanationRenderer
{
    public string RenderPlainText(PlanExplanation explanation)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"QUERY {explanation.Query.PredicateName}/{explanation.Query.Arity}");
        sb.AppendLine($"MODULE {explanation.Query.ModuleName ?? "<global>"}");
        sb.AppendLine();

        sb.AppendLine("SEMANTIC");
        foreach (VariableExplanation variable in explanation.Semantic.Variables)
            sb.AppendLine($"  variable {variable.Name}: {variable.Type} -> {variable.SlotId} terminal={variable.IsTerminal} projection={variable.IsProjection}");
        foreach (FactBindingExplanation fact in explanation.Semantic.FactBindings)
            sb.AppendLine($"  fact {fact.FactType} [{fact.StorageScope}] via {fact.ContextType}");
        foreach (PredicateBindingExplanation binding in explanation.Semantic.PredicateBindings)
            sb.AppendLine($"  call {binding.PredicateName}/{binding.Arity} -> {binding.ResolvedSymbol}");
        foreach (BuiltinExplanation builtin in explanation.Semantic.Builtins)
            sb.AppendLine($"  builtin {builtin.Name} ({builtin.Kind}) args=[{string.Join(", ", builtin.Arguments)}]");
        sb.AppendLine();

        sb.AppendLine("IR");
        sb.AppendLine(explanation.Ir.NormalizedText);
        sb.AppendLine();

        sb.AppendLine("PLAN");
        sb.AppendLine(explanation.PlannedIr.PlanText);
        sb.AppendLine();

        sb.AppendLine("ACCESS PATHS");
        foreach (AccessPathExplanation accessPath in explanation.PlannedIr.AccessPaths)
        {
            sb.AppendLine($"  {accessPath.Source}: {accessPath.Kind}{(string.IsNullOrWhiteSpace(accessPath.IndexName) ? string.Empty : $"({accessPath.IndexName})")}");
            sb.AppendLine($"    bound: {string.Join(", ", accessPath.BoundInputs)}");
            sb.AppendLine($"    reason: {accessPath.Reason}");
        }

        sb.AppendLine();
        sb.AppendLine("RECURSIVE PLANNING");
        if (explanation.RecursivePlanning.Predicates.Length == 0)
        {
            sb.AppendLine("  none");
        }
        else
        {
            foreach (RecursivePredicateExplanation recursive in explanation.RecursivePlanning.Predicates)
                sb.AppendLine($"  {recursive.Predicate}/{recursive.Arity}: {recursive.Decision} ({recursive.Reason})");
        }

        sb.AppendLine();
        sb.AppendLine("OPTIMIZATION");
        foreach (OptimizationPassExplanation pass in explanation.Optimization.Passes)
        {
            sb.AppendLine($"  {pass.PassName} [{pass.InputHash}->{pass.OutputHash}]");
            foreach (OptimizationChangeExplanation change in pass.Changes)
                sb.AppendLine($"    {change.Kind}: {change.Target} ({change.Reason})");
        }

        sb.AppendLine();
        sb.AppendLine("CODE EMISSION");
        sb.AppendLine($"  result: {explanation.CodeEmission.ResultTypeName}");
        sb.AppendLine($"  state: {explanation.CodeEmission.StateTypeName}");
        sb.AppendLine($"  slots: {explanation.CodeEmission.SlotEnumName}");
        foreach (GeneratedMemberExplanation member in explanation.CodeEmission.Members)
            sb.AppendLine($"  member {member.Kind} {member.Name}: {member.Reason}");

        sb.AppendLine();
        sb.AppendLine("DIAGNOSTICS");
        foreach (DiagnosticExplanation diagnostic in explanation.Diagnostics)
            sb.AppendLine($"  {diagnostic.Id} [{diagnostic.Phase}] {diagnostic.Message}");

        if (explanation.RuntimeMetrics is not null)
        {
            sb.AppendLine();
            sb.AppendLine("RUNTIME METRICS");
            global::Fletched.Core.Runtime.QueryMetricsSnapshot metrics = explanation.RuntimeMetrics.Metrics;
            sb.AppendLine($"  FactRowsScanned: {metrics.FactRowsScanned}");
            sb.AppendLine($"  IndexLookups: {metrics.IndexLookups}");
            sb.AppendLine($"  IndexHits: {metrics.IndexHits}");
            sb.AppendLine($"  IndexMisses: {metrics.IndexMisses}");
            sb.AppendLine($"  UnificationAttempts: {metrics.UnificationAttempts}");
            sb.AppendLine($"  UnificationSuccesses: {metrics.UnificationSuccesses}");
            sb.AppendLine($"  UnificationFailures: {metrics.UnificationFailures}");
            sb.AppendLine($"  ConstraintEvaluations: {metrics.ConstraintEvaluations}");
            sb.AppendLine($"  ConstraintFailures: {metrics.ConstraintFailures}");
            sb.AppendLine($"  PredicateCalls: {metrics.PredicateCalls}");
            sb.AppendLine($"  PredicateCallResults: {metrics.PredicateCallResults}");
            sb.AppendLine($"  Backtracks: {metrics.Backtracks}");
            sb.AppendLine($"  ResultsEmitted: {metrics.ResultsEmitted}");
            sb.AppendLine($"  TableProbes: {metrics.TableProbes}");
            sb.AppendLine($"  TableHits: {metrics.TableHits}");
            sb.AppendLine($"  TableMisses: {metrics.TableMisses}");
            sb.AppendLine($"  TableInserts: {metrics.TableInserts}");
            sb.AppendLine($"  MagicSourceProbes: {metrics.MagicSourceProbes}");
            sb.AppendLine($"  MagicSourceHits: {metrics.MagicSourceHits}");
            sb.AppendLine($"  MagicSourceMisses: {metrics.MagicSourceMisses}");
        }

        return sb.ToString().TrimEnd();
    }

    public string RenderMarkdown(PlanExplanation explanation)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Query `{explanation.Query.PredicateName}/{explanation.Query.Arity}`");
        sb.AppendLine();
        sb.AppendLine($"- Module: `{explanation.Query.ModuleName ?? "<global>"}`");
        sb.AppendLine($"- Source: `{explanation.Query.SourceFile}`");
        sb.AppendLine();

        sb.AppendLine("## Semantic");
        sb.AppendLine();
        sb.AppendLine("| Variable | Type | Slot | Terminal | Projection |");
        sb.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (VariableExplanation variable in explanation.Semantic.Variables)
            sb.AppendLine($"| {variable.Name} | {variable.Type} | {variable.SlotId} | {variable.IsTerminal} | {variable.IsProjection} |");
        sb.AppendLine();

        sb.AppendLine("## IR");
        sb.AppendLine("```text");
        sb.AppendLine(explanation.Ir.NormalizedText);
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Plan");
        sb.AppendLine("```text");
        sb.AppendLine(explanation.PlannedIr.PlanText);
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Optimization");
        foreach (OptimizationPassExplanation pass in explanation.Optimization.Passes)
        {
            sb.AppendLine($"- **{pass.PassName}** `{pass.InputHash}` → `{pass.OutputHash}`");
            foreach (OptimizationChangeExplanation change in pass.Changes)
                sb.AppendLine($"  - {change.Kind}: `{change.Target}` — {change.Reason}");
        }
        sb.AppendLine();

        sb.AppendLine("## Diagnostics");
        foreach (DiagnosticExplanation diagnostic in explanation.Diagnostics)
            sb.AppendLine($"- `{diagnostic.Id}` **{diagnostic.Phase}**: {diagnostic.Message}");

        if (explanation.RuntimeMetrics is not null)
        {
            global::Fletched.Core.Runtime.QueryMetricsSnapshot metrics = explanation.RuntimeMetrics.Metrics;
            sb.AppendLine();
            sb.AppendLine("## Runtime metrics");
            sb.AppendLine();
            sb.AppendLine("| Counter | Value |");
            sb.AppendLine("| --- | --- |");
            sb.AppendLine($"| FactRowsScanned | {metrics.FactRowsScanned} |");
            sb.AppendLine($"| IndexLookups | {metrics.IndexLookups} |");
            sb.AppendLine($"| IndexHits | {metrics.IndexHits} |");
            sb.AppendLine($"| IndexMisses | {metrics.IndexMisses} |");
            sb.AppendLine($"| UnificationAttempts | {metrics.UnificationAttempts} |");
            sb.AppendLine($"| UnificationSuccesses | {metrics.UnificationSuccesses} |");
            sb.AppendLine($"| UnificationFailures | {metrics.UnificationFailures} |");
            sb.AppendLine($"| ConstraintEvaluations | {metrics.ConstraintEvaluations} |");
            sb.AppendLine($"| ConstraintFailures | {metrics.ConstraintFailures} |");
            sb.AppendLine($"| PredicateCalls | {metrics.PredicateCalls} |");
            sb.AppendLine($"| PredicateCallResults | {metrics.PredicateCallResults} |");
            sb.AppendLine($"| Backtracks | {metrics.Backtracks} |");
            sb.AppendLine($"| ResultsEmitted | {metrics.ResultsEmitted} |");
            sb.AppendLine($"| TableProbes | {metrics.TableProbes} |");
            sb.AppendLine($"| TableHits | {metrics.TableHits} |");
            sb.AppendLine($"| TableMisses | {metrics.TableMisses} |");
            sb.AppendLine($"| TableInserts | {metrics.TableInserts} |");
            sb.AppendLine($"| MagicSourceProbes | {metrics.MagicSourceProbes} |");
            sb.AppendLine($"| MagicSourceHits | {metrics.MagicSourceHits} |");
            sb.AppendLine($"| MagicSourceMisses | {metrics.MagicSourceMisses} |");
        }

        return sb.ToString().TrimEnd();
    }

    public string RenderJson(PlanExplanation explanation)
    {
        return JsonSerializer.Serialize(
            explanation,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null
            });
    }
}

internal sealed class PlanningExplanationBuilder
{
    private readonly IPlanExplanationRenderer _renderer;

    public PlanningExplanationBuilder(IPlanExplanationRenderer? renderer = null)
    {
        _renderer = renderer ?? new PlanExplanationRenderer();
    }

    public PlanExplanation Build(
        PredicateModel model,
        PlanProgram plan,
        PlanOptimizationTrace? optimizationTrace,
        IReadOnlyList<Diagnostic> diagnostics,
        bool generateLegacyNames = true)
    {
        QueryIdentity queryIdentity = BuildQueryIdentity(model);
        SemanticSummary semantic = BuildSemanticSummary(model, plan);
        IrSummary ir = BuildIrSummary(model);
        PlannedIrSummary plannedIr = BuildPlannedIrSummary(model, plan);
        RecursivePlanningSummary recursivePlanning = BuildRecursivePlanningSummary(model, plan);
        OptimizationSummary optimization = BuildOptimizationSummary(model, optimizationTrace);
        CodeEmissionSummary codeEmission = BuildCodeEmissionSummary(model, plan, generateLegacyNames);
        ImmutableArray<DiagnosticExplanation> diagnosticExplanations = BuildDiagnostics(diagnostics);

        return new PlanExplanation(
            queryIdentity,
            semantic,
            ir,
            plannedIr,
            recursivePlanning,
            optimization,
            codeEmission,
            diagnosticExplanations);
    }

    public string RenderPlainText(PlanExplanation explanation) => _renderer.RenderPlainText(explanation);

    public string RenderMarkdown(PlanExplanation explanation) => _renderer.RenderMarkdown(explanation);

    public string RenderJson(PlanExplanation explanation) => _renderer.RenderJson(explanation);

    private static QueryIdentity BuildQueryIdentity(PredicateModel model)
    {
        INamedTypeSymbol? moduleRoot = SourceSymbolHelpers.GetModuleRoot(model.Symbol);
        string sourceFile = string.Empty;
        TextSpan sourceSpan = default;
        if (model.BodyMethod.Locations.FirstOrDefault(location => location.IsInSource) is { } bodyLocation)
        {
            sourceFile = Path.GetFileName(bodyLocation.SourceTree?.FilePath ?? string.Empty);
            sourceSpan = bodyLocation.SourceSpan;
        }

        return new QueryIdentity(
            model.Symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? model.Symbol.Name,
            model.Name,
            model.Arity,
            moduleRoot?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            sourceFile,
            sourceSpan);
    }

    private static SemanticSummary BuildSemanticSummary(PredicateModel model, PlanProgram plan)
    {
        var factBindings = new List<FactBindingExplanation>();
        var predicateBindings = new List<PredicateBindingExplanation>();
        var builtins = new List<BuiltinExplanation>();
        var variables = new Dictionary<string, VariableExplanation>(StringComparer.Ordinal);

        string contextType = SourceSymbolHelpers.GetContextTypeName(model.Symbol);
        string storageScope = contextType.Contains("EngineContext", StringComparison.Ordinal)
            ? "global"
            : "module";

        foreach (VariableSymbol parameter in model.Parameters)
            AddVariable(parameter, plan, isProjection: true, variables);

        TraverseSemanticExpr(
            model.Body,
            onWith: withExpr =>
            {
                foreach (VariableSymbol variable in withExpr.Variables)
                {
                    AddVariable(variable, plan, isProjection: false, variables);
                    if (variable.Kind == VariableKind.Source)
                    {
                        factBindings.Add(new FactBindingExplanation(
                            variable.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                            storageScope,
                            contextType,
                            null));
                    }
                }
            },
            onCall: callExpr => predicateBindings.Add(new PredicateBindingExplanation(
                callExpr.PredicateType.Name,
                callExpr.Arity,
                storageScope,
                callExpr.PredicateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                null)),
            onConstraint: constraintExpr => builtins.Add(new BuiltinExplanation(
                constraintExpr.Method.Name,
                "ClrConstraint",
                constraintExpr.Arguments.Select(RenderSemanticValue).ToImmutableArray(),
                null)),
            onVar: variable => AddVariable(variable, plan, isProjection: model.Parameters.Any(parameter => parameter.Name == variable.Name), variables));

        return new SemanticSummary(
            factBindings
                .OrderBy(binding => binding.FactType, StringComparer.Ordinal)
                .ThenBy(binding => binding.ContextType, StringComparer.Ordinal)
                .ToImmutableArray(),
            predicateBindings
                .OrderBy(binding => binding.PredicateName, StringComparer.Ordinal)
                .ThenBy(binding => binding.Arity)
                .ToImmutableArray(),
            variables.Values
                .OrderBy(variable => variable.SlotId, StringComparer.Ordinal)
                .ThenBy(variable => variable.Name, StringComparer.Ordinal)
                .ToImmutableArray(),
            builtins
                .OrderBy(builtin => builtin.Name, StringComparer.Ordinal)
                .ThenBy(builtin => string.Join("|", builtin.Arguments), StringComparer.Ordinal)
                .ToImmutableArray());
    }

    private static void AddVariable(
        VariableSymbol variable,
        PlanProgram plan,
        bool isProjection,
        IDictionary<string, VariableExplanation> variables)
    {
        int slot = plan.SlotMap
            .Where(entry => entry.Key.Name == variable.Name)
            .Select(entry => entry.Value)
            .DefaultIfEmpty(-1)
            .Min();

        string slotId = slot >= 0 ? $"s{slot}" : "s?";

        variables[variable.Name] = new VariableExplanation(
            variable.Name,
            variable.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            slotId,
            variable.Kind == VariableKind.Terminal,
            isProjection,
            null);
    }

    private static IrSummary BuildIrSummary(PredicateModel model)
    {
        var nodes = new List<IrNodeExplanation>();
        int counter = 0;

        TraverseSemanticExpr(model.Body, onExpr: expr =>
        {
            string id = $"n{counter++}";
            IReadOnlyCollection<string> reads = GetSemanticReads(expr);
            IReadOnlyCollection<string> writes = GetSemanticWrites(expr);
            nodes.Add(new IrNodeExplanation(
                id,
                GetSemanticKind(expr),
                RenderSemanticValue(expr),
                null,
                reads.OrderBy(value => value, StringComparer.Ordinal).ToImmutableArray(),
                writes.OrderBy(value => value, StringComparer.Ordinal).ToImmutableArray()));
        });

        return new IrSummary(RenderSemanticValue(model.Body), nodes.ToImmutableArray());
    }

    private static PlannedIrSummary BuildPlannedIrSummary(PredicateModel model, PlanProgram plan)
    {
        IReadOnlyList<PlanBlock> blocks = PlanAnalysis.AllBlocks(plan);
        var blockExplanations = new List<PlanBlockExplanation>(blocks.Count);
        var accessPaths = new List<AccessPathExplanation>();
        var usedSlots = new SortedDictionary<int, (string Name, string Type, SlotKind Kind, bool IsTerminal, bool MustBeBoundForProjection)>();

        for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
        {
            PlanBlock block = blocks[blockIndex];
            string blockId = $"b{blockIndex}";
            var instructionExplanations = new List<PlanInstructionExplanation>(block.Instructions.Count);

            for (int instructionIndex = 0; instructionIndex < block.Instructions.Count; instructionIndex++)
            {
                PlanInstruction instruction = block.Instructions[instructionIndex];
                string instructionId = $"i{blockIndex}_{instructionIndex}";
                AccessSet access = PlanAnalysis.AnalyzeInstruction(instruction);
                RegisterSlots(usedSlots, plan, model, instruction);

                string instructionKind = instruction.GetType().Name;
                if (instructionKind.EndsWith("Instr", StringComparison.Ordinal))
                    instructionKind = instructionKind.Substring(0, instructionKind.Length - "Instr".Length);

                instructionExplanations.Add(new PlanInstructionExplanation(
                    instructionId,
                    instructionKind,
                    RenderInstruction(instruction),
                    null,
                    access.Reads.Select(slot => $"s{slot}").OrderBy(slot => slot, StringComparer.Ordinal).ToImmutableArray(),
                    access.Writes.Select(slot => $"s{slot}").OrderBy(slot => slot, StringComparer.Ordinal).ToImmutableArray(),
                    MayFail(instruction),
                    MayProduceMultipleResults(instruction)));

                if (instruction is IndexInitInstr fullScan && fullScan.IndexedLookup is null)
                {
                    accessPaths.Add(new AccessPathExplanation(
                        $"FactTable<{fullScan.FactType.Name}>",
                        AccessPathKind.FullFactScan,
                        null,
                        ImmutableArray<string>.Empty,
                        "no bound indexed field available"));
                }
                else if (instruction is IndexInitInstr indexed)
                {
                    accessPaths.Add(new AccessPathExplanation(
                        $"FactTable<{indexed.FactType.Name}>",
                        indexed.IndexedLookup?.AccessPathKind switch
                        {
                            FactAccessPathKind.CompositeEqualityIndex => AccessPathKind.CompositeEqualityIndex,
                            FactAccessPathKind.RangeIndex => AccessPathKind.RangeIndex,
                            FactAccessPathKind.EqualityIndex => AccessPathKind.EqualityIndex,
                            _ => AccessPathKind.FullFactScan
                        },
                        indexed.IndexedLookup?.IndexName,
                        indexed.IndexedLookup is null
                            ? ImmutableArray<string>.Empty
                            : indexed.IndexedLookup.BoundInputNames,
                        indexed.IndexedLookup is null
                            ? "fallback to full scan"
                            : indexed.IndexedLookup.Reason));
                }
                else if (instruction is CallInstr call && call.IsTabledCall)
                {
                    accessPaths.Add(new AccessPathExplanation(
                        call.PredicateType.Name,
                        AccessPathKind.TableLookup,
                        null,
                        call.ArgumentSlots.Select(slot => $"s{slot}").ToImmutableArray(),
                        "recursive tabled call uses table lookup"));
                }
            }

            blockExplanations.Add(new PlanBlockExplanation(
                blockId,
                block == plan.Entry ? "Entry" : "Block",
                instructionExplanations.ToImmutableArray()));
        }

        if (plan.Metadata is not null)
        {
            foreach (RecursiveAccessPathPlan path in plan.Metadata.AccessPaths.OrderBy(p => p.Label, StringComparer.Ordinal))
            {
                accessPaths.Add(new AccessPathExplanation(
                    path.TargetName,
                    path.Kind switch
                    {
                        RecursiveAccessPathKind.FullFactScan => AccessPathKind.FullFactScan,
                        RecursiveAccessPathKind.IndexedFactLookup => AccessPathKind.EqualityIndex,
                        RecursiveAccessPathKind.MagicSourceLookup => AccessPathKind.MagicSourceLookup,
                        _ => AccessPathKind.TableLookup
                    },
                    null,
                    ImmutableArray<string>.Empty,
                    "recursive planning metadata"));
            }
        }

        var slotExplanations = usedSlots
            .Select(entry => new SlotExplanation(
                $"s{entry.Key}",
                entry.Value.Name,
                entry.Value.Type,
                entry.Value.Kind,
                entry.Value.IsTerminal,
                entry.Value.MustBeBoundForProjection))
            .ToImmutableArray();

        return new PlannedIrSummary(
            RenderPlanText(plan),
            blockExplanations.ToImmutableArray(),
            slotExplanations,
            accessPaths
                .OrderBy(path => path.Source, StringComparer.Ordinal)
                .ThenBy(path => path.Kind)
                .ThenBy(path => path.IndexName, StringComparer.Ordinal)
                .ToImmutableArray());
    }

    private static void RegisterSlots(
        SortedDictionary<int, (string Name, string Type, SlotKind Kind, bool IsTerminal, bool MustBeBoundForProjection)> slots,
        PlanProgram plan,
        PredicateModel model,
        PlanInstruction instruction)
    {
        AccessSet access = PlanAnalysis.AnalyzeInstruction(instruction);
        foreach (int slot in access.Reads.Concat(access.Writes).Distinct())
            RegisterSlot(slots, plan, model, slot);
    }

    private static void RegisterSlot(
        SortedDictionary<int, (string Name, string Type, SlotKind Kind, bool IsTerminal, bool MustBeBoundForProjection)> slots,
        PlanProgram plan,
        PredicateModel model,
        int slot)
    {
        if (slots.ContainsKey(slot))
            return;

        foreach (KeyValuePair<VariableSymbol, int> entry in plan.SlotMap)
        {
            if (entry.Value != slot)
                continue;

            VariableSymbol variable = entry.Key;
            bool isProjection = model.Parameters.Any(parameter => parameter.Name == variable.Name);
            slots[slot] = (
                variable.Name,
                variable.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                variable.Kind switch
                {
                    VariableKind.Terminal => SlotKind.Terminal,
                    VariableKind.Source => SlotKind.Source,
                    VariableKind.Fresh => SlotKind.Fresh,
                    _ => SlotKind.Temporary
                },
                variable.Kind == VariableKind.Terminal,
                isProjection);
            return;
        }

        slots[slot] = ($"_slot{slot}", "object?", SlotKind.Temporary, false, false);
    }

    private static RecursivePlanningSummary BuildRecursivePlanningSummary(PredicateModel model, PlanProgram plan)
    {
        if (plan.Metadata is null)
        {
            return new RecursivePlanningSummary(
                ImmutableArray<RecursivePredicateExplanation>.Empty,
                ImmutableArray<TableExplanation>.Empty,
                ImmutableArray<MagicSetExplanation>.Empty,
                ImmutableArray<RecursiveAccessPathExplanation>.Empty);
        }

        ImmutableArray<RecursivePredicateExplanation> predicates = plan.Metadata.RecursiveCalls
            .Select(call => new RecursivePredicateExplanation(
                call.TargetPredicateName,
                call.Adornment.Pattern.Length,
                call.IsInsideNegation ? "NegativeRecursiveCall" : "PositiveRecursiveCall",
                call.IsTabledCall ? "Tabled" : "Untabled",
                call.IsInsideNegation
                    ? "recursive negation requires conservative planning"
                    : call.IsTabledCall
                        ? "recursive call marked as table boundary"
                        : "recursive call detected"))
            .OrderBy(predicate => predicate.Predicate, StringComparer.Ordinal)
            .ThenBy(predicate => predicate.Arity)
            .ToImmutableArray();

        ImmutableArray<TableExplanation> tables = plan.Metadata.RecursiveCalls
            .Where(call => call.IsTabledCall)
            .Select(call => new TableExplanation(
                call.TargetPredicateName,
                call.Adornment.Pattern.Length,
                call.Adornment.Pattern,
                "table selected for recursive call"))
            .Distinct()
            .OrderBy(table => table.Predicate, StringComparer.Ordinal)
            .ToImmutableArray();

        ImmutableArray<MagicSetExplanation> magicSets = plan.Metadata.MagicPredicates
            .Select(predicate => new MagicSetExplanation(
                predicate.PredicateName,
                predicate.Adornment.Pattern,
                plan.Metadata.MagicSeeds
                    .Where(seed => seed.TargetPredicateName == predicate.PredicateName && seed.Adornment == predicate.Adornment)
                    .Select(seed => seed.CallingPredicateName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(seed => seed, StringComparer.Ordinal)
                    .ToImmutableArray(),
                ImmutableArray.Create(predicate.MagicPredicateName),
                plan.Metadata.ModifiedRules
                    .Where(rule => rule.PredicateName == predicate.PredicateName && rule.Adornment == predicate.Adornment)
                    .Select(rule => rule.MagicPredicateName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToImmutableArray(),
                predicate.Adornment.IsAllFree ? "skipped" : "applied",
                predicate.Adornment.IsAllFree
                    ? "all-free adornment has no bound seed"
                    : "bound adornment produced magic seed"))
            .OrderBy(magic => magic.Predicate, StringComparer.Ordinal)
            .ThenBy(magic => magic.Adornment, StringComparer.Ordinal)
            .ToImmutableArray();

        ImmutableArray<RecursiveAccessPathExplanation> accessPaths = plan.Metadata.AccessPaths
            .Select(path => new RecursiveAccessPathExplanation(
                path.TargetName,
                plan.Metadata.EntryAdornment.Pattern,
                path.Kind.ToString(),
                ImmutableArray<string>.Empty,
                "selected by recursive planner"))
            .OrderBy(path => path.Predicate, StringComparer.Ordinal)
            .ThenBy(path => path.AccessPathKind, StringComparer.Ordinal)
            .ToImmutableArray();

        return new RecursivePlanningSummary(predicates, tables, magicSets, accessPaths);
    }

    private static OptimizationSummary BuildOptimizationSummary(PredicateModel model, PlanOptimizationTrace? optimizationTrace)
    {
        if (optimizationTrace is null)
            return new OptimizationSummary(ImmutableArray<OptimizationPassExplanation>.Empty);

        ImmutableArray<OptimizationPassExplanation> passes = optimizationTrace.Passes
            .Select(pass => new OptimizationPassExplanation(
                pass.PassName,
                pass.InputHash,
                pass.OutputHash,
                pass.Changes
                    .Select(change => new OptimizationChangeExplanation(
                        change.Kind,
                        change.Target,
                        change.Reason,
                        GetSourceLocation(model.BodyMethod.Locations.FirstOrDefault(location => location.IsInSource)),
                        null,
                        null))
                    .ToImmutableArray()))
            .ToImmutableArray();

        return new OptimizationSummary(passes);
    }

    private static CodeEmissionSummary BuildCodeEmissionSummary(PredicateModel model, PlanProgram plan, bool generateLegacyNames)
    {
        string generatedName = generateLegacyNames ? model.Name : $"{model.Name}Arity{model.Arity}";
        string resultTypeName = generateLegacyNames ? $"{model.Name}Result" : $"{generatedName}Result";
        string stateTypeName = $"{generatedName}_State";
        string slotEnumName = $"{generatedName}_SlotId";

        var members = new List<GeneratedMemberExplanation>
        {
            new(resultTypeName, "ResultType", "generated result projection type"),
            new(stateTypeName, "StateType", "generated execution state type"),
            new(slotEnumName, "SlotEnum", "generated slot identifier enum"),
            new($"ExecuteArity{model.Arity}", "Method", "sync execution entry point"),
            new($"ExecuteAsyncArity{model.Arity}", "Method", "async execution entry point")
        };

        if (generateLegacyNames)
        {
            members.Add(new GeneratedMemberExplanation("Execute", "Method", "legacy sync wrapper"));
            members.Add(new GeneratedMemberExplanation("ExecuteAsync", "Method", "legacy async wrapper"));
        }

        if (SourceSymbolHelpers.GetModuleRoot(model.Symbol) is not null)
            members.Add(new GeneratedMemberExplanation($"{model.Name}ModuleQuery", "Type", "module query wrapper"));

        var decisions = new List<CodegenDecisionExplanation>();

        bool usesIndexedLoop = PlanAnalysis.AllBlocks(plan)
            .SelectMany(block => block.Instructions)
            .OfType<IndexInitInstr>()
            .Any(instr => instr.IndexedLookup is not null);

        if (usesIndexedLoop)
            decisions.Add(new CodegenDecisionExplanation("IndexedLoop", "query uses indexed loop codegen", null));

        string[] terminalSlots = model.Parameters
            .Select(parameter => parameter.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        decisions.Add(new CodegenDecisionExplanation(
            "TerminalMaterialization",
            terminalSlots.Length == 0
                ? "query has no terminal-slot materialization"
                : $"query materializes terminal slots {string.Join(", ", terminalSlots)}",
            null));

        decisions.Add(new CodegenDecisionExplanation("AsyncWrapper", "query emits async wrapper", null));

        return new CodeEmissionSummary(
            resultTypeName,
            stateTypeName,
            slotEnumName,
            members
                .OrderBy(member => member.Name, StringComparer.Ordinal)
                .ThenBy(member => member.Kind, StringComparer.Ordinal)
                .ToImmutableArray(),
            decisions
                .OrderBy(decision => decision.Decision, StringComparer.Ordinal)
                .ToImmutableArray());
    }

    private static ImmutableArray<DiagnosticExplanation> BuildDiagnostics(IReadOnlyList<Diagnostic> diagnostics)
    {
        return diagnostics
            .OrderBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceTree?.FilePath, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .Select(diagnostic =>
            {
                string reason = GetDiagnosticReason(diagnostic.Id);
                ImmutableArray<string> relatedSymbols = ExtractRelatedSymbols(diagnostic.GetMessage());
                ImmutableArray<string> fixes = GetSuggestedFixes(diagnostic.Id);
                SourceLocation source = GetSourceLocation(diagnostic.Location) ?? new SourceLocation(string.Empty, 0, 0, 0, 0, default);

                return new DiagnosticExplanation(
                    diagnostic.Id,
                    diagnostic.Severity.ToString(),
                    diagnostic.GetMessage(),
                    source,
                    GetPhase(diagnostic.Id),
                    reason,
                    relatedSymbols,
                    fixes);
            })
            .ToImmutableArray();
    }

    private static DiagnosticPhase GetPhase(string diagnosticId)
    {
        if (diagnosticId.StartsWith("FLTCH00", StringComparison.Ordinal))
            return DiagnosticPhase.SemanticBinding;

        return diagnosticId switch
        {
            "FLG0001" => DiagnosticPhase.DslValidation,
            "FLG0002" => DiagnosticPhase.DslValidation,
            "FLG0003" => DiagnosticPhase.RecursivePlanning,
            "FLG0004" => DiagnosticPhase.DslValidation,
            "FLT2002" => DiagnosticPhase.RecursivePlanning,
            "FLT2004" => DiagnosticPhase.CodeEmission,
            "FLT2005" => DiagnosticPhase.RecursivePlanning,
            "FLM3001" => DiagnosticPhase.RecursivePlanning,
            "FLM3002" => DiagnosticPhase.RecursivePlanning,
            "FLM3003" => DiagnosticPhase.RecursivePlanning,
            "FLM3004" => DiagnosticPhase.RecursivePlanning,
            "FLM3005" => DiagnosticPhase.RecursivePlanning,
            _ => DiagnosticPhase.Planning
        };
    }

    private static string GetDiagnosticReason(string diagnosticId)
    {
        return diagnosticId switch
        {
            "FLTCH001" => "predicate body violated required signature invariant",
            "FLTCH002" => "expression kind is not supported in lowering",
            "FLTCH003" => "operands failed type-compatibility invariant",
            "FLTCH004" => "terminal projection variable is not grounded in all paths",
            "FLTCH009" => "predicate call could not be resolved",
            "FLG0001" => "negation requires variables to be ground",
            "FLG0002" => "negation-local variable escaped its scope",
            "FLG0003" => "negative recursion cycle detected",
            "FLT2002" => "tabled predicate participates in unsupported negation cycle",
            "FLT2005" => "mutually recursive tabled predicates are unsupported",
            "FLM3002" => "all-free adornment has no bound seed",
            "FLM3003" => "recursive negation blocks magic-set rewriting",
            "FLM3004" => "recursive bound call has no indexed access path",
            "FLI4001" => "index declaration references an unknown member",
            "FLI4002" => "index declaration references an invalid member kind",
            "FLI4003" => "range index requires a comparable member type",
            "FLI4004" => "composite range indexes are unsupported",
            "FLI4005" => "duplicate index declaration detected",
            "FLI4006" => "multiple index declarations resolved to the same name",
            _ => "compiler invariant failed"
        };
    }

    private static ImmutableArray<string> GetSuggestedFixes(string diagnosticId)
    {
        return diagnosticId switch
        {
            "FLTCH001" => ImmutableArray.Create("change PredicateBody return type to LogicExpr<bool>"),
            "FLTCH003" => ImmutableArray.Create("align both sides of unification to the same type"),
            "FLTCH004" => ImmutableArray.Create("bind terminal variable before projection"),
            "FLTCH009" => ImmutableArray.Create("verify predicate name, arity, and argument types"),
            "FLG0001" => ImmutableArray.Create("ground all variables used inside Logic.Not before negation"),
            "FLG0003" => ImmutableArray.Create("remove negative recursive dependency or rewrite recursion as positive tabled recursion"),
            "FLT2002" => ImmutableArray.Create("remove negated recursive cycle from tabled predicates"),
            "FLM3004" => ImmutableArray.Create("add or generate an index for the bound recursive argument"),
            "FLI4001" => ImmutableArray.Create("fix the member name or remove the invalid declaration"),
            "FLI4002" => ImmutableArray.Create("use a readable instance field or property"),
            "FLI4003" => ImmutableArray.Create("use a comparable member type for the range index"),
            "FLI4004" => ImmutableArray.Create("split the declaration into single-member range indexes"),
            "FLI4005" => ImmutableArray.Create("remove the duplicate index declaration"),
            "FLI4006" => ImmutableArray.Create("rename one of the colliding index declarations"),
            _ => ImmutableArray<string>.Empty
        };
    }

    private static ImmutableArray<string> ExtractRelatedSymbols(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return ImmutableArray<string>.Empty;

        var related = new SortedSet<string>(StringComparer.Ordinal);
        int start = message.IndexOf("'", StringComparison.Ordinal);
        while (start >= 0)
        {
            int end = message.IndexOf("'", start + 1, StringComparison.Ordinal);
            if (end < 0)
                break;

            if (end > start + 1)
                related.Add(message.Substring(start + 1, end - start - 1));

            start = message.IndexOf("'", end + 1, StringComparison.Ordinal);
        }

        return related.ToImmutableArray();
    }

    private static SourceLocation? GetSourceLocation(Location? location)
    {
        if (location is null || !location.IsInSource)
            return null;

        FileLinePositionSpan span = location.GetLineSpan();
        return new SourceLocation(
            Path.GetFileName(location.SourceTree?.FilePath ?? string.Empty),
            span.StartLinePosition.Line + 1,
            span.StartLinePosition.Character + 1,
            span.EndLinePosition.Line + 1,
            span.EndLinePosition.Character + 1,
            location.SourceSpan);
    }

    private static void TraverseSemanticExpr(
        SemanticExpr expr,
        Action<SemanticExpr>? onExpr = null,
        Action<WithExpr>? onWith = null,
        Action<CallExpr>? onCall = null,
        Action<ConstraintExpr>? onConstraint = null,
        Action<VariableSymbol>? onVar = null)
    {
        onExpr?.Invoke(expr);

        switch (expr)
        {
            case VarExpr varExpr:
                onVar?.Invoke(varExpr.Variable);
                break;

            case FieldExpr field:
                TraverseSemanticExpr(field.Target, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case UnifyExpr unify:
                TraverseSemanticExpr(unify.Left, onExpr, onWith, onCall, onConstraint, onVar);
                TraverseSemanticExpr(unify.Right, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case ConjExpr conjunction:
                foreach (SemanticExpr part in conjunction.Parts)
                    TraverseSemanticExpr(part, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case DisjExpr disjunction:
                TraverseSemanticExpr(disjunction.Left, onExpr, onWith, onCall, onConstraint, onVar);
                TraverseSemanticExpr(disjunction.Right, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case ConstraintExpr constraint:
                onConstraint?.Invoke(constraint);
                foreach (SemanticExpr argument in constraint.Arguments)
                    TraverseSemanticExpr(argument, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case WithExpr with:
                onWith?.Invoke(with);
                TraverseSemanticExpr(with.Body, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case CallExpr call:
                onCall?.Invoke(call);
                foreach (SemanticExpr argument in call.Arguments)
                    TraverseSemanticExpr(argument, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case CompExpr comparison:
                TraverseSemanticExpr(comparison.Left, onExpr, onWith, onCall, onConstraint, onVar);
                TraverseSemanticExpr(comparison.Right, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case ArithExpr arithmetic:
                TraverseSemanticExpr(arithmetic.Left, onExpr, onWith, onCall, onConstraint, onVar);
                TraverseSemanticExpr(arithmetic.Right, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case NotExpr not:
                TraverseSemanticExpr(not.Goal, onExpr, onWith, onCall, onConstraint, onVar);
                break;

            case ListConsExpr cons:
                TraverseSemanticExpr(cons.Head, onExpr, onWith, onCall, onConstraint, onVar);
                TraverseSemanticExpr(cons.Tail, onExpr, onWith, onCall, onConstraint, onVar);
                break;
        }
    }

    private static string GetSemanticKind(SemanticExpr expr)
    {
        string name = expr.GetType().Name;
        return name.EndsWith("Expr", StringComparison.Ordinal)
            ? name.Substring(0, name.Length - "Expr".Length)
            : name;
    }

    private static IReadOnlyCollection<string> GetSemanticReads(SemanticExpr expr)
    {
        var reads = new SortedSet<string>(StringComparer.Ordinal);
        TraverseSemanticExpr(expr, onVar: variable => reads.Add(variable.Name));
        return reads;
    }

    private static IReadOnlyCollection<string> GetSemanticWrites(SemanticExpr expr)
    {
        var writes = new SortedSet<string>(StringComparer.Ordinal);
        if (expr is WithExpr with)
        {
            foreach (VariableSymbol variable in with.Variables)
                writes.Add(variable.Name);
        }

        return writes;
    }

    private static string RenderSemanticValue(SemanticExpr expr)
    {
        return expr switch
        {
            VarExpr variable => variable.Variable.Name,
            ConstExpr constant => constant.Value is string text ? $"\"{text}\"" : constant.Value?.ToString() ?? "null",
            FieldExpr field => $"{RenderSemanticValue(field.Target)}.{field.Member.Name}",
            UnifyExpr unify => $"{RenderSemanticValue(unify.Left)} == {RenderSemanticValue(unify.Right)}",
            ConjExpr conjunction => string.Join(" && ", conjunction.Parts.Select(RenderSemanticValue)),
            DisjExpr disjunction => $"({RenderSemanticValue(disjunction.Left)} || {RenderSemanticValue(disjunction.Right)})",
            ConstraintExpr constraint => $"{constraint.Method.Name}({string.Join(", ", constraint.Arguments.Select(RenderSemanticValue))})",
            WithExpr with => $"with [{string.Join(", ", with.Variables.Select(variable => variable.Name))}] => {RenderSemanticValue(with.Body)}",
            CallExpr call => $"{call.PredicateType.Name}/{call.Arity}({string.Join(", ", call.Arguments.Select(RenderSemanticValue))})",
            CompExpr comparison => $"{RenderSemanticValue(comparison.Left)} {RenderComp(comparison.Op)} {RenderSemanticValue(comparison.Right)}",
            ArithExpr arithmetic => $"{RenderSemanticValue(arithmetic.Left)} {RenderArith(arithmetic.Op)} {RenderSemanticValue(arithmetic.Right)}",
            NotExpr not => $"not({RenderSemanticValue(not.Goal)})",
            ListEmptyExpr => "[]",
            ListConsExpr cons => $"[{RenderSemanticValue(cons.Head)} | {RenderSemanticValue(cons.Tail)}]",
            _ => expr.GetType().Name
        };
    }

    private static string RenderComp(CompOp op)
    {
        return op switch
        {
            CompOp.NotEqual => "!=",
            CompOp.LessThan => "<",
            CompOp.GreaterThan => ">",
            CompOp.LessThanOrEqual => "<=",
            CompOp.GreaterThanOrEqual => ">=",
            _ => "?"
        };
    }

    private static string RenderArith(ArithOp op)
    {
        return op switch
        {
            ArithOp.Add => "+",
            ArithOp.Subtract => "-",
            _ => "?"
        };
    }

    private static string RenderPlanText(PlanProgram plan)
    {
        var sb = new StringBuilder();
        foreach (PlanBlock block in PlanAnalysis.AllBlocks(plan))
        {
            sb.AppendLine($"{block.Label}:");
            foreach (PlanInstruction instruction in block.Instructions)
                sb.AppendLine($"  {RenderInstruction(instruction)}");
            sb.AppendLine($"  -> {RenderTerminator(block.Terminator)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string RenderInstruction(PlanInstruction instruction)
    {
        return instruction switch
        {
            UnifyInstr unify => $"unify {RenderValue(unify.Left)} == {RenderValue(unify.Right)}",
            ConstraintInstr constraint => $"constraint {constraint.Method.Name}({string.Join(", ", constraint.Arguments.Select(RenderValue))})",
            AssignInstr assign => $"assign s{assign.Slot} = {RenderValue(assign.Value)}",
            CompInstr comparison => $"compare {RenderValue(comparison.Left)} {RenderComp(comparison.Op)} {RenderValue(comparison.Right)}",
            IndexInitInstr index when index.IndexedLookup is null => $"index-init {index.IndexVar} scan {index.FactType.Name}",
            IndexInitInstr index when index.IndexedLookup!.AccessPathKind == FactAccessPathKind.RangeIndex
                => $"index-init {index.IndexVar} range {index.IndexedLookup.IndexName}",
            IndexInitInstr index when index.IndexedLookup!.AccessPathKind == FactAccessPathKind.CompositeEqualityIndex
                => $"index-init {index.IndexVar} composite {index.IndexedLookup.IndexName}({string.Join(", ", index.IndexedLookup.EqualityParts.Select(part => RenderValue(part.Key)))})",
            IndexInitInstr index => $"index-init {index.IndexVar} lookup {index.IndexedLookup!.IndexName} == {RenderValue(index.IndexedLookup.Key)}",
            LoopBindInstr bind => $"loop-bind s{bind.Slot} from {bind.IndexVar}",
            IndexIncrInstr increment => $"index-incr {increment.IndexVar}",
            CallInstr call => $"call {call.PredicateType.Name}/{call.Arity}({string.Join(", ", call.ArgumentSlots.Select(slot => $"s{slot}"))}) tabled={call.IsTabledCall}",
            NotInstr not => $"not [{string.Join("; ", not.SubGoalInstructions.Select(RenderInstruction))}]",
            _ => instruction.GetType().Name
        };
    }

    private static string RenderValue(PlanValue value)
    {
        return value switch
        {
            SlotValue slot => $"s{slot.Slot}",
            ConstValue constant when constant.Value is string text => $"\"{text}\"",
            ConstValue constant => constant.Value?.ToString() ?? "null",
            FieldValue field => $"{RenderValue(field.Target)}.{field.MemberName}",
            ArithValue arithmetic => $"{RenderValue(arithmetic.Left)} {RenderArith(arithmetic.Op)} {RenderValue(arithmetic.Right)}",
            ListEmptyValue => "[]",
            ListConsValue cons => $"[{RenderValue(cons.Head)} | {RenderValue(cons.Tail)}]",
            _ => value.GetType().Name
        };
    }

    private static ImmutableArray<string> ExtractBoundInputs(PlanValue key)
    {
        return key switch
        {
            SlotValue slot => ImmutableArray.Create($"s{slot.Slot}"),
            _ => ImmutableArray<string>.Empty
        };
    }

    private static string RenderTerminator(PlanTerminator terminator)
    {
        return terminator switch
        {
            GotoTerm goTo => $"goto {goTo.TargetLabel}",
            ChoiceTerm choice => $"choice next={choice.NextLabel} alt={choice.AlternativeLabel} trail=s{choice.TrailSlot}",
            SucceedTerm => "succeed",
            FailTerm => "fail",
            LoopCheckTerm loop => $"loop-check {loop.IndexVar} body={loop.BodyLabel} fail={loop.FailLabel}",
            _ => terminator.GetType().Name
        };
    }

    private static bool MayFail(PlanInstruction instruction)
    {
        return instruction is UnifyInstr or ConstraintInstr or CompInstr or NotInstr;
    }

    private static bool MayProduceMultipleResults(PlanInstruction instruction)
    {
        return instruction is CallInstr or LoopBindInstr;
    }
}
