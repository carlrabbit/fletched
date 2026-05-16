using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fletched.Core.Runtime;

/// <summary>
/// Query-scope coordinator for table stores keyed by execution context object.
/// </summary>
public static class QueryTableRuntime
{
    private static readonly ConditionalWeakTable<object, QueryTableRuntimeState> States = new();

    public static QueryScope EnterScope(object context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        QueryTableRuntimeState state = States.GetOrCreateValue(context);
        state.Enter();
        return new QueryScope(context, state);
    }

    public static QueryTableStore<TAnswer> GetStore<TAnswer>(object context)
        where TAnswer : notnull
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        QueryTableRuntimeState state = States.GetOrCreateValue(context);
        return state.GetStore<TAnswer>();
    }

    public readonly struct QueryScope : IDisposable
    {
        private readonly object _context;
        private readonly QueryTableRuntimeState _state;

        internal QueryScope(object context, QueryTableRuntimeState state)
        {
            _context = context;
            _state = state;
        }

        public void Dispose()
        {
            _state.Exit();
            GC.KeepAlive(_context);
        }
    }

    internal sealed class QueryTableRuntimeState
    {
        private readonly object _gate = new();
        private readonly Dictionary<Type, object> _stores = [];
        private int _activeScopes;

        public void Enter()
        {
            lock (_gate)
            {
                if (_activeScopes == 0)
                    _stores.Clear();

                _activeScopes++;
            }
        }

        public void Exit()
        {
            lock (_gate)
            {
                if (_activeScopes <= 0)
                    return;

                _activeScopes--;
                if (_activeScopes == 0)
                    _stores.Clear();
            }
        }

        public QueryTableStore<TAnswer> GetStore<TAnswer>()
            where TAnswer : notnull
        {
            lock (_gate)
            {
                Type key = typeof(TAnswer);
                if (_stores.TryGetValue(key, out object? store))
                    return (QueryTableStore<TAnswer>)store;

                var created = new QueryTableStore<TAnswer>();
                _stores.Add(key, created);
                return created;
            }
        }
    }
}
