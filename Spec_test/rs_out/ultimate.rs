static mut GLOBAL_COUNTER: i32 = 0;

pub fn add(a: i32, b: i32) -> i32 {
    return a + b;
}

pub fn subtract(a: i32, b: i32) -> i32 {
    return a - b;
}

pub fn multiply(a: i32, b: i32) -> i32 {
    return a * b;
}

pub fn divide(a: i32, b: i32) -> i32 {
    if b == 0 {
        return 0;
    }
    return a / b;
}

pub fn absolute(x: i32) -> i32 {
    if x < 0 {
        return -x;
    } else {
        return x;
    }
}

pub fn max(a: i32, b: i32) -> i32 {
    if a > b {
        return a;
    }
    return b;
}

pub fn sum_to_n(n: i32) -> i32 {
    let mut sum: i32 = 0;
    let mut i: i32 = 1;

    while i <= n {
        sum = sum + i;
        i = i + 1;
    }

    return sum;
}

pub fn factorial_iterative(n: i32) -> i32 {
    let mut result: i32 = 1;

    for i in 1..=n {
        result = result * i;
    }

    return result;
}

pub fn factorial_recursive(n: i32) -> i32 {
    if n <= 1 {
        return 1;
    }
    return n * factorial_recursive(n - 1);
}

pub fn fibonacci(n: i32) -> i32 {
    if n <= 1 {
        return n;
    }
    return fibonacci(n - 1) + fibonacci(n - 2);
}

pub fn increment_global() {
    unsafe {
        GLOBAL_COUNTER = GLOBAL_COUNTER + 1;
    }
}

pub fn get_global() -> i32 {
    unsafe {
        return GLOBAL_COUNTER;
    }
}

pub fn array_sum(arr: &[i32], length: i32) -> i32 {
    let mut sum: i32 = 0;
    let mut i: i32 = 0;

    while i < length {
        sum = sum + arr[i as usize];
        i = i + 1;
    }

    return sum;
}

pub fn array_fill(arr: &mut [i32], length: i32, value: i32) {
    let mut i: i32 = 0;

    while i < length {
        arr[i as usize] = value;
        i = i + 1;
    }
}

#[derive(Copy, Clone)]
struct Stack {
    data: [i32; 256],
    top: i32,
}

pub fn stack_new() -> Stack {
    return Stack {
        data: [0; 256],
        top: 0,
    };
}

pub fn stack_push(stack: &mut Stack, value: i32) {
    if stack.top < 256 {
        stack.data[stack.top as usize] = value;
        stack.top = stack.top + 1;
    }
}

pub fn stack_pop(stack: &mut Stack) -> i32 {
    if stack.top > 0 {
        stack.top = stack.top - 1;
        return stack.data[stack.top as usize];
    }
    return 0;
}

pub fn stack_is_empty(stack: &Stack) -> bool {
    return stack.top == 0;
}

pub fn collatz_steps(n: i32) -> i32 {
    let mut steps: i32 = 0;
    let mut current: i32 = n;

    while current != 1 {
        if current % 2 == 0 {
            current = current / 2;
        } else {
            current = current * 3 + 1;
        }
        steps = steps + 1;

        if steps > 10000 {
            return -1;
        }
    }

    return steps;
}

#[derive(Copy, Clone, PartialEq)]
enum TapeSymbol {
    Zero,
    One,
}

#[derive(Copy, Clone, PartialEq)]
enum State {
    A,
    B,
    Halt,
}

#[derive(Copy, Clone)]
struct TuringMachine {
    tape: [TapeSymbol; 100],
    head: i32,
    state: State,
    steps: i32,
}

pub fn tm_new() -> TuringMachine {
    let tm = TuringMachine {
        tape: [TapeSymbol::Zero; 100],
        head: 50,
        state: State::A,
        steps: 0,
    };
    return tm;
}

pub fn tm_step(tm: &mut TuringMachine) {
    match tm.state {
        State::Halt => {
            return;
        }
        _ => {}
    }

    let current_symbol = tm.tape[tm.head as usize];

    match (tm.state, current_symbol) {
        (State::A, TapeSymbol::Zero) => {
            tm.tape[tm.head as usize] = TapeSymbol::One;
            tm.head = tm.head + 1;
            tm.state = State::B;
        }
        (State::A, TapeSymbol::One) => {
            tm.tape[tm.head as usize] = TapeSymbol::Zero;
            tm.head = tm.head - 1;
            tm.state = State::B;
        }
        (State::B, TapeSymbol::Zero) => {
            tm.tape[tm.head as usize] = TapeSymbol::One;
            tm.head = tm.head - 1;
            tm.state = State::A;
        }
        (State::B, TapeSymbol::One) => {
            tm.tape[tm.head as usize] = TapeSymbol::One;
            tm.head = tm.head + 1;
            tm.state = State::Halt;
        }
        _ => {}
    }

    tm.steps = tm.steps + 1;
}

pub fn tm_run(tm: &mut TuringMachine, max_steps: i32) -> i32 {
    let mut i: i32 = 0;

    while i < max_steps {
        match tm.state {
            State::Halt => {
                break;
            }
            _ => {}
        }

        tm_step(tm);
        i = i + 1;
    }

    return tm.steps;
}

pub fn main() -> i32 {
    println!("=== TURING COMPLETENESS TESTS ===\n");

    println!("1. Arithmetic:");
    println!("   5 + 3 = {}", add(5, 3));
    println!("   10 - 4 = {}", subtract(10, 4));
    println!("   6 * 7 = {}", multiply(6, 7));
    println!("   20 / 4 = {}\n", divide(20, 4));

    println!("2. Conditionals:");
    println!("   abs(-42) = {}", absolute(-42));
    println!("   max(15, 23) = {}\n", max(15, 23));

    println!("3. Loops:");
    println!("   sum(1..10) = {}", sum_to_n(10));
    println!("   factorial(5) = {}\n", factorial_iterative(5));

    println!("4. Recursion:");
    println!("   factorial_recursive(6) = {}", factorial_recursive(6));
    println!("   fibonacci(10) = {}\n", fibonacci(10));

    println!("5. State Mutation:");
    println!("   global_counter = {}", get_global());
    increment_global();
    increment_global();
    increment_global();
    println!("   after 3 increments = {}\n", get_global());

    println!("6. Arrays:");
    let mut numbers: [i32; 5] = [1, 2, 3, 4, 5];
    println!("   array sum = {}", array_sum(&numbers, 5));
    array_fill(&mut numbers, 5, 42);
    println!("   after fill(42) = {}\n", array_sum(&numbers, 5));

    println!("7. Stack Machine:");
    let mut stack = stack_new();
    stack_push(&mut stack, 10);
    stack_push(&mut stack, 20);
    stack_push(&mut stack, 30);
    println!("   pop = {}", stack_pop(&mut stack));
    println!("   pop = {}", stack_pop(&mut stack));
    println!("   pop = {}\n", stack_pop(&mut stack));

    println!("8. Collatz Conjecture:");
    println!("   collatz(27) takes {} steps\n", collatz_steps(27));

    println!("9. Turing Machine (2-state Busy Beaver):");
    let mut tm = tm_new();
    let steps = tm_run(&mut tm, 100);
    println!("   Halted after {} steps\n", steps);

    println!("=== ALL TESTS COMPLETE ===");

    return 0;
}
