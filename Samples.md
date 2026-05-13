# Samples

This repository includes runnable sample applications under `samples/`.

## WorkAssignment

Path: `samples/WorkAssignment`

`WorkAssignment` is a .NET console app that demonstrates how to model a small
assignment problem with Fletched.

It:

- builds a weekday-only schedule for a concrete month with `Early` and `Late` shifts
- reads worker availability from CSV or generates deterministic sample input for the requested month
- models fair scheduling with Fletched facts and predicates
- prints the first five valid balanced assignments
- prints in-memory engine metrics collected through `System.Diagnostics.Metrics`

## Run the sample

### Generated input

```bash
dotnet run --project samples/WorkAssignment -- --year 2026 --month 2 --workers 4 --seed alpha
```

This generates a deterministic worker set for the requested month from the
supplied seed and prints the first five valid assignments. Weekend dates are
excluded automatically.

### CSV input

```bash
dotnet run --project samples/WorkAssignment -- --year 2026 --month 2 --csv /path/to/workers.csv
```

The CSV format is:

```text
worker;noshift1;noshift2;...
```

Example:

```text
Alice;2026-02-02Early;2026-02-04Late
Bob;2026-02-06Early
Cara;2026-02-17Late
Dan
```

Each no-shift entry uses an ISO date plus `Early` or `Late`. The parser accepts
both `2026-02-04Late` and `2026-02-04 Late`. Dates must be weekday shifts that
belong to the requested `--year` and `--month`.

## What the sample demonstrates

- module-scoped Fletched facts and predicates in an application project
- describing a month-sized assignment problem in the Fletched DSL
- executing the generated query code from a console app
- formatting multiple solutions and engine metrics gathered from `EngineMetrics`
