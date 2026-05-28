# Milestone 16: Food Ontology Sample

## Goal

Add a real-life ontology-style sample that demonstrates Fletched as a typed, source-generated logic engine over curated product and ingredient data.

The sample must show how ontology-like facts, recursive predicates, indexes, negation, disjunction, list/built-in predicates, explanations, and metrics can be combined to answer practical classification and safety questions.

The milestone produces a repository-ready sample, not a new core language feature.

## Repository Path

```text
docs/milestones/016-food-ontology-sample.md
```

## Sample Path

```text
samples/Ontology.FoodSafety/
```

## Research and Repository Requirements

The implementation must follow the current repository guides:

```text
docs/research/project-setup-guide-v5.md
docs/research/engineering-guide-v4.md
```

The milestone document must be updated during implementation to meet repository requirements, including updating the milestone index document.

Required index update:

```text
docs/milestones/index.md
```

If the repository uses a different canonical milestone index path, update that canonical index instead and keep this milestone document consistent with it.

The sample must obey the repository README rule:

```text
Only the repository root may contain README.md.
The sample must not add samples/Ontology.FoodSafety/README.md.
Use samples/Ontology.FoodSafety/overview.md instead.
```

## Scope

Create a sample named:

```text
Ontology.FoodSafety
```

The sample models:

```text
food concepts
ingredient categories
allergen categories
dietary profile restrictions
products
product ingredients
unsafe-product classification
safe-product classification
reason/explanation queries
```

The sample must use curated, checked-in fixture data. It must not require network access at build time, test time, or sample execution time.

## Data Policy

The sample must use small, deterministic fixture data.

Required fixture size:

```text
Concepts:              20-80 rows
Subclass edges:        30-120 rows
Products:              20-100 rows
Product ingredients:   60-400 rows
Profiles:              3-10 rows
Avoidance rules:       5-30 rows
```

The sample may be inspired by real public data sources and ontology vocabularies, but checked-in data must be curated and small.

Preferred external references:

```text
FoodOn / OBO-style food ontology concepts
Open Food Facts product/ingredient-style data
```

The sample must include attribution documentation for any source-derived data.

Required attribution file:

```text
samples/Ontology.FoodSafety/data-sources.md
```

The attribution file must state:

```text
- which external data or ontology sources inspired the fixture;
- whether fixture rows are directly copied, transformed, or hand-curated;
- the license or reuse note for each source;
- that the checked-in fixture is a small deterministic sample;
- that the sample is not a medical, nutritional, allergy, or legal authority.
```

## Non-Goals

Do not implement:

```text
full OWL reasoning
RDF parser
SPARQL parser
Turtle parser
JSON-LD parser
Open Food Facts API client
FoodOn importer
large dataset ingestion
runtime network download
medical advice
nutritional recommendation engine
allergen-safety guarantee
package feature changes
core DSL feature changes
new compiler features
```

The sample must demonstrate existing Fletched capabilities. Any missing engine capability must be represented as a blocking prerequisite, not implemented inside the sample.

## Required Repository Layout

```text
samples/Ontology.FoodSafety/
  Ontology.FoodSafety.csproj
  Program.cs
  overview.md
  data-sources.md

  Data/
    concepts.csv
    subclass-of.csv
    products.csv
    product-ingredients.csv
    profiles.csv
    avoids.csv

  Facts/
    FoodConcept.cs
    SubClassOf.cs
    Product.cs
    ProductIngredient.cs
    DietaryProfile.cs
    Avoids.cs

  Predicates/
    IsA.cs
    DirectlyContainsIngredient.cs
    ContainsKindOfIngredient.cs
    UnsafeProductForProfile.cs
    SafeProductForProfile.cs
    UnsafeReason.cs
    ProductHasMajorAllergen.cs

  Loading/
    CsvLoader.cs
    SampleData.cs

  Output/
    ConsoleReporter.cs
```

If the repository uses a centralized sample structure, adapt the physical layout but preserve the same logical files and responsibilities.

## Sample Project Requirements

The sample project must be a normal consumer-style project.

Required properties:

```text
- references Fletched packages/projects the same way other samples do;
- does not depend on test-only utilities;
- does not depend on internal compiler namespaces;
- does not require generated-code inspection to understand the sample;
- runs through dotnet run;
- participates in repository build/check scripts if samples are included there.
```

The sample must compile against the public API surface intended for consumers.

## Domain Model

### FoodConcept

Represents a concept in the food ontology slice.

