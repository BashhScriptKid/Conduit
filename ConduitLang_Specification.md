# **Conduit Language Specification – Version 7**  
*(January 05, 2026)*

This specification is not covered under repository's Apache 2.0 license, but instead under the Creative Commons' [CC-BY-SA](https://creativecommons.org/licenses/by-sa/4.0/_) license

## **Introduction**

Conduit is a systems programming language. 

It focuses on safety, performance, and developer ergonomics.

The language draws from established paradigms. 

It incorporates Rust's memory safety and ownership model. 

It adopts C#'s readable syntax and usability features. 

It includes C's direct control over hardware and predictable execution.

Conduit transpiles to Rust. 

This allows it to leverage Rust's borrow checker, package ecosystem, and runtime performance. 

At the same time, it provides a more approachable surface syntax for developers.

Files use the `.cndt` extension.

### **Rebranding Note**
This language was formerly known as Cb or C-flat in early development stages. 

The name was rebranded to Conduit due to existing name conflicts with other projects and esoteric languages. 

Originally, Cb was envisioned as a conceptual "lower version" of C#—aiming to carry C#'s ergonomics while incorporating C++-like features without historical baggage. 

However, the focus shifted to transpilation and Rust integration, leading to this rebrand.

## **Philosophy**
> "Honesty in English: Explicitness without making your brain flip"

Conduit emphasizes clarity in syntax. 

It prioritizes explicit constructs that are easy to understand. 

The goal is to minimize cognitive overhead. 

This is achieved without introducing unnecessary boilerplate.

## **Core Principles**
1. **Immutable by default** with explicit, scoped mutability control.
2. **Explicitness for clarity**, but without boilerplate or verbosity (e.g., no redundant keywords in common cases).
3. **Safety through Rust transpilation**: Borrow checker, ownership, no null by default.
4. **Flexibility**: Multiple syntax styles where they enhance readability without harming consistency.

---

## **1. Variables and Mutability**

### **Immutable by Default**
```cndt
int x = 5;              // Immutable binding
var y = 10;             // Type-inferred, immutable
```

### **Mutability Control**
```cndt
int mut x = 5;          // Explicit mutable binding
x = 10;                 // Allowed because x is mutable

mut x;                  // Transform existing binding to mutable (no assignment)
unmut x;                // Transform existing binding to immutable (no assignment)

unmut x = x + 1;        // Shadow: create new immutable binding
mut x = x * 2;          // Shadow: create new mutable binding
```

### **Rules**
- `mut` / `unmut` **without** `=` → **transforms** the mutability of the existing binding in the current scope
- `mut` / `unmut` **with** `=` → **shadows** (creates a new binding with the specified mutability)
- Any declaration with a type keyword (`int`, `string`, etc.) → always a **new binding**
- **Redundant bare toggles** (e.g., `mut x;` when x is already mutable) → **compile error**
- **Redundant toggles on shadowing** (e.g., `mut x = ...` when x is already mutable) → **warning** (allowed for explicitness)

### **Scoping Example**
```cndt
int mut x = 5;
{
    mut x;                  // Makes outer x mutable within this block
    x = 8;                  // Modifies outer x
    int x = x;              // New immutable binding shadowing outer x
    mut x;                  // Makes the inner binding mutable
    x = 12;                 // Only affects inner x
}
print(x);                   // Prints 8 (outer modified)
```

---

## **2. Ownership and Borrowing**

### **Ownership**
Values are owned by default. Moving a value transfers ownership.

### **Borrowing and References**
Borrowing requires explicit symbols. To avoid ambiguity, any reference to a lifetime must be accompanied by a borrow operator if it is not an owned type.

- `&T` → Immutable borrow (shared reference).
- `&!T` → Mutable/exclusive borrow.
- `&T^['a]` → Shared reference bound to lifetime `'a`.
- `T^['a]` → Lifetime-gated owned type (owns data, but cannot outlive `'a`).

### **Rules**
- References **must** use `&` or `&!`. The syntax `string^['a]` is interpreted as an owned type constrained by a lifetime, not a reference.

---

## **3. Lifetimes**

### **Postfix Notation**
- `^[name]` after a name → **declares** a new lifetime parameter.
- `^['name]` → **references** an existing lifetime.

```cndt
struct Tokenizer^[input] {
    &string^['input] data    // Explicitly a borrow of the input string
    int pos
}

string^[a] longest^[a](&string^[a] x, &string^[a] y) { ... }
```

### **Lifetime Relationships**
For explicit bounds (outlives relationships):
```cndt
void foo^[a b c d]() : where ^[ a > (b && c), d > a ]
```
- `a > b` means 'a outlives 'b (consistent with Rust's `'a: 'b`).
- Use `&&` for conjunctions; parentheses for grouping.
- Multi-line for clarity:
```cndt
void foo^[a b c d]() : where ^[ 
    a > (b && c),
    d > a
]
```
- Implicit where possible; compiler suggests additions if needed.

**Rationale**: `>` is concise and guessable ("greater/longer lifetime"). Avoids verbose keywords while remaining readable.

---

## **4. Type System**

### **Primitive Types**

#### **Integers**

Conduit provides both C#-style names and explicit-size aliases for all integer types.

| Type | Alias | Size | Range | Rust |
|------|-------|------|-------|------|
| `sbyte` | `int8` | 1 byte | -128 to 127 | `i8` |
| `byte` | `uint8` | 1 byte | 0 to 255 | `u8` |
| `short` | `int16` | 2 bytes | -32,768 to 32,767 | `i16` |
| `ushort` | `uint16` | 2 bytes | 0 to 65,535 | `u16` |
| `int` | `int32` | 4 bytes | -2^31 to 2^31-1 | `i32` |
| `uint` | `uint32` | 4 bytes | 0 to 2^32-1 | `u32` |
| `long` | `int64` | 8 bytes | -2^63 to 2^63-1 | `i64` |
| `ulong` | `uint64` | 8 bytes | 0 to 2^64-1 | `u64` |
| `loong` | `int128` | 16 bytes | -2^127 to 2^127-1 | `i128` |
| `uloong` | `uint128` | 16 bytes | 0 to 2^128-1 | `u128` |
| `archint` | - | Pointer | Platform-dependent | `isize` |
| `uarchint` | - | Pointer | Platform-dependent | `usize` |

**Default:** `int` is the default integer type (32-bit signed).

#### **Floating Point**

| Type | Alias | Size | Precision | Rust |
|------|-------|------|-----------|------|
| `float` | `float32` | 4 bytes | ~7 decimal digits | `f32` |
| `double` | `float64` | 8 bytes | ~15 decimal digits | `f64` |

**Default:** `float` is the default floating-point type.

#### **Other Primitives**

| Type | Size | Description | Rust |
|------|------|-------------|------|
| `char` | 4 bytes | Unicode scalar value | `char` |
| `string` | Heap | Owned UTF-8 string | `String` |
| `bool` | 1 byte | `true` or `false` | `bool` |
| `void` | 0 bytes | Unit type (no value) | `()` |

### **Examples**
```cndt
// C# style
int count = 100;
byte flag = 0xFF;
long timestamp = 1234567890123;
string name = "Alice";

// Explicit size
int32 precise_count = 100;
uint64 large_value = 18446744073709551615;
int128 massive = 170141183460469231731687303715884105727;

// Architecture-dependent
archint offset = ptr_diff(a, b);
uarchint size = array.len();

// Floating point
float pi = 3.14159;
double e = 2.718281828459045;

// Other
char initial = 'A';
bool valid = true;

void print_message(string msg) {
    #println("{}", msg);
}
```

### **Type Aliases**

Both forms are equivalent and can be used interchangeably:
```cndt
int x = 42;       // Same as int32 x = 42;
byte b = 255;     // Same as uint8 b = 255;
float f = 3.14;   // Same as float32 f = 3.14;
```

Choose the style that best fits your context:
- **C# style** (`int`, `byte`, `long`) for familiarity
- **Explicit size** (`int32`, `uint8`, `int64`) for clarity in systems code

### **Initialization**
```cndt
var v = Point{}             // Inferred from initializer
Point p = new{}             // Type from LHS
Point p = new Point{}       // Fully explicit
var v = new Point{}         // Inferred
var v = new{}               // Error – cannot infer type
```

`new` is optional syntactic sugar equivalent to `{}` (provided for C# familiarity).

### **Arrays and Slices**
```cndt
int[5] fixed = {}                    // Fixed-size array of 5 elements
int[] vec = {1, 2, 3}                 // Vector/slice – size inferred at compile time
var arr = int[]{1, 2, 3}              // Size inferred at compile time
```

All array sizes in initializers are known at compile time.

### **Tuples**
```cndt
var point = (5, 10.0)                // (int, float)
(int, string) pair = (42, "answer")
```

### **Anonymous Structs**
```cndt
var obj = struct { int x = 5; int y = 10 };  // Inline types and initializers
// OR (contextual inference where possible)
var obj = { x: 5, y: 10 }                    // Allowed if target type is known
```

The `struct` keyword is optional when the type can be reliably inferred from context. Fields combine types and initial values inline for conciseness.

### **Nullability**
```cndt
string? maybe_null = null
string never_null = "hello"

if (var value = maybe_null) {        // Safe unwrap – value is non-null here
    print(value);
}
```

---

## **5. Functions**

### **Syntax**
Return type comes first (C-family style):
```cndt
int add(int x, int y) { return x + y; }

void process(&Vector<int> data) { ... }

^[a] string longest^[a](string^[a] x, string^[a] y) { ... }
```

`function` and `static` keywords are optional and have no effect.

---

## **6. Structs and Traits**

### **Struct Definition**
```cndt
struct Parser {
    string content
    int position
}

struct Cache<T>^[data] {
    T^['data] value
}
```

### **Unified Definition with `define`**
```cndt
define Parser : Parsable, Debuggable {
    struct {
        string content
        int position
    }

    Parser(string content) {
        self.content = content
        self.position = 0
    }

    methods Parsable {
        Token next() { ... }
        bool is_done() { ... }
    }

    methods {
        void reset() { self.position = 0 }
    }
}
```

Split style is also permitted (Rust-like).

### **Trait Definition**
```cndt
trait Parsable {
    Token next()
    bool is_done()
}
```

---

## **7. Modules and Imports**

```cndt
using std.collections.HashMap as Map from Crates;  // Rust crate interop
using MyProject.Utils;                             // Native Conduit module
```

Absence of `from` imports from the native Conduit ecosystem.

---

## **8. Error Handling**

### **The `Auto` Inference Limit**
`SafetyNet<T>` (equivalent to `SafetyNet<T, Auto>`) allows the compiler to infer all possible error variants within a function body.

- **Internal Only**: `Auto` inference is strictly permitted only for `private` or `internal` functions.
- **Public Stability**: Functions exposed in public modules **must** use explicit error types (e.g., `SafetyNet<T, MyError>`) to prevent breaking changes in the public API contract.
- **Feedback**: The compiler will provide an `INFO` message on `Auto` usage and an `ERROR` if a public function attempts to use it.

### **Error Type Specification**
- `E` is typically an enum with descriptive variants (e.g., `FileError.NotFound`, `DatabaseError.NoPermission`).
- Multi-error: `SafetyNet<Data, FileError or DatabaseError>`
- Auto-inference:
  - `SafetyNet<T>` ≡ `SafetyNet<T, Auto>` (infers all possible errors in the function body)
  - Compiler feedback:
    - INFO on any `Auto` usage
    - WARN if inferred branches >5
    - ERROR if >15 ("Too many error types to infer! Use `or`, explicit enum, or `Auto_all` to override")

### **Throwing Errors**
```cndt
Caught FileError.NotFound;               // Keyword – propagates error
```

### **Propagation**
Implicit via `Caught`. Optional `?` sugar:
```cndt
let content = read_file(path)?;          // Propagates any FileError
```

### **Handling Methods**
Chainable:
- `.OnSuccess(Action<T>)`
- `.OnCaught(Action<E>)`
- `.OnCaught(Action)` (wildcard handler)

Terminal:
- `.HandleNet(Action<T>, Action<E>)`

Risky unwraps (panic on error):
- `.LetUncaught()` → panic
- `.LetUncaughtWithFallback(T)` → fallback on error
- `.OnCaught(string msg)` → panic with message (overloaded)

### **Pattern Matching**
```cndt
match (result) {
    value => { print("Got: " + value); }          // Success case (required)
    FileError.NotFound => { print("Missing"); }
    DatabaseError => { print("DB issue"); }       // Entire error domain
    _ => { print("Other error"); }
}
```

`value` or explicit `T` matches success; specific variants or whole domains match errors.

### **Example**
```cndt
enum FileError { NotFound, PermissionDenied }

SafetyNet<string, FileError> read_file(string path) {
    if (!exists(path)) { Caught FileError.NotFound; }
    return load_contents(path);
}

int main() {
    let result = read_file("data.txt")
        .OnSuccess(c => print("Content: " + c))
        .OnCaught(e => print("Failed: " + e));

    match (result) {
        value => process(value);
        FileError.NotFound => print("Create default");
        _ => Caught SysRuntimePanic.Unhandled;
    }

    return 0;
}
```

---

## **9. Enums and Sum Types**

Conduit supports three enum styles for flexibility and explicitness.

### **Standard `enum` (Mixed Variants)**
```cndt
enum Message {
    Quit,                              // Unit variant (allowed)
    Move bundles { int x, int y },     // Struct bundle
    Write bundles string,              // Single field
    ChangeColor bundles (int, int, int)
}
```
- Allows mixing unit variants (no data), tuple bundles, and struct bundles.
- Data-carrying variants require explicit `bundles`.
- Use for enums with simple tags alongside data.

### **Data-Only `enum bundles` (All-or-None, No Units)**
```cndt
enum bundles Event {
    KeyPress(char, int modifiers),
    MouseClick(int x, int y, bool double),
    WindowResize(int width, int height),
    Paste(string content)
}
```
- **Every variant must carry data** — no unit variants allowed.
- Payload syntax is **implicit** (no repeated `bundles`).
- Forms: tuple `(int, string)`, struct `{ int x, int y }`, or single field `string`.
- Use for sum types where all cases represent data (e.g., events, AST nodes).

### **SafetyNet Error Enums (Implicit Mixed)**
```cndt
enum FileError {
    NotFound,                          // Unit allowed
    PermissionDenied,
    InvalidPath(string),
    IoError { code: int, msg: string }
}
```
- Implicit payloads (no `bundles` needed).
- Mix of unit and data variants allowed.
- Reserved for error types in `SafetyNet<_, E>`.

---

## **10. Key Design Decisions**

- Immutable by default with explicit, scoped mutability control
- Borrowing via `&T` / `&!T` for clarity and relation
- Postfix lifetimes with `>` for outlives relationships, for readability
- Return type prefix (C-style)
- Optional `new` keyword for familiarity
- Multiple syntax flexibility where it doesn’t harm consistency
- Comprehensive, ergonomic error handling built on Rust’s guarantees
- Explicit enum bundles for sum types, with options to minimize boilerplate
- Full transpilation to idiomatic Rust for safety and performance

---

## **11. Unsafe**

Conduit provides an `unsafe` keyword to perform operations that the borrow checker cannot verify.

- **Raw Pointers**: Use `*T` (immutable) and `*!T` (mutable).
- **Unsafe Blocks**: Required to dereference raw pointers or call `unsafe` functions.

```cndt
unsafe void raw_copy(*int src, *!int dest, int count) { ... }

int main() {
    int[5] arr1 = {1, 2, 3, 4, 5};
    int[5] arr2 = {};
    unsafe {
        raw_copy(arr1.as_ptr(), arr2.as_ptr(), 5);
    }
}
```

---

## **12. Deterministic Cleanup (Drop)**

Conduit uses RAII for automatic cleanup. Custom cleanup logic is defined by implementing the `Drop` trait.

```cndt
define FileHandle {
    struct { int fd }

    methods Drop {
        void drop(&!self) {
            unsafe { close_external(self.fd); }
        }
    }
}
```

Variables can be manually cleaned up early using the `drop()` keyword, which moves the value and triggers its destructor.

```cndt
var buffer = BigData.load();
process(buffer);
drop(buffer); // Cannot use buffer after this line
```

---

## **13. Compiler Message Design**

Conduit prioritizes helpful, humane, and actionable compiler diagnostics. 

Messages guide developers toward fixes quickly, using clear language and visual code snippets similar to Rust's style, while avoiding blame or excessive jargon.

### **Message Levels**

1. **INFO** – Gentle, educational notes.
   - Short and optional.
   - Example:
     ```
     INFO (line 42): Public function 'read_config' uses SafetyNet<T, Auto>.
     Suggestion: Specify an explicit error type to improve API stability.
     ```

2. **WARN** – Highlights potential improvements.
   - Non-blocking, focused on best practices.
   - Example:
     ```
     WARN (line 23): Redundant 'mut' toggle.
     Variable 'count' is already mutable in this scope.
     ```

3. **ERROR** – Blocking issues with full guidance.
   - Structured with code pointers and snippets for clarity.

### **ERROR Message Structure**

Every ERROR uses a Rust-inspired layout with arrows pointing to relevant code, combined with narrative guidance:

```
ERROR: [Clear description of the problem] (line X)

WHY: [Rule explanation where applicable] 
       
   --> file.cndt:line:column
     |
line | relevant code line with the problem
     |     ^^^^^ highlight under the problematic part
     |
    = note: [short explanation of the rule]

Help:
• [Preferred fix]
• [Alternative fix]
• [Additional option if useful]
```

Multiple locations are shown with additional `--> ` lines when needed.

### **Design Principles**

- **Visual Code Pointers**: Use arrows (`-->`, `|`, `^`) to highlight exact locations and spans, like Rust.
- **Causal Narrative**: Problem → context → explanation → resolution.
- **Minimal Jargon**: Explain concepts in plain English first; use symbols second.
- **Actionable Guidance**: Always provide concrete fixes.
- **Focused Context**: Show only the most relevant code spans.
- **Consistency**: Similar errors use identical visual patterns.
- **Humane Tone**: Helpful and encouraging, never punitive.
- **Brevity for INFO/WARN**: Keep non-errors concise.

### **Examples of Common Errors**

**Borrow Conflict**
```
ERROR: Cannot borrow 'buffer' to 'edited' as mutable, because it is also borrowed to 'snapshot' as immutable (line 15)

WHY: Mutable and immutable borrow cannot coexist; all of either borrow types must end first before you can use the other. 

   --> main.cndt:15:12
    |
8   |     var snapshot = &buffer
    |                    ------- immutable borrow occurs here
...

11  |     var edited = &!buffer
    |                  ^^^^^^^^ mutable borrow occurs here
15  |     #println(snapshot)
    |           -------- immutable borrow later used here

Help:
• Move the mutable borrow after the last use of 'snapshot' (after line 11).
• Clone 'buffer' if you need both an immutable view and mutation.
• Restructure to finish using the immutable borrow earlier.

TOOL-ID: CNDT_COEXIST_BORROWTYPE_ERR
```

**Lifetime Violation**
```
ERROR: The value the function returns may go invalid before it can be passed out from the function (line 32)

WHY: You're probably attempting to return a borrowed reference that may last shorter than lifetime 'a', possibly making the variable point to invalid value. 

   --> parser.cndt:32:5
    |
25  | &string^[a] get_slice^[a](&string^[a] input) -> 
    | ----------- returned reference tied to 'a'
...
28  |     var temp = String.new();
29  |     return &temp[..]  // temporary value dropped at end of scope
    |            ^^^^^^^^ returns a reference to data owned by the current function

Help:
• Return an owned String instead of a reference.
• Accept an owned String or extend the input lifetime.
• Store the data in a struct that owns it.

TOOL-ID: CNDT_RETURNTYPE_INSUFFICIENT_LIFETIME
```

**Type Mismatch (Nullability)**
```
ERROR: Cannot assign potentially null value to non-nullable variable (line 19)

WHY: Your variable may not accept null values; This is unsafe as null reference can result in undefined behaviour.

   --> config.cndt:19:10
    |
19  |     string name = config.get("user")?
    |                   ^^^^^^^^^^^^^^^^^^^ expected 'string', found 'string?'

Help:
• Safely unwrap: if (var n = config.get("user")?) { string name = n; }
• Provide a default: string name = config.get("user")? ?? "guest";
• Allow null: string? name = config.get("user")?;

TOOL-ID: CNDT_RETURNED_NULL_ON_NON_NULLABLE
```

**Redundant Mutability Toggle**
```
ERROR (line 47): Redundant 'mut' toggle

WHY: This is to make sure that variable mutability is managed properly. 

   --> utils.cndt:47:5
    |
24  |  int mut x = 8
    |.     ^^^ 'x' is declared as mutable here 
... 
    |
47  |     mut x;
    |     ^^^ redundant – new binding is already mutable

Help:
• Remove the line. 
• Shadow instead to override: mut x = x; (Not recommended!) 

TOOL-ID: CNDT_REDUNDANT_VARMUT_TOGGLE
```

## **14. Macros**

Conduit uses `#` prefix for macro invocations.

### **Core Principle**
If a construct is a macro in Rust, it remains a macro in Conduit with `#` prefix.

### **Standard Macros**
```cndt
#print(...)         // print! - no newline
#println(...)       // println! - with newline
#format(...)        // format! - returns string
#vec[...]           // vec! - vector literal
#dbg(expr)          // dbg! - debug print with location
#panic(msg)         // panic! - unrecoverable error
#assert(cond)       // assert! - runtime check
#assert_eq(a, b)    // assert_eq! - equality check
#todo()             // todo! - placeholder
#unimplemented()    // unimplemented! - placeholder
```

### **Format Syntax**
Conduit uses Rust's format syntax:
```cndt
#println("x={}, y={}", x, y);      // Basic
#println("hex: {:x}", value);      // Hexadecimal
#println("debug: {:?}", obj);      // Debug print
#println("pretty: {:#?}", obj);    // Pretty debug
```

No C-style `printf` or C#-style `Console.Write` variants to avoid fragmentation.

### **Migration Guide**
For C developers:
- `printf("%d\n", x)` → `#println("{}", x)`
- `printf("%s", s)` → `#print("{}", s)`

For C# developers:
- `Console.WriteLine(...)` → `#println(...)`
- `Console.Write(...)` → `#print(...)`

## **15. RAII and Resource Management**

### **Defer Statement**
Execute code when the current scope exits (LIFO order if multiple defers).
```cndt
SafetyNet<(), Error> process_file(string path) {
    var file = open(path)?;
    defer close(file);  // Runs when function exits
    
    var buffer = allocate(1024);
    defer free(buffer);  // Runs before close(file)
    
    // Use file and buffer...
    return Ok(());
}
```

Defer is useful for cleanup that doesn't warrant a full `Drop` implementation.

### **Drop Trait**
For complex cleanup logic, implement the `Drop` trait.
```cndt
define FileHandle {
    struct { int fd }
    
    methods Drop {
        void drop(&!self) {
            unsafe { close_fd(self.fd); }
        }
    }
}
```

---

## **16. Attributes**

Attributes modify compiler behavior and emit warnings/errors.
```cndt
@must_use
SafetyNet<Config, Error> load_config() { ... }  // Warn if return ignored

@deprecated("Use new_api instead")
void old_api() { ... }

@inline
int fast_add(int a, int b) { return a + b; }

@packed
struct NetworkPacket {
    u8 header
    u16 payload_length
}

@derive(Debug, Clone)
struct Point { int x, int y }
```

Available attributes:
- `@must_use` - Warn if return value ignored
- `@deprecated(msg)` - Warn on usage
- `@inline`, `@noinline` - Inlining hints
- `@packed` - Remove struct padding
- `@derive(...)` - Auto-implement traits

---

## **17. Null Safety Extensions**

### **Null Coalescing**
```cndt
string name = user?.name ?? "Unknown";  // Safe navigation + default
```

### **Null Assignment**
```cndt
config ??= load_default();  // Assign only if null
```

These transpile to Rust's Option handling.

---

## **18. Advanced Pattern Matching**

### **Range Matching**
```cndt
match status_code {
    200..299 => { /* success */ }
    400..499 => { /* client error */ }
    500..599 => { /* server error */ }
    _ => { /* unknown */ }
}
```

### **Guard Clauses**
```cndt
match value {
    x if x > 100 => { /* large */ }
    x if x > 0 => { /* positive */ }
    _ => { /* other */ }
}
```

## **19. Lambdas and Closures**

### **Lambda Syntax**
Lambdas use `=>` expression syntax and can capture variables from their environment.
```cndt
// Expression-bodied
var double(int x) => x * 2;

// Block-bodied
var process(int x) => {
    var temp = x * 2;
    return temp + 1;
};

// With explicit return type
int calculate(int a, int b) => a * b + 1;

// Capturing environment
int multiplier = 10;
var scale(int x) => x * multiplier;  // Closure
```

### **Function vs Lambda**

| Feature | Function | Lambda |
|---------|----------|--------|
| Definition | `int add(int a, int b) { return a + b; }` | `var add(int a, int b) => a + b;` |
| Can capture variables | ❌ No | ✅ Yes |
| Stored as value | ❌ No | ✅ Yes |
| Inline expression | ❌ No | ✅ Yes (with `=>`) |

---

## **16. Inline Code Blocks**

### **`rust!` - Safe Rust Code**
Execute raw Rust code within Conduit. Useful for:
- Direct crate usage
- Rust-specific features
- Performance optimization
```cndt
rust! {
    use std::collections::HashMap;
    let mut map = HashMap::new();
    map.insert("key", 42);
    println!("{:?}", map);
}
```

### **`unsafe_rust!` - Unsafe Rust Code**
Execute unsafe Rust operations.
```cndt
unsafe_rust! {
    let ptr = data.as_ptr() as *mut u8;
    *ptr = 0xFF;
}
```

**Warning:** Bypasses all safety guarantees. Use only when necessary.

### **`asm!` - Inline Assembly**
Execute assembly instructions using Rust's `asm!` macro syntax.
```cndt
u64 read_timestamp() {
    u64 result;
    asm! {
        "rdtsc",
        "shl rdx, 32",
        "or rax, rdx",
        out("rax") result,
        out("rdx") _
    }
    return result;
}
```

Supports:
- Input/output constraints: `in(reg)`, `out(reg)`, `inout(reg)`
- Clobbers: `clobber("rax")`
- Options: `volatile`, `pure`, `nomem`, `nostack`

See Rust's inline assembly documentation for full syntax.

---

## **17. Comparison Table**

| Block Type | Safety | Use Case | Transpiles To |
|------------|--------|----------|---------------|
| `rust!` | Safe | Direct Rust code, crates | `{ ... }` |
| `unsafe_rust!` | Unsafe | Raw pointers, FFI | `unsafe { ... }` |
| `asm!` | Unsafe | CPU instructions | `std::arch::asm!(...)` |
