using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fletched.Roslyn.Pipeline;

public sealed record PredicateCallGraphNode(
    string Id,
    INamedTypeSymbol PredicateType,
    int Arity)
{
    public string DisplayName => $"{PredicateType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}/{Arity}";
}

public sealed record PredicateCallGraphEdge(
    PredicateCallGraphNode From,
    PredicateCallGraphNode To,
    bool IsNegative,
    Location? Location);

public sealed record PredicateCallGraphCycle(IReadOnlyList<PredicateCallGraphEdge> Edges)
{
    public IReadOnlyList<PredicateCallGraphNode> Nodes
    {
        get
        {
            if (Edges.Count == 0)
                return [];

            var nodes = new List<PredicateCallGraphNode> { Edges[0].From };
            nodes.AddRange(Edges.Select(edge => edge.To));
            return nodes;
        }
    }
}

public sealed class PredicateCallGraph
{
    private readonly IReadOnlyDictionary<string, PredicateCallGraphNode> _nodesById;
    private readonly IReadOnlyList<PredicateCallGraphEdge> _edges;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<PredicateCallGraphEdge>> _outgoing;
    private readonly IReadOnlyList<IReadOnlyList<PredicateCallGraphNode>> _components;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<PredicateCallGraphNode>> _componentsByNodeId;

    private PredicateCallGraph(
        IReadOnlyDictionary<string, PredicateCallGraphNode> nodesById,
        IReadOnlyList<PredicateCallGraphEdge> edges)
    {
        _nodesById = nodesById;
        _edges = edges;
        _outgoing = edges
            .GroupBy(edge => edge.From.Id, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PredicateCallGraphEdge>)group.ToList(),
                StringComparer.Ordinal);

        _components = ComputeStronglyConnectedComponents(nodesById.Values.ToList(), _outgoing);
        _componentsByNodeId = _components
            .SelectMany(component => component.Select(node => new KeyValuePair<string, IReadOnlyList<PredicateCallGraphNode>>(node.Id, component)))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    public IReadOnlyList<PredicateCallGraphNode> Nodes => _nodesById.Values.ToList();

    public IReadOnlyList<PredicateCallGraphEdge> Edges => _edges;

    public static PredicateCallGraph Create(IEnumerable<PredicateModel> models)
    {
        var nodesById = new Dictionary<string, PredicateCallGraphNode>(StringComparer.Ordinal);
        var edges = new List<PredicateCallGraphEdge>();

        foreach (PredicateModel model in models)
        {
            PredicateCallGraphNode fromNode = GetOrAddNode(nodesById, model.Symbol, model.Arity);
            CollectEdges(model.Body, fromNode, isNegativeContext: false, model.BodyMethod.Locations.FirstOrDefault(), nodesById, edges);
        }

        return new PredicateCallGraph(nodesById, edges);
    }

    public PredicateCallGraphNode? TryGetNode(INamedTypeSymbol predicateType, int arity)
    {
        string nodeId = GetNodeId(predicateType, arity);
        return _nodesById.TryGetValue(nodeId, out PredicateCallGraphNode? node) ? node : null;
    }

    public bool IsRecursive(PredicateCallGraphNode node)
    {
        IReadOnlyList<PredicateCallGraphNode> component = _componentsByNodeId[node.Id];
        return component.Count > 1 || IsDirectRecursive(node);
    }

    public bool IsDirectRecursive(PredicateCallGraphNode node)
    {
        return _outgoing.TryGetValue(node.Id, out IReadOnlyList<PredicateCallGraphEdge>? edges)
            && edges.Any(edge => edge.To.Id == node.Id);
    }

    public bool IsMutuallyRecursive(PredicateCallGraphNode node)
    {
        return _componentsByNodeId[node.Id].Count > 1;
    }

    public bool HasMutuallyRecursiveTabledCycle(PredicateCallGraphNode node)
    {
        IReadOnlyList<PredicateCallGraphNode> component = _componentsByNodeId[node.Id];
        if (component.Count <= 1)
            return false;

        int tabledCount = component.Count(componentNode =>
            PredicateAttributeHelpers.IsTabledPredicate(componentNode.PredicateType));
        return tabledCount > 1;
    }

    public IReadOnlyList<PredicateCallGraphCycle> GetNegativeCycles()
    {
        var cycles = new List<PredicateCallGraphCycle>();

        foreach (IReadOnlyList<PredicateCallGraphNode> component in _components)
        {
            if (!IsRecursiveComponent(component))
                continue;

            var componentIds = new HashSet<string>(
                component.Select(node => node.Id),
                StringComparer.Ordinal);

            PredicateCallGraphEdge? negativeEdge = _edges.FirstOrDefault(edge =>
                edge.IsNegative &&
                componentIds.Contains(edge.From.Id) &&
                componentIds.Contains(edge.To.Id));

            if (negativeEdge is null)
                continue;

            List<PredicateCallGraphEdge> cycleEdges = BuildCycleEdges(negativeEdge, componentIds);
            if (cycleEdges.Count > 0)
                cycles.Add(new PredicateCallGraphCycle(cycleEdges));
        }

        return cycles;
    }