```csharp
[Fact]
public readonly partial record struct FoodConcept(
    string Id,
    string Label,
    string Kind);
```

Rules:

```text
Id is stable and lowercase snake_case.
Label is human-readable.
Kind classifies the concept as ingredient, category, allergen, diet, or product_category.
```

Example rows:

```csv
Id,Label,Kind
almond,Almond,ingredient
tree_nut,Tree nut,category
nut,Nut,category
major_allergen,Major allergen,allergen
dairy,Dairy,category
milk,Milk,ingredient
gluten_source,Gluten source,category
wheat,Wheat,ingredient
soy,Soy,ingredient
legume,Legume,category
```

### SubClassOf

Represents the ontology hierarchy.

```csharp
[Fact]
[FactIndex(nameof(Child))]
[FactIndex(nameof(Parent))]
public readonly partial record struct SubClassOf(
    string Child,
    string Parent);
```

Rules:

```text
Child and Parent reference FoodConcept.Id.
The graph must be acyclic in fixture data.
The graph must contain at least one hierarchy with depth >= 3.
The graph must contain at least one shared ancestor.
```

Example rows:

```csv
Child,Parent
almond,tree_nut
cashew,tree_nut
tree_nut,nut
nut,major_allergen
peanut,legume
soy,legume
legume,plant_food
milk,dairy
dairy,major_allergen
wheat,gluten_source
gluten_source,major_allergen
```

### Product

Represents a product-like item.

```csharp
[Fact]
[FactIndex(nameof(ProductId))]
public readonly partial record struct Product(
    string ProductId,
    string Name,
    string Category);
```

Rules:

```text
ProductId is stable.
Name is human-readable.
Category references a FoodConcept.Id when possible.
```

Example rows:

```csv
ProductId,Name,Category
p001,Chocolate Almond Bar,snack
p002,Oat Cookie,bakery
p003,Soy Protein Shake,beverage
p004,Plain Rice Cakes,snack
```

### ProductIngredient

Represents product ingredient membership.

```csharp
[Fact]
[FactIndex(nameof(ProductId))]
[FactIndex(nameof(Ingredient))]
public readonly partial record struct ProductIngredient(
    string ProductId,
    string Ingredient);
```

Rules:

```text
ProductId references Product.ProductId.
Ingredient references FoodConcept.Id.
A product may have multiple ingredient rows.
Ingredient order is not semantically significant unless a separate Position field is added.
```

Example rows:

```csv
ProductId,Ingredient
p001,almond
p001,milk
p002,wheat
p003,soy
p004,rice
```

### DietaryProfile

Represents a user-facing restriction profile.

```csharp
[Fact]
[FactIndex(nameof(ProfileId))]
public readonly partial record struct DietaryProfile(
    string ProfileId,
    string Label);
```

Example rows:

```csv
ProfileId,Label
nut_free,Nut Free
gluten_free,Gluten Free
vegan,Vegan
major_allergen_free,Major Allergen Free
```

### Avoids

Represents a profile restriction against a concept.

```csharp
[Fact]
[FactIndex(nameof(ProfileId))]
[FactIndex(nameof(Concept))]
public readonly partial record struct Avoids(
    string ProfileId,
    string Concept);
```

Rules:

```text
ProfileId references DietaryProfile.ProfileId.
Concept references FoodConcept.Id.
A profile may avoid both specific ingredients and higher-level categories.
```

Example rows:

```csv
ProfileId,Concept
nut_free,nut
gluten_free,gluten_source
vegan,dairy
major_allergen_free,major_allergen
```

## Predicate Requirements

### IsA

Computes ontology reachability.

Logical shape:

```prolog
is_a(X, Y) :-
    subclass_of(X, Y).

is_a(X, Y) :-
    subclass_of(X, Z),
    is_a(Z, Y).
```

Required behavior:

```text
- direct subclass relation succeeds;
- transitive subclass relation succeeds;
- unrelated concepts fail;
- recursive traversal terminates on fixture data;
- duplicate output behavior follows engine semantics;
- indexed lookup is used when Child or Parent is bound where supported.
```

Suggested source shape:

```csharp
[Predicate]
public readonly partial record struct IsA
{
    [PredicateBody]
    public LogicExpr Body(Var<string> child, Var<string> parent) =>
        Logic.Or(
            () => With<SubClassOf>(s =>
                s.Child == child &&
                s.Parent == parent),
            () => With<SubClassOf>(s =>
                s.Child == child &&
                IsA.Query(s.Parent, parent)));
}
```

Adapt invocation syntax to the current public API.

