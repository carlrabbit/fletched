# Samples

This repository includes runnable sample applications under `samples/`.

## WorkAssignment

Path: `samples/WorkAssignment`

`WorkAssignment` is a .NET console app that demonstrates how to model a small
assignment problem with Fletched.

It:

- builds a 7-day schedule with `Early` and `Late` shifts
- reads worker availability from CSV or generates deterministic sample input
- models fair scheduling with Fletched facts and predicates
- prints the first five valid balanced assignments
- prints in-memory engine metrics collected during execution

## Run the sample

### Generated input

```bash
dotnet run --project samples/WorkAssignment -- --workers 4 --seed alpha
```

This generates a deterministic worker set from the supplied seed and prints the
first five valid assignments.

### CSV input

```bash
dotnet run --project samples/WorkAssignment -- --csv /path/to/workers.csv
```

The CSV format is:

```text
worker;noshift1;noshift2;...
```

Example:

```text
Alice;MonEarly;WedLate
Bob;FridayEarly
Cara;TueLate
Dan
```

Supported shift tokens follow the sample's day/shift naming such as `MonEarly`,
and the parser also accepts longer aliases such as `MondayLate`.

## What the sample demonstrates

- module-scoped Fletched facts and predicates in an application project
- describing a complete assignment problem in the Fletched DSL
- executing the generated query code from a console app
- formatting multiple solutions and lightweight performance metrics for display
