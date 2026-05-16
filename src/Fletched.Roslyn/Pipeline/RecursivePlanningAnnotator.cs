using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

public static class RecursivePlanningAnnotator
{
    public static PlanProgram Annotate(
        PredicateModel model,
        PlanProgram plan,
        PredicateCallGraph? callGraph,
        DiagnosticReporter reporter)
    {
        PredicateCallGraph effectiveGraph = callGraph ?? PredicateCallGraph.Create([model]);
        PredicateCallGraphNode? callerNode = effectiveGraph.TryGetNode(model.Symbol, model.Arity);
        AdornmentAnalysisResult adornmentAnalysis = AdornmentAnalyzer.Analyze(model, reporter: reporter);

        List<(string Label, CallInstr Call)> plannedCalls = PlanAnalysis.AllBlocks(plan)
            .SelectMany(block => block.Instructions.OfType<CallInstr>().Select(call => (block.Label, call)))
            .ToList();

        var recursiveCalls = new List<RecursiveCallPlan>();
        var magicPredicates = new List<MagicPredicatePlan>();
        var magicSeeds = new List<MagicSeedPlan>();
        var modifiedRules = new List<MagicModifiedRulePlan>();
        var propagationRules = new List<MagicPropagationRulePlan>();
        var accessPaths = CollectAccessPaths(plan);

        for (int callIndex = 0; callIndex < adornmentAnalysis.Calls.Count; callIndex++)
        {
            AdornedCallPlan callPlan = adornmentAnalysis.Calls[callIndex];
            PredicateCallGraphNode? targetNode = effectiveGraph.TryGetNode(callPlan.Call.PredicateType, callPlan.Call.Arity);
            if (callerNode is null || targetNode is null || !effectiveGraph.IsInSameRecursiveComponent(callerNode, targetNode))
                continue;

            string? blockLabel = callIndex < plannedCalls.Count
                ? plannedCalls[callIndex].Label
                : null;

            var recursiveCall = new RecursiveCallPlan(
                model.Name,
                callPlan.Call.PredicateType.Name,
                callPlan.Adornment,
                PredicateAttributeHelpers.IsTabledPredicate(callPlan.Call.PredicateType),
                callPlan.IsInsideNegation,
                blockLabel);
            recursiveCalls.Add(recursiveCall);

            bool rejectedForNegation = callPlan.IsInsideNegation || effectiveGraph.HasNegativeCycle(targetNode);
            if (rejectedForNegation)
            {
                reporter.Report(
                    DiagnosticsCatalog.MagicRewriteRejectedNegation,
                    model.BodyMethod.Locations.FirstOrDefault(),
                    callPlan.Call.PredicateType.Name);
                continue;
            }

            if (callPlan.Adornment.IsAllFree)
            {
                reporter.Report(
                    DiagnosticsCatalog.MagicRewriteSkippedAllFree,
                    model.BodyMethod.Locations.FirstOrDefault(),
                    callPlan.Call.PredicateType.Name);
                continue;
            }

            IReadOnlyList<int> boundArguments = callPlan.Adornment.Pattern
                .Select((marker, index) => (marker, index))
                .Where(entry => entry.marker == 'b')
                .Select(entry => entry.index)
                .ToArray();

            MagicPredicatePlan magicPredicate = magicPredicates.FirstOrDefault(predicate =>
                    predicate.PredicateName == callPlan.Call.PredicateType.Name
                    && predicate.Adornment == callPlan.Adornment)
                ?? new MagicPredicatePlan(callPlan.Call.PredicateType.Name, callPlan.Adornment, boundArguments);

            if (!magicPredicates.Contains(magicPredicate))
                magicPredicates.Add(magicPredicate);

            magicSeeds.Add(new MagicSeedPlan(
                model.Name,
                callPlan.Call.PredicateType.Name,
                callPlan.Adornment,
                boundArguments,
                blockLabel));

            propagationRules.Add(new MagicPropagationRulePlan(
                model.Name,
                callPlan.Call.PredicateType.Name,
                callPlan.Adornment,
                boundArguments,
                blockLabel));

            if (SymbolEqualityComparer.Default.Equals(callPlan.Call.PredicateType, model.Symbol))
            {
                modifiedRules.Add(new MagicModifiedRulePlan(
                    model.Name,
                    callPlan.Adornment,
                    magicPredicate.MagicPredicateName));
            }

            accessPaths.Add(new RecursiveAccessPathPlan(
                blockLabel ?? $"recursive_call_{callIndex}",
                RecursiveAccessPathKind.MagicSourceLookup,
                magicPredicate.MagicPredicateName));
        }

        if (recursiveCalls.Count > 0
            && recursiveCalls.Any(call => call.Adornment.HasBoundArguments)
            && accessPaths.All(path => path.Kind != RecursiveAccessPathKind.IndexedFactLookup))
        {
            reporter.Report(
                DiagnosticsCatalog.MissingRecursiveIndex,
                model.BodyMethod.Locations.FirstOrDefault(),
                model.Name);
        }

        return plan with
        {
            Metadata = new RecursivePlanMetadata(
                adornmentAnalysis.EntryAdornment,
                recursiveCalls,
                magicPredicates,
                magicSeeds,
                modifiedRules,
                propagationRules,
                accessPaths)
        };
    }

    private static List<RecursiveAccessPathPlan> CollectAccessPaths(PlanProgram plan)
    {
        var accessPaths = new List<RecursiveAccessPathPlan>();
        foreach (PlanBlock block in PlanAnalysis.AllBlocks(plan))
        {
            foreach (PlanInstruction instruction in block.Instructions)
            {
                switch (instruction)
                {
                    case IndexInitInstr init when init.IndexedLookup is null:
                        accessPaths.Add(new RecursiveAccessPathPlan(block.Label, RecursiveAccessPathKind.FullFactScan, init.FactType.Name));
                        break;

                    case IndexInitInstr init:
                        accessPaths.Add(new RecursiveAccessPathPlan(block.Label, RecursiveAccessPathKind.IndexedFactLookup, $"{init.FactType.Name}.{init.IndexedLookup!.MemberName}"));
                        break;

                    case CallInstr call when call.IsTabledCall:
                        accessPaths.Add(new RecursiveAccessPathPlan(block.Label, RecursiveAccessPathKind.TableLookup, call.PredicateType.Name));
                        break;
                }
            }
        }

        return accessPaths;
    }
}
