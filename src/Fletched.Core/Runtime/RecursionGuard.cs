using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Fletched.Core.Runtime;

/// <summary>Runtime recursion guard and depth tracking for predicate invocation chains.</summary>
public static class RecursionGuard
{
    private sealed class RecursionGuardState
    {
        public int Depth;
        public int NegationDepth;
        public bool HasConfiguredMaxDepth;
        public int? ConfiguredMaxDepth;
        public Stack<string> CallChain { get; } = new();
    }

    private static readonly ConditionalWeakTable<object, RecursionGuardState> States = new();

    public static int GetCurrentDepth(object context)
    {
        if (context is null)
            return 0;

        return GetState(context).Depth;
    }

    public static void EnterNegationScope(object context)
    {
        if (context is null)
            return;

        GetState(context).NegationDepth++;
    }

    public static void ExitNegationScope(object context)
    {
        if (context is null)
            return;

        RecursionGuardState state = GetState(context);
        if (state.NegationDepth > 0)
            state.NegationDepth--;
    }


    public static void SetMaxRecursionDepth(object context, int? maxDepth)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        if (maxDepth is <= 0)
            throw new InvalidRecursionDepthConfigurationException(maxDepth.Value);

        RecursionGuardState state = GetState(context);
        state.HasConfiguredMaxDepth = true;
        state.ConfiguredMaxDepth = maxDepth;
    }

    public static void EnterPredicateInvocation(object context, string predicateName, object? observer = null)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        RecursionGuardState state = GetState(context);
        int nextDepth = state.Depth + 1;
        int? maxDepth = ResolveRuntimeOptions(context)?.MaxRecursionDepth;
        if (maxDepth is null && state.HasConfiguredMaxDepth)
            maxDepth = state.ConfiguredMaxDepth;

        if (maxDepth is int configuredDepth && nextDepth > configuredDepth)
        {
            bool insideNegation = state.NegationDepth > 0;
            string[] callChain = BuildCallChain(state.CallChain, predicateName);
            NotifyRecursiveDepthExceeded(observer, predicateName, nextDepth, configuredDepth, insideNegation);
            throw new RecursiveDepthExceededException(predicateName, nextDepth, configuredDepth, callChain, insideNegation);
        }

        state.Depth = nextDepth;
        state.CallChain.Push(predicateName);
        NotifyRecursiveInvocation(observer, predicateName, nextDepth);
    }

    public static void ExitPredicateInvocation(object context)
    {
        if (context is null)
            return;

        RecursionGuardState state = GetState(context);

        if (state.Depth > 0)
            state.Depth--;

        if (state.CallChain.Count > 0)
            state.CallChain.Pop();
    }

    private static RecursionGuardState GetState(object context) =>
        States.GetValue(context, _ => new RecursionGuardState());

    private static EngineRuntimeOptions? ResolveRuntimeOptions(object context)
    {
        if (context is EngineContext engineContext)
            return engineContext.RuntimeOptions;

        PropertyInfo? runtimeOptionsProperty = context.GetType().GetProperty(
            "RuntimeOptions",
            BindingFlags.Public | BindingFlags.Instance);

        return runtimeOptionsProperty?.GetValue(context) as EngineRuntimeOptions;
    }

    private static string[] BuildCallChain(Stack<string> existingChain, string nextPredicate)
    {
        if (existingChain.Count == 0)
            return [nextPredicate];

        return existingChain.Reverse().Concat([nextPredicate]).ToArray();
    }

    private static void NotifyRecursiveInvocation(object? observer, string predicateName, int depth)
    {
#if !NETSTANDARD2_0
        if (observer is Fletched.Core.Performance.IExecutionObserver executionObserver)
            executionObserver.OnRecursiveInvocation(predicateName, depth);
#endif
    }

    private static void NotifyRecursiveDepthExceeded(
        object? observer,
        string predicateName,
        int depth,
        int maxDepth,
        bool insideNegation)
    {
#if !NETSTANDARD2_0
        if (observer is Fletched.Core.Performance.IExecutionObserver executionObserver)
            executionObserver.OnRecursiveDepthExceeded(predicateName, depth, maxDepth, insideNegation);
#endif
    }
}
