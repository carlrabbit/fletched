using System.Collections.Generic;
using System.Linq;

namespace Fletched.Roslyn.Pipeline;

public sealed record AdornedCallPlan(
    CallExpr Call,
    Adornment Adornment,
    bool IsInsideNegation);

public sealed record AdornmentAnalysisResult(
    Adornment EntryAdornment,
    IReadOnlyList<AdornedCallPlan> Calls);

public static class AdornmentAnalyzer
{
    public static AdornmentAnalysisResult Analyze(
        PredicateModel model,
        Adornment? entryAdornment = null,
        DiagnosticReporter? reporter = null)
    {
        Adornment initialAdornment = entryAdornment
            ?? Adornment.FromBoundArguments(Enumerable.Repeat(false, model.Arity));

        if (initialAdornment.Pattern.Length != model.Arity)
        {
            reporter?.Report(
                DiagnosticsCatalog.AmbiguousAdornment,
                model.BodyMethod.Locations.FirstOrDefault(),
                model.Name);
            initialAdornment = Adornment.FromBoundArguments(Enumerable.Repeat(false, model.Arity));
        }

        var state = new Dictionary<VariableSymbol, bool>();
        for (int index = 0; index < model.Parameters.Count; index++)
            state[model.Parameters[index]] = initialAdornment.Pattern[index] == 'b';

        var calls = new List<AdornedCallPlan>();
        AnalyzeExpr(model.Body, state, calls, isInsideNegation: false);
        return new AdornmentAnalysisResult(initialAdornment, calls);
    }

    private static Dictionary<VariableSymbol, bool> AnalyzeExpr(
        SemanticExpr expr,
        Dictionary<VariableSymbol, bool> state,
        List<AdornedCallPlan> calls,
        bool isInsideNegation)
    {
        switch (expr)
        {
            case ConjExpr conjunction:
                {
                    Dictionary<VariableSymbol, bool> current = Clone(state);
                    foreach (SemanticExpr part in conjunction.Parts)
                        current = AnalyzeExpr(part, current, calls, isInsideNegation);

                    return current;
                }

            case DisjExpr disjunction:
                {
                    Dictionary<VariableSymbol, bool> leftState = AnalyzeExpr(disjunction.Left, Clone(state), calls, isInsideNegation);
                    Dictionary<VariableSymbol, bool> rightState = AnalyzeExpr(disjunction.Right, Clone(state), calls, isInsideNegation);
                    return IntersectStates(leftState, rightState);
                }

            case WithExpr withExpr:
                {
                    Dictionary<VariableSymbol, bool> scoped = Clone(state);
                    foreach (VariableSymbol variable in withExpr.Variables)
                        scoped[variable] = false;

                    Dictionary<VariableSymbol, bool> scopedResult = AnalyzeExpr(withExpr.Body, scoped, calls, isInsideNegation);
                    var merged = Clone(state);
                    foreach (KeyValuePair<VariableSymbol, bool> binding in scopedResult)
                    {
                        if (state.ContainsKey(binding.Key))
                            merged[binding.Key] = binding.Value;
                    }

                    return merged;
                }

            case UnifyExpr unify:
                {
                    var updated = Clone(state);
                    PropagateBindings(unify.Left, unify.Right, updated);
                    PropagateBindings(unify.Right, unify.Left, updated);
                    return updated;
                }

            case NotExpr notExpr:
                {
                    _ = AnalyzeExpr(notExpr.Goal, Clone(state), calls, isInsideNegation: true);
                    return state;
                }

            case CallExpr call:
                {
                    Adornment adornment = Adornment.FromBoundArguments(call.Arguments.Select(argument => IsGround(argument, state)));
                    calls.Add(new AdornedCallPlan(call, adornment, isInsideNegation));
                    var updated = Clone(state);
                    foreach (SemanticExpr argument in call.Arguments)
                        BindIntroducedVariables(argument, updated);

                    return updated;
                }

            default:
                return state;
        }
    }

    private static void PropagateBindings(
        SemanticExpr source,
        SemanticExpr target,
        Dictionary<VariableSymbol, bool> state)
    {
        if (!IsGround(source, state))
            return;

        BindIntroducedVariables(target, state);
    }

    private static void BindIntroducedVariables(
        SemanticExpr expr,
        IDictionary<VariableSymbol, bool> state)
    {
        switch (expr)
        {
            case VarExpr varExpr:
                state[varExpr.Variable] = true;
                break;

            case ListConsExpr listCons:
                BindIntroducedVariables(listCons.Head, state);
                BindIntroducedVariables(listCons.Tail, state);
                break;
        }
    }

    private static bool IsGround(
        SemanticExpr expr,
        IReadOnlyDictionary<VariableSymbol, bool> state)
    {
        return expr switch
        {
            ConstExpr => true,
            ListEmptyExpr => true,
            VarExpr varExpr => state.TryGetValue(varExpr.Variable, out bool isBound) && isBound,
            FieldExpr fieldExpr => IsGround(fieldExpr.Target, state),
            ArithExpr arithExpr => IsGround(arithExpr.Left, state) && IsGround(arithExpr.Right, state),
            ListConsExpr listConsExpr => IsGround(listConsExpr.Head, state) && IsGround(listConsExpr.Tail, state),
            _ => false,
        };
    }

    private static Dictionary<VariableSymbol, bool> Clone(IReadOnlyDictionary<VariableSymbol, bool> state) =>
        state.ToDictionary(pair => pair.Key, pair => pair.Value);

    private static Dictionary<VariableSymbol, bool> IntersectStates(
        IReadOnlyDictionary<VariableSymbol, bool> left,
        IReadOnlyDictionary<VariableSymbol, bool> right)
    {
        var intersection = new Dictionary<VariableSymbol, bool>();
        foreach (VariableSymbol variable in left.Keys.Union(right.Keys))
        {
            bool leftBound = left.TryGetValue(variable, out bool leftValue) && leftValue;
            bool rightBound = right.TryGetValue(variable, out bool rightValue) && rightValue;
            intersection[variable] = leftBound && rightBound;
        }

        return intersection;
    }
}
