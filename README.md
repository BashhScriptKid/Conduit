# Conduit Programming Language

> **"Honesty in English: Explicitness without making your brain flip"**

Conduit is a modern systems programming language that combines Rust's memory safety with C#'s ergonomic syntax and C's low-level control. It transpiles to Rust, providing safety guarantees while maintaining readability and developer productivity.

**File Extension:** `.cndt`

---

## Quick Start

### Hello World

```cndt
int main() {
    #println("Hello, Conduit!");
    return 0;
}
```

### Fibonacci Generator

```cndt
int main() {
    (u128, u128) mut x = (0, 1);
    Vector<u128> mut list = Vector<u128>.new();

    list.push(x.0);
    list.push(x.1);

    while (true) {
        var (t, overflow) = x.1.overflowing_add(x.0);

        if (overflow) {
            #println("Sequence complete: {} numbers", list.len());
            break;
        }

        list.push(t);
        x.0 = x.1;
        x.1 = t;
    }

    #println("{:?}", list);
    return 0;
}
```

---

## Features

### Memory Safety
- **Borrow checker** - Rust's proven ownership system
- **No null by default** - Use `string?` for nullable types
- **Lifetime tracking** - Compile-time memory management

### Familiar Syntax
- **C-family style** - Return types first, familiar operators
- **Clear mutability** - `mut`/`unmut` instead of confusing shadowing
- **Explicit borrowing** - `&T` (immutable), `&!T` (mutable)
- **Intuitive lifetimes** - `^[a]` postfix notation

### Modern Ergonomics
- **Type inference** - `var x = 5` when types are obvious
- **Pattern matching** - Powerful `match` expressions
- **Error handling** - `SafetyNet<T, E>` with `Caught` keyword
- **C# visibility** - `public`, `internal`, `private`

### Zero-Cost Abstractions
- **Transpiles to Rust** - Inherits Rust's performance
- **No runtime overhead** - All safety checks at compile-time
- **Direct hardware access** - `unsafe_rust!` escape hatch

---

## Language Overview

### Variables & Mutability

```cndt
// Immutable by default
int x = 5;
var y = 10;  // Type inferred

// Explicit mutability
int mut counter = 0;
counter = counter + 1;

// Transform mutability
mut counter;      // Make mutable (no assignment)
unmut counter;    // Make immutable
```

### Ownership & Borrowing

```cndt
// Ownership (default)
Vector<int> data = Vector<int>{1, 2, 3};

// Immutable borrow
void read_data(&Vector<int> data) {
    #println("Length: {}", data.len());
}

// Mutable borrow
void modify_data(&!Vector<int> data) {
    data.push(4);
}
```

### Lifetimes

```cndt
// Lifetime declaration (postfix)
struct Parser^[input] {
    string content
}

// Lifetime in functions
string^[a] longest^[a](string^[a] x, string^[a] y) {
    if (x.len() > y.len()) { x } else { y }
}

// Borrow existing lifetime
string get_data^['input](&Parser^[input] parser) {
    parser.content
}
```

### Error Handling

```cndt
enum error FileError {
    NotFound,
    PermissionDenied,
    InvalidPath(string)
}

SafetyNet<string, FileError> read_file(string path) {
    if (!exists(path)) {
        Caught FileError.NotFound;
    }
    return load_contents(path);
}

// Pattern matching
match (result) {
    content => { #println("Success: {}", content); }
    FileError.NotFound => { #println("File missing"); }
    _ => { #println("Other error"); }
}

// Method chaining
result
    .OnSuccess((content) => process(content))
    .OnCaught((err) => log_error(err));
```

### Structs & Traits

```cndt
struct Point {
    int x
    int y
}

trait Drawable {
    void draw()
}

define Circle : Drawable {
    struct {
        Point center
        int radius
    }
    
    Circle(Point center, int radius) {
        self.center = center
        self.radius = radius
    }
    
    methods Drawable {
        void draw() {
            #println("Circle at ({}, {})", self.center.x, self.center.y);
        }
    }
    
    methods {
        int area() {
            return 3.14 * self.radius * self.radius;
        }
    }
}
```

### Enums

```cndt
// Standard enum (mixed variants)
enum Message {
    Quit,
    Move bundles { int x, int y },
    Write bundles string
}

// All-data enum
enum bundles Event {
    KeyPress(char, int),
    MouseClick(int x, int y)
}

// Pattern matching
match (msg) {
    Message.Quit => { exit(0); }
    Message.Move { x, y } => { move_to(x, y); }
    Message.Write(text) => { print(text); }
}
```

### Lambdas

```cndt
// Expression-bodied
var double(int x) => x * 2;

// Block-bodied
var process(int x) => {
    var temp = x * 2;
    return temp + 1;
};

// Capturing environment (primitives auto-copy)
int factor = 10;
var scale(int x) => x * factor;

// Non-Copy types require 'move'
Vector<int> data = #vec[1, 2, 3];
var process(int x) => move x + data[0];
```