### DirectlyContainsIngredient

Tests direct product ingredient membership.

Logical shape:

```prolog
directly_contains_ingredient(Product, Ingredient) :-
    product_ingredient(Product, Ingredient).
```

Suggested source shape:

```csharp
[Predicate]
public readonly partial record struct DirectlyContainsIngredient
{
    [PredicateBody]
    public LogicExpr Body(
        Var<string> productId,
        Var<string> ingredient) =>
        With<ProductIngredient>(pi =>
            pi.ProductId == productId &&
            pi.Ingredient == ingredient);
}
```

### ContainsKindOfIngredient

Tests whether a product contains an ingredient that is a kind of a requested concept.

Logical shape:

```prolog
contains_kind_of_ingredient(Product, Concept, Ingredient) :-
    product_ingredient(Product, Ingredient),
    is_a(Ingredient, Concept).
```

Required behavior:

```text
- binds the concrete ingredient causing the match;
- supports queries by product;
- supports queries by avoided concept;
- uses ProductIngredient indexes where possible.
```

Suggested source shape:

```csharp
[Predicate]
public readonly partial record struct ContainsKindOfIngredient
{
    [PredicateBody]
    public LogicExpr Body(
        Var<string> productId,
        Var<string> concept,
        TerminalVar<string> ingredient) =>
        With<ProductIngredient>(pi =>
            pi.ProductId == productId &&
            pi.Ingredient == ingredient &&
            IsA.Query(pi.Ingredient, concept));
}
```

Adapt TerminalVar usage to the current projection rules.

### UnsafeProductForProfile

Finds products unsafe for a profile.

Logical shape:

```prolog
unsafe_product_for_profile(Product, Profile, ReasonConcept, Ingredient) :-
    avoids(Profile, ReasonConcept),
    contains_kind_of_ingredient(Product, ReasonConcept, Ingredient).
```

Required outputs:

```text
ProductId
ProfileId
ReasonConcept
Ingredient
```

Suggested source shape:

```csharp
[Predicate]
public readonly partial record struct UnsafeProductForProfile
{
    [PredicateBody]
    public LogicExpr Body(
        TerminalVar<string> productId,
        TerminalVar<string> profileId,
        TerminalVar<string> reasonConcept,
        TerminalVar<string> ingredient) =>
        With<Avoids>(a =>
            a.ProfileId == profileId &&
            a.Concept == reasonConcept &&
            ContainsKindOfIngredient.Query(productId, a.Concept, ingredient));
}
```

### SafeProductForProfile

Finds products for which no unsafe reason exists.

Logical shape:

```prolog
safe_product_for_profile(Product, Profile) :-
    product(Product, _, _),
    dietary_profile(Profile, _),
    not(unsafe_product_for_profile(Product, Profile, _, _)).
```

Required behavior:

```text
- uses negation only with grounded Product and Profile;
- does not allow variables to escape from negation;
- returns products with no matching avoidance reason;
- demonstrates existing Not semantics.
```

Suggested source shape:

```csharp
[Predicate]
public readonly partial record struct SafeProductForProfile
{
    [PredicateBody]
    public LogicExpr Body(
        TerminalVar<string> productId,
        TerminalVar<string> profileId) =>
        With<Product, DietaryProfile>((p, profile) =>
            p.ProductId == productId &&
            profile.ProfileId == profileId &&
            Logic.Not(() =>
                UnsafeProductForProfile.Query(
                    productId,
                    profileId,
                    Var<string>.Discard,
                    Var<string>.Discard)));
}
```

Adapt discard variable syntax to the current API.

### UnsafeReason

Explains why a specific product is unsafe for a profile.

Required inputs:

```text
ProductId bound
ProfileId bound
```

Required outputs:

```text
ProductName
ProfileLabel
Ingredient
ReasonConcept
ReasonLabel
```

Suggested facts used:

```text
Product
DietaryProfile
FoodConcept
Avoids
ProductIngredient
SubClassOf / IsA
```

Required behavior:

```text
- produces human-readable explanation rows;
- supports multiple reasons;
- preserves duplicate behavior according to engine semantics;
- is used by console output.
```

### ProductHasMajorAllergen

Demonstrates classification against a top-level ontology concept.

Logical shape:

```prolog
product_has_major_allergen(Product, Ingredient) :-
    contains_kind_of_ingredient(Product, major_allergen, Ingredient).
```

Required behavior:

```text
- demonstrates partial application with a constant concept;
- supports listing all products containing major allergens;
- demonstrates classification through transitive hierarchy.
```

