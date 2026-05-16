using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fletched.Core.Runtime;

/// <summary>Query-scope coordinator for magic sources keyed by execution context object.</summary>
public static class QueryMagicRuntime
{
    private static readonly ConditionalWeakTable<object, QueryMagicRuntimeState> States = new();

    public static QueryScope EnterScope(object context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        QueryMagicRuntimeState state = States.GetOrCreateValue(context);
        state.Enter();
        return new QueryScope(context, state);
    }

    public static QueryMagicStore<TTuple> GetStore<TTuple>(object context)
        where TTuple : notnull
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        QueryMagicRuntimeState state = States.GetOrCreateValue(context);
        return state.GetStore<TTuple>();
    }

    public readonly struct QueryScope : IDisposable
    {
        private readonly object _context;
        private readonly QueryMagicRuntimeState _state;

        internal QueryScope(object context, QueryMagicRuntimeState state)
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

    internal sealed class QueryMagicRuntimeState
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
                if (_activeScopes == 0)
                    throw new InvalidOperationException("Query magic scope exited without a matching active scope.");

                _activeScopes--;
                if (_activeScopes == 0)
                    _stores.Clear();
            }
        }

        public QueryMagicStore<TTuple> GetStore<TTuple>()
            where TTuple : notnull
        {
            lock (_gate)
            {
                Type key = typeof(TTuple);
                if (_stores.TryGetValue(key, out object? store))
                    return (QueryMagicStore<TTuple>)store;

                var created = new QueryMagicStore<TTuple>();
                _stores.Add(key, created);
                return created;
            }
        }
    }
}
