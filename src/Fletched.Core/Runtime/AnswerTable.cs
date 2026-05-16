using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace Fletched.Core.Runtime;

/// <summary>Query-scoped answer store for a single table key.</summary>
public sealed class AnswerTable<TAnswer>
    where TAnswer : notnull
{
    private readonly List<TAnswer> _answers = [];
    private readonly HashSet<TAnswer> _seen = [];

    public IReadOnlyList<TAnswer> Answers => _answers;

    public TableStatus Status { get; private set; } = TableStatus.Producing;

    public Exception? Fault { get; private set; }

    public bool TryAddAnswer(TAnswer answer)
    {
        ThrowIfFaulted();
        if (Status == TableStatus.Complete)
            throw new InvalidOperationException("Cannot add answers to a completed table.");

        if (!_seen.Add(answer))
            return false;

        _answers.Add(answer);
        return true;
    }

    public void MarkComplete()
    {
        ThrowIfFaulted();
        Status = TableStatus.Complete;
    }

    public void MarkFaulted(Exception exception)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));

        Fault = exception;
        Status = TableStatus.Faulted;
    }

    public void ThrowIfFaulted()
    {
        if (Fault is not null)
            ExceptionDispatchInfo.Capture(Fault).Throw();
    }
}