## Required Queries in Program.cs

The console sample must run deterministic queries and print stable output.

Required sections:

```text
1. Products unsafe for nut_free
2. Products unsafe for gluten_free
3. Products safe for nut_free
4. Major allergen classification
5. Explanation for a specific product/profile pair
6. Optional: plan explanation or metrics output when supported by public API
```

Example output shape:

```text
== Unsafe products for nut_free ==
p001 Chocolate Almond Bar
  ingredient: almond
  reason: nut

== Safe products for nut_free ==
p002 Oat Cookie
p003 Soy Protein Shake
p004 Plain Rice Cakes

== Why is p001 unsafe for vegan? ==
Product: Chocolate Almond Bar
Profile: Vegan
Ingredient: milk
Matched restriction: dairy
Path: milk -> dairy
```

The exact output may differ, but it must be deterministic and documented in `overview.md`.

## Ontology Path Explanation

If supported by the current engine, add a predicate that returns the hierarchy path:

```text
milk -> dairy
almond -> tree_nut -> nut
```

If list support is available, represent paths as typed lists.

Suggested optional predicate:

```text
IsAPath(child, ancestor, path)
```

If this would require unsupported list or recursive path behavior, omit it and document the reason in the sample overview.

## Data Loading

Add a simple deterministic CSV loader for the sample fixtures.

Requirements:

```text
- no external package required unless repository already uses one;
- validates required columns;
- validates missing concept references;
- validates missing product references;
- validates duplicate primary identifiers;
- validates acyclic subclass fixture graph;
- produces deterministic errors;
- loads into generated EngineContext / fact tables.
```

The loader may be sample-local code.

Required validation failures:

```text
UnknownConcept
UnknownProduct
UnknownProfile
DuplicateConcept
DuplicateProduct
DuplicateProfile
CycleInSubclassGraph
MissingRequiredColumn
InvalidCsvRow
```

These are sample validation errors, not Fletched compiler diagnostics.

## Indexing Requirements

The sample should include fact indexes where useful.

Required index candidates:

```text
SubClassOf.Child
SubClassOf.Parent
Product.ProductId
ProductIngredient.ProductId
ProductIngredient.Ingredient
DietaryProfile.ProfileId
Avoids.ProfileId
Avoids.Concept
```

Purpose:

```text
- recursive IsA traversal should not require avoidable full scans when Child is bound;
- product-to-ingredient lookup should be indexable;
- profile-to-restriction lookup should be indexable;
- explanation queries should have predictable access paths.
```

If the current engine cannot use one of these indexes, keep the declaration only if supported and document the limitation.

## Explanation and Metrics

If public APIs exist for plan explanations and metrics, the sample must include an optional execution mode.

Recommended command-line options:

```text
--explain
--metrics
```

Behavior:

```text
dotnet run -- --explain
dotnet run -- --metrics
dotnet run -- --explain --metrics
```

`--explain` should print or save a plan/query explanation for at least:

```text
UnsafeProductForProfile
SafeProductForProfile
IsA
```

`--metrics` should print query metrics for at least:

```text
unsafe products for nut_free
safe products for nut_free
major allergen classification
```

If explanation or metrics APIs are not public, omit the feature and document that it is intentionally unavailable in the sample.

## Public Documentation Requirements

Add:

```text
samples/Ontology.FoodSafety/overview.md
samples/Ontology.FoodSafety/data-sources.md
```

`overview.md` must include:

```text
- sample purpose;
- what ontology-style reasoning means in this sample;
- explicit statement that this is not full OWL/RDF reasoning;
- project layout;
- how to run;
- expected output;
- explanation of facts;
- explanation of predicates;
- explanation of safe vs unsafe query semantics;
- note about negation requiring grounded product/profile;
- note about fixture data being curated and non-authoritative.
```

`data-sources.md` must include:

```text
- external source inspiration;
- license notes;
- attribution;
- fixture derivation statement;
- non-authoritative data disclaimer.
```

## Build and Check Integration

The sample must be integrated into repository validation according to the current engineering guide.

Required behavior:

```text
./eng/build.sh builds the sample if samples are included in build.
./eng/test.sh validates sample behavior if sample tests are part of test suite.
./eng/check.sh passes after the sample is added.
```

If samples are not built by default, add the sample to the appropriate sample validation command and document that routing in `overview.md`.

## Tests

Add tests only where repository sample policy requires them.

Recommended test location:

```text
tests/Fletched.Samples.Tests/OntologyFoodSafetyTests.cs
```