    public string FormatCycle(PredicateCallGraphCycle cycle)
    {
        if (cycle.Edges.Count == 0)
            return string.Empty;

        var parts = new List<string> { cycle.Edges[0].From.DisplayName };
        foreach (PredicateCallGraphEdge edge in cycle.Edges)
        {
            parts.Add(edge.IsNegative ? "-not->" : "->");
            parts.Add(edge.To.DisplayName);
        }

        return string.Join(" ", parts);
    }

    private static PredicateCallGraphNode GetOrAddNode(
        IDictionary<string, PredicateCallGraphNode> nodesById,
        INamedTypeSymbol predicateType,
        int arity)
    {
        string nodeId = GetNodeId(predicateType, arity);
        if (!nodesById.TryGetValue(nodeId, out PredicateCallGraphNode? node))
        {
            node = new PredicateCallGraphNode(nodeId, predicateType, arity);
            nodesById[nodeId] = node;
        }

        return node;
    }

    private static string GetNodeId(INamedTypeSymbol predicateType, int arity)
    {
        return $"{predicateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}/{arity}";
    }

    private static void CollectEdges(
        SemanticExpr expr,
        PredicateCallGraphNode fromNode,
        bool isNegativeContext,
        Location? location,
        IDictionary<string, PredicateCallGraphNode> nodesById,
        ICollection<PredicateCallGraphEdge> edges)
    {
        switch (expr)
        {
            case CallExpr call:
            {
                PredicateCallGraphNode toNode = GetOrAddNode(nodesById, call.PredicateType, call.Arity);
                edges.Add(new PredicateCallGraphEdge(fromNode, toNode, isNegativeContext, location));

                foreach (SemanticExpr argument in call.Arguments)
                    CollectEdges(argument, fromNode, isNegativeContext, location, nodesById, edges);
                break;
            }

            case FieldExpr field:
                CollectEdges(field.Target, fromNode, isNegativeContext, location, nodesById, edges);
                break;

            case UnifyExpr unify:
                CollectEdges(unify.Left, fromNode, isNegativeContext, location, nodesById, edges);
                CollectEdges(unify.Right, fromNode, isNegativeContext, location, nodesById, edges);
                break;

            case ConjExpr conjunction:
                foreach (SemanticExpr part in conjunction.Parts)
                    CollectEdges(part, fromNode, isNegativeContext, location, nodesById, edges);
                break;

            case DisjExpr disjunction:
                CollectEdges(disjunction.Left, fromNode, isNegativeContext, location, nodesById, edges);
                CollectEdges(disjunction.Right, fromNode, isNegativeContext, location, nodesById, edges);
                break;

            case ConstraintExpr constraint:
                foreach (SemanticExpr argument in constraint.Arguments)
                    CollectEdges(argument, fromNode, isNegativeContext, location, nodesById, edges);
                break;

            case WithExpr withExpr:
                CollectEdges(withExpr.Body, fromNode, isNegativeContext, location, nodesById, edges);
                break;

            case NotExpr notExpr:
                CollectEdges(notExpr.Goal, fromNode, isNegativeContext: true, location, nodesById, edges);
                break;

            case CompExpr comp:
                CollectEdges(comp.Left, fromNode, isNegativeContext, location, nodesById, edges);
                CollectEdges(comp.Right, fromNode, isNegativeContext, location, nodesById, edges);
                break;

            case ArithExpr arith:
                CollectEdges(arith.Left, fromNode, isNegativeContext, location, nodesById, edges);
                CollectEdges(arith.Right, fromNode, isNegativeContext, location, nodesById, edges);
                break;

            case ListConsExpr listCons:
                CollectEdges(listCons.Head, fromNode, isNegativeContext, location, nodesById, edges);
                CollectEdges(listCons.Tail, fromNode, isNegativeContext, location, nodesById, edges);
                break;
        }
    }

    private static IReadOnlyList<IReadOnlyList<PredicateCallGraphNode>> ComputeStronglyConnectedComponents(
        IReadOnlyList<PredicateCallGraphNode> nodes,
        IReadOnlyDictionary<string, IReadOnlyList<PredicateCallGraphEdge>> outgoing)
    {
        var indexByNodeId = new Dictionary<string, int>(StringComparer.Ordinal);
        var lowLinkByNodeId = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new Stack<PredicateCallGraphNode>();
        var onStack = new HashSet<string>(StringComparer.Ordinal);
        var components = new List<IReadOnlyList<PredicateCallGraphNode>>();
        int index = 0;

        foreach (PredicateCallGraphNode node in nodes)
        {
            if (!indexByNodeId.ContainsKey(node.Id))
                StrongConnect(node);
        }

        return components;

        void StrongConnect(PredicateCallGraphNode node)
        {
            indexByNodeId[node.Id] = index;
            lowLinkByNodeId[node.Id] = index;
            index++;

            stack.Push(node);
            onStack.Add(node.Id);

            if (outgoing.TryGetValue(node.Id, out IReadOnlyList<PredicateCallGraphEdge>? edges))
            {
                foreach (PredicateCallGraphEdge edge in edges)
                {
                    PredicateCallGraphNode target = edge.To;
                    if (!indexByNodeId.ContainsKey(target.Id))
                    {
                        StrongConnect(target);
                        lowLinkByNodeId[node.Id] = System.Math.Min(lowLinkByNodeId[node.Id], lowLinkByNodeId[target.Id]);
                    }
                    else if (onStack.Contains(target.Id))
                    {
                        lowLinkByNodeId[node.Id] = System.Math.Min(lowLinkByNodeId[node.Id], indexByNodeId[target.Id]);
                    }
                }
            }

            if (lowLinkByNodeId[node.Id] != indexByNodeId[node.Id])
                return;

            var component = new List<PredicateCallGraphNode>();
            PredicateCallGraphNode current;
            do
            {
                current = stack.Pop();
                onStack.Remove(current.Id);
                component.Add(current);
            }
            while (current.Id != node.Id);

            components.Add(component);
        }
    }