### Macros

```cndt
// Standard macros (# prefix)
#println("Hello, {}!", name);
#dbg(variable);
#vec[1, 2, 3];
#panic("Critical error!");
```

### Visibility

```cndt
public struct User {
    public string username
    internal int user_id
    private string password_hash
}

public void api_function() { }
internal void helper() { }
private void secret() { }
```

---

## Installation

Available soon.

<!--
```bash
# Clone repository
git clone https://github.com/yourusername/conduit.git
cd conduit

# Build transpiler
make

# Install (optional)
sudo make install
```
-->

---

## Usage

### Compile & Run

Available soon.

<!--
```bash
# Transpile to Rust
conduit hello.cndt -o hello.rs

# Compile with rustc
rustc hello.rs -o hello

# Run
./hello
```

### One-Step Compilation

```bash
# Transpile + compile + run
conduit run program.cndt
```
-->

---

## Project Structure

```
conduit/
├── CSBackend # Suite written in C# for quick prototype
├── RSBackend # Suite written in Rust for more advanced UNICODE_VERSION
│
├── Spec_test/       # Test suite
│   ├── cndt_in/     # Test inputs
│   ├── rs_out/      # Expected outputs
│   └── rs_gen/      # Generated outputs
│
├── ConduitLang_Specification.md  # Specification file
└── README.md                     # Obvious.
```

---

## Testing

### Test Files

- `helloworld.cndt` - Basic printing
- `minimal.cndt` - Variables, arithmetic
- `functions.cndt` - Function calls
- `conditional.cndt` - if/else branching
- `loops.cndt` - Loops, mutability
- `structs.cndt` - Data structures
- `fibonacci.cndt` - Complex example
- `ultimate.cndt` - Turing-complete test suite

---

## Philosophy & Design Goals

### Honesty in English
Code should read like human language. Avoid symbolic soup and cryptic syntax like an FP purist.

**Example - Rust vs Conduit:**

```rust
// Rust
fn longest<'a>(x: &'a str, y: &'a str) -> &'a str

// Conduit
string^[a] longest^[a](string^[a] x, string^[a] y)
```

### Explicitness Without Cognitive Overhead
Make intent clear, but don't require mental gymnastics.

**Example - Mutability:**

```rust
// Rust (confusing shadowing)
let x = 5;
let x = x + 1;  // New binding or mutation?
let mut x = x;  // Now mutable

// Conduit (clear intent)
int x = 5;
unmut x = x + 1;  // Transform immutable while assigning values
mut x;            // Transform to mutable
```

### Safety Through Transpilation
Leverage Rust's battle-tested borrow checker without its syntax complexity.

---

## Language Specification

For the complete language specification, see [the specification](./ConduitLang_Specification.md).

Key sections:
- Variables & Mutability
- Ownership & Borrowing
- Lifetimes
- Type System
- Functions & Lambdas
- Structs & Traits
- Enums & Pattern Matching
- Error Handling (SafetyNet)
- Macros
- Visibility & Access Control

---

## Roadmap

- [x] Core syntax design
- [x] Language specification
- [ ] Lexer implementation
- [ ] Parser (AST generation)
- [ ] Basic transpiler (minimal features)
- [ ] More stuff idk

---

## Contributing

### Areas We Need Help
- **Critics to spefification** - Any proposes to cover more of Rust's feature set, or report a flaw within the existing specification syntax
- **Parser development** - Implementing language features
- **Standard library** - Wrapping Rust stdlib with Conduit-friendly APIs
- **Documentation** - Tutorials, examples, guides
- **Testing** - More test cases, edge cases
- **Tooling** - IDE extensions, syntax highlighting, formatters

---

## FAQ

### Why create Conduit when Rust exists?

Rust's borrow checker is brilliant, but its syntax is a barrier. Conduit provides the same safety guarantees with familiar C-family syntax, making systems programming accessible to more developers.

### Does Conduit add runtime overhead?

No. Conduit transpiles to idiomatic Rust, which compiles to native machine code with zero runtime overhead.

The only overhead is compile time. Users won't notice, but *you* will

### Can I use Rust crates?

TBD, as new syntaxes may conflict with Crates API provided, that expects native Rust syntax

### Is Conduit ready for production?

Not yet. Conduit is in early development (Haven't even written a working suite yet).

### How can I help?

Feel free to email me (from profile) or make an issue!

---

## License

MIT License - See [LICENSE](./LICENSE) for details.

---

## Contact

- **Repository:** https://github.com/BashhScriptKid/Conduit
- **Issues:** https://github.com/BashhScriptKid/Conduit/issues
- **Discussions:** https://github.com/BashhScriptKid/Conduit/discussions

---

## Acknowledgments

Conduit's design is derived from:
- **Rust** - For the borrow checker and safety guarantees
- **C#** - For syntax inspiration and developer ergonomics
- **C** - For direct hardware control philosophy
- **Zen-C** - For transpiler architecture inspiration