Required behavioral checks:

```text
IsA direct relation succeeds.
IsA transitive relation succeeds.
IsA unrelated relation fails.
UnsafeProductForProfile returns expected rows for nut_free.
UnsafeProductForProfile returns expected rows for gluten_free.
SafeProductForProfile excludes unsafe products.
SafeProductForProfile includes products with no matching restriction.
UnsafeReason returns expected human-readable reason.
ProductHasMajorAllergen classifies through transitive hierarchy.
Sample output is deterministic.
Fixture validation rejects unknown concepts.
Fixture validation rejects subclass cycles.
```

If sample tests are intentionally avoided, the sample must still be exercised by a script or build command.

## Determinism Rules

The sample must be deterministic.

Required rules:

```text
- checked-in fixture data only;
- no network calls;
- stable CSV row ordering;
- stable query output ordering;
- stable console output;
- stable validation errors;
- stable generated code behavior;
- no dependency on local culture for sorting or formatting.
```

If query result ordering depends on fact insertion order, fixture loading order must be explicit and documented.

## Safety and Disclaimer Requirements

The sample must include this disclaimer or equivalent wording:

```text
This sample demonstrates ontology-style logic queries over curated fixture data.
It is not medical, allergy, nutritional, legal, or safety advice.
Do not use it to determine whether a product is safe to consume.
```

The sample must not claim that its output is complete, authoritative, or clinically safe.

## Implementation Status (2026-05-28)

Implemented in `samples/Ontology.FoodSafety/` with curated fixture CSV data, ontology facts/predicates, deterministic console output, and repository index updates in `docs/MILESTONES.md` and `docs/engineering/samples.md`.

## Acceptance Criteria

```text
- samples/Ontology.FoodSafety/ exists.
- The sample has no non-root README.md.
- samples/Ontology.FoodSafety/overview.md exists.
- samples/Ontology.FoodSafety/data-sources.md exists.
- Fixture CSV files exist under samples/Ontology.FoodSafety/Data/.
- FoodConcept fact exists.
- SubClassOf fact exists.
- Product fact exists.
- ProductIngredient fact exists.
- DietaryProfile fact exists.
- Avoids fact exists.
- IsA predicate exists and supports direct hierarchy queries.
- IsA predicate supports transitive hierarchy queries.
- DirectlyContainsIngredient predicate exists.
- ContainsKindOfIngredient predicate exists.
- UnsafeProductForProfile predicate exists.
- SafeProductForProfile predicate exists.
- UnsafeReason predicate exists.
- ProductHasMajorAllergen predicate exists.
- Product/profile safety queries run from Program.cs.
- The sample prints deterministic output.
- The sample uses checked-in data only.
- The sample performs no network access.
- The sample uses indexes where supported.
- The sample demonstrates recursive ontology traversal.
- The sample demonstrates negation with grounded inputs.
- The sample demonstrates explanation output if public API support exists.
- The sample demonstrates metrics output if public API support exists.
- The sample includes non-authoritative data disclaimer.
- Data source attribution exists.
- Fixture validation rejects invalid references.
- Fixture validation rejects subclass cycles.
- Tests or validation scripts cover the required sample behavior.
- docs/milestones/index.md or the repository's canonical milestone index is updated.
- This milestone document is updated to meet repository milestone-document requirements.
- The implementation follows docs/research/project-setup-guide-v5.md.
- The implementation follows docs/research/engineering-guide-v4.md.
- ./eng/check.sh passes.
```

## Suggested Implementation Sequence

```text
1. Add docs/milestones/016-food-ontology-sample.md.
2. Update docs/milestones/index.md or the canonical milestone index.
3. Create samples/Ontology.FoodSafety project.
4. Add sample overview and data-sources documents.
5. Add fixture CSV files.
6. Add fact records.
7. Add fact indexes where supported.
8. Add CSV loader and fixture validation.
9. Add IsA predicate.
10. Add product ingredient predicates.
11. Add unsafe/safe classification predicates.
12. Add reason/explanation predicate.
13. Add Program.cs query output.
14. Add optional --explain support if public API exists.
15. Add optional --metrics support if public API exists.
16. Add sample tests or validation script.
17. Ensure no non-root README.md was added.
18. Run ./eng/check.sh.
```

## Completion Rule

The milestone is complete only when the Food Safety ontology sample is implemented, deterministic, documented, attribution-compliant, integrated into repository validation, indexed where supported, tested or otherwise validated, represented in the milestone index, and `./eng/check.sh` passes.