    private bool IsRecursiveComponent(IReadOnlyList<PredicateCallGraphNode> component)
    {
        return component.Count > 1 || component.Any(IsDirectRecursive);
    }

    private List<PredicateCallGraphEdge> BuildCycleEdges(
        PredicateCallGraphEdge negativeEdge,
        ISet<string> componentIds)
    {
        if (negativeEdge.From.Id == negativeEdge.To.Id)
            return [negativeEdge];

        var queue = new Queue<PredicateCallGraphNode>();
        var previousEdgeByNodeId = new Dictionary<string, PredicateCallGraphEdge>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal) { negativeEdge.To.Id };

        queue.Enqueue(negativeEdge.To);

        while (queue.Count > 0)
        {
            PredicateCallGraphNode current = queue.Dequeue();
            if (!_outgoing.TryGetValue(current.Id, out IReadOnlyList<PredicateCallGraphEdge>? edges))
                continue;

            foreach (PredicateCallGraphEdge edge in edges)
            {
                if (!componentIds.Contains(edge.To.Id) || !visited.Add(edge.To.Id))
                    continue;

                previousEdgeByNodeId[edge.To.Id] = edge;

                if (edge.To.Id == negativeEdge.From.Id)
                {
                    var cycle = new List<PredicateCallGraphEdge> { negativeEdge };
                    var tail = new List<PredicateCallGraphEdge>();
                    string cursor = edge.To.Id;
                    while (cursor != negativeEdge.To.Id)
                    {
                        PredicateCallGraphEdge previousEdge = previousEdgeByNodeId[cursor];
                        tail.Add(previousEdge);
                        cursor = previousEdge.From.Id;
                    }

                    tail.Reverse();
                    cycle.AddRange(tail);
                    return cycle;
                }

                queue.Enqueue(edge.To);
            }
        }

        return [];
    }
}

public static class PredicateRecursionValidator
{
    public static void ReportMutualNegativeCycles(
        PredicateCallGraph graph,
        IEnumerable<PredicateModel> currentModels,
        DiagnosticReporter reporter)
    {
        var currentNodeIds = new HashSet<string>(
            currentModels.Select(PredicateCallGraphNodeId),
            StringComparer.Ordinal);

        foreach (PredicateCallGraphCycle cycle in graph.GetNegativeCycles())
        {
            if (cycle.Edges.Count <= 1)
                continue;

            if (!cycle.Nodes.Any(node => currentNodeIds.Contains(node.Id)))
                continue;

            Location? location = cycle.Edges
                .Select(edge => edge.Location)
                .FirstOrDefault(edgeLocation => edgeLocation is not null);

            reporter.Error(
                GetNegationCycleDiagnostic(cycle),
                location,
                $": {graph.FormatCycle(cycle)}");
        }
    }

    public static void ReportUnsupportedTabledMutualRecursion(
        PredicateCallGraph graph,
        IEnumerable<PredicateModel> currentModels,
        DiagnosticReporter reporter)
    {
        foreach (PredicateModel model in currentModels)
        {
            if (!PredicateAttributeHelpers.IsTabledPredicate(model.Symbol))
                continue;

            PredicateCallGraphNode? node = graph.TryGetNode(model.Symbol, model.Arity);
            if (node is null || !graph.HasMutuallyRecursiveTabledCycle(node))
                continue;

            reporter.Error(
                DiagnosticsCatalog.UnsupportedTabledMutualRecursion,
                model.BodyMethod.Locations.FirstOrDefault());
        }
    }

    private static string PredicateCallGraphNodeId(PredicateModel model)
    {
        return $"{model.Symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}/{model.Arity}";
    }

    /// <summary>
    /// Returns the recursive-negation diagnostic for a cycle.
    /// Uses FLT2002 when any predicate in the cycle is tabled; otherwise uses FLG0003.
    /// </summary>
    private static DiagnosticDescriptor GetNegationCycleDiagnostic(PredicateCallGraphCycle cycle)
    {
        return cycle.Nodes.Any(node => PredicateAttributeHelpers.IsTabledPredicate(node.PredicateType))
            ? DiagnosticsCatalog.InvalidTabledNegationCycle
            : DiagnosticsCatalog.UnsupportedRecursiveNegation;
    }
}
