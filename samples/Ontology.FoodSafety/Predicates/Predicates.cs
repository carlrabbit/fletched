using Fletched.Core;

namespace Ontology.FoodSafety;

public static partial class FoodSafetyModule
{
    [Predicate]
    public readonly partial record struct IsA
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> child, TerminalVar<string> parent) =>
            Logic.Or(
                () => Logic.With<SubClassOf>(s => s.Child == child && s.Parent == parent),
                () => Logic.With<SubClassOf>(s => s.Child == child && IsA(s.Parent, parent)));
    }

    [Fact, Predicate]
    public readonly partial record struct DirectlyContainsIngredient(string ProductId, string Ingredient)
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> productId, TerminalVar<string> ingredient) =>
            Logic.With<ProductIngredient>(pi => pi.ProductId == productId && pi.Ingredient == ingredient);
    }

    [Predicate]
    public readonly partial record struct ContainsKindOfIngredient
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> productId, TerminalVar<string> concept, TerminalVar<string> ingredient) =>
            Logic.With<ProductIngredient>(pi => pi.ProductId == productId && pi.Ingredient == ingredient && IsA(pi.Ingredient, concept));
    }

    [Predicate]
    public readonly partial record struct UnsafeProductForProfile
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> productId, TerminalVar<string> profileId, TerminalVar<string> reasonConcept, TerminalVar<string> ingredient) =>
            Logic.With<Avoids>(a => a.ProfileId == profileId && a.Concept == reasonConcept && ContainsKindOfIngredient(productId, a.Concept, ingredient));
    }

    [Predicate]
    public readonly partial record struct SafeProductForProfile
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> productId, TerminalVar<string> profileId) =>
            Logic.With<Product, DietaryProfile>((p, profile) =>
                p.ProductId == productId &&
                profile.ProfileId == profileId &&
                Logic.With<string, string>((reasonConcept, ingredient) =>
                    Logic.Not(UnsafeProductForProfile(productId, profileId, reasonConcept, ingredient))));
    }

    [Predicate]
    public readonly partial record struct ProductHasMajorAllergen
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> productId, TerminalVar<string> ingredient) =>
            ContainsKindOfIngredient(productId, "major_allergen", ingredient);
    }
}
