DSL.md


1. Overview


Defines the embedded C# domain-specific language used to express logical facts and predicates.
The DSL is compiled via source generation into an intermediate representation and execution plan.


---


2. Core Concepts / Data Structures


Attributes


[Fact]
partial record struct T(...);


[Predicate]
partial record struct P;


[PredicateBody]
LogicExpr<TResult> Body(...);


---


Logical Expression Wrapper


readonly struct LogicExpr<T>
{
    internal ExprNode Node;
}


---


Terminal Variables


readonly struct TerminalVar<T>;


- Represents externally visible variables
- Must be bound at query completion


---


Scoped Variable Introduction


LogicExpr<bool> With<T1, ..., TN>(
    Func<Proxy<T1>, ..., Proxy<TN>, LogicExpr<bool>> body
);


- Introduces scoped variables of fact types
- Each type corresponds to one logical variable


---


Proxies


Generated per "[Fact]" type:


readonly struct Proxy<T>
{
    public LogicExpr<TField> Field { get; }
}


- Exposes fields as "LogicExpr<T>"
- Captures member access structurally


---


Operators


LogicExpr<bool> operator ==(LogicExpr<T> a, LogicExpr<T> b);
LogicExpr<bool> operator !=(LogicExpr<T> a, LogicExpr<T> b);


LogicExpr<bool> operator &&(LogicExpr<bool> a, LogicExpr<bool> b);
LogicExpr<bool> operator ||(LogicExpr<bool> a, LogicExpr<bool> b);
LogicExpr<bool> Not(LogicExpr<bool> goal);


---


Constants


LogicExpr<T> Constant<T>(T value);


Implicit conversion from literals is supported.


---


Constraints (Method Calls)


LogicExpr<bool> MethodCall(...);


- Allowed only on "LogicExpr<T>" values
- Must return "bool"


---


3. Rules and Invariants


General


- All DSL expressions must be representable as "LogicExpr<T>"
- No side effects are permitted inside predicate bodies
- Execution semantics are defined by source-generated code only


---


Variables


- "TerminalVar<T>" represents query boundary variables
- Variables introduced via "With<...>" are scoped to the lambda
- Variables are immutable at DSL level


---


Types


- All expressions are statically typed
- Unification requires both operands to have identical types
- Field access types must match declared member types


---


Unification


- "==" represents logical unification, not value comparison
- "!=" represents inequality constraint
- Unification is symmetric


---


Composition


- "&&" represents conjunction (logical AND)
- "||" represents disjunction (logical OR)
- "Not(expr)" represents negation-as-failure
- Expressions are pure and declarative


---


Member Access


- Only direct property/field access on "[Fact]" types is allowed
- Member access is captured as structural expression nodes
- No dynamic or reflection-based access is allowed


---


Constraints


- Method calls must:
  - Return "bool"
  - Be side-effect free
- Arguments must be "LogicExpr<T>" or constants


---


Restrictions


- No loops, assignments, or control flow constructs
- No mutation of variables
- No invocation of arbitrary external code
- Positive recursion is allowed only through predicate calls and keeps ordinary predicate invocation semantics
- Recursive negation is not allowed
- "Not(expr)" requires all referenced outward-visible variables to be ground at evaluation time
- "Not(expr)" must not introduce outward-visible bindings


---


4. Execution / Behavior


- DSL expressions are not executed directly
- Expressions are translated into an intermediate representation (IR)
- Operators construct expression trees ("ExprNode")
- Variable binding and control flow are resolved during code generation
- Evaluation is performed by generated C# code


---


5. Examples


Example 1: Simple Predicate


[Fact]
partial record struct User(string Login, string Name);


[Predicate]
partial record struct UsersByName
{
    [PredicateBody]
    LogicExpr<bool> Body(TerminalVar<string> name) =>
        With<User>(user =>
            user.Name == name
        );
}


---


Example 2: Join


[Fact]
partial record struct Admin(string Login);


[Predicate]
partial record struct AdminUsers
{
    [PredicateBody]
    LogicExpr<bool> Body(TerminalVar<string> name) =>
        With<User, Admin>((user, admin) =>
            user.Name == name &&
            user.Login == admin.Login
        );
}


---


Example 3: Constraint


With<User>(user =>
    user.Name.StartsWith("A")
);


---


Example 4: Disjunction


With<User>(user =>
    user.Name == "Alice" ||
    user.Name == "Bob"
);


---


Example 5: Mixed Expression


With<User, Admin>((user, admin) =>
    user.Login == admin.Login &&
    user.Name.StartsWith("A")
);

---

Example 6: Negation-as-Failure

With<User>(user =>
    user.Name == name &&
    Not(With<Admin>(admin => admin.Login == user.Login))
);
