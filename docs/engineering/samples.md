# Samples

## Purpose

This document defines the repository sample overview and execution guidance.

## Available Samples

### `samples/WorkAssignment`

`WorkAssignment` is a .NET console sample that:

- builds a weekday-only schedule for a concrete month with `Early` and `Late` shifts;
- reads worker availability from CSV or generates deterministic sample input;
- models fair scheduling with Fletched facts and predicates;
- prints the first five valid balanced assignments;
- prints in-memory engine metrics collected through `System.Diagnostics.Metrics`.


### `samples/Ontology.FoodSafety`

`Ontology.FoodSafety` is a .NET console sample that:

- loads deterministic, curated food ontology fixture data from CSV files;
- models ontology-style subclass traversal with recursive predicates;
- classifies unsafe and safe products for dietary profiles using negation and transitive reasoning;
- demonstrates major-allergen classification through ontology hierarchy.

Run the sample:

```sh
dotnet run --project samples/Ontology.FoodSafety
```

## Run the Sample

### Generated input

```sh
dotnet run --project samples/WorkAssignment -- --year 2026 --month 2 --workers 4 --seed alpha --max-recursion-depth 64
```

### CSV input

```sh
dotnet run --project samples/WorkAssignment -- --year 2026 --month 2 --csv /path/to/workers.csv --max-recursion-depth 64
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

Each no-shift entry uses an ISO date plus `Early` or `Late`. The parser accepts both `2026-02-04Late` and `2026-02-04 Late`. Dates must be weekday shifts that belong to the requested `--year` and `--month`.

## What the Sample Demonstrates

- module-scoped Fletched facts and predicates in an application project;
- describing a month-sized assignment problem in the Fletched DSL;
- executing generated query code from a console app;
- configuring recursion guard policy via `--max-recursion-depth`;
- reporting engine metrics including recursive metrics such as `recursive_invocations` and `recursive_depth`.

# Authority

This document is authoritative for:
- sample overview and execution guidance
- sample documentation routing under `docs/engineering/`

This document is not authoritative for:
- runtime behavior specifications
- sample test assertions

# Document Contract

## Related Documents

- `docs/ENGINEERING.md`
- `samples/WorkAssignment/`
- `samples/Ontology.FoodSafety/`
- `samples/Ontology.FoodSafety/`

## Must Be Updated Together

When sample guidance changes, review and update:
- `README.md`
- `samples/WorkAssignment/`
