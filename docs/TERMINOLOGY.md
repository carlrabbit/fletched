# Terminology

## EngineContext

The runtime container that exposes fact tables to generated predicate entry points.

## Fact

A typed record-like domain shape marked with `[Fact]` and stored in a `FactTable<T>`.

## Fact Table

The runtime storage structure that owns dense fact data and optional indexes for a single fact type.

## Indexed Source

An execution-plan data source that replaces a full fact scan with a keyed lookup when a bound or constant equality constraint exists.

## Logic Expression

A typed symbolic expression that represents relational constraints and composition in the DSL.

## Module

A generated scope boundary for facts and predicates in application assemblies that avoids global `EngineContext` collisions.

## Plan

The lowered execution description produced by the generator before C# emission.

## Predicate

A typed logical query shape marked with `[Predicate]` and compiled into executable enumeration code.

## Predicate Body

The method marked with `[PredicateBody]` that defines the logical constraints for a predicate arity.

## Source Generator

The Roslyn component that validates attributed source, builds semantic and planning models, and emits executable code.
