fn add(a: i32, b: i32) -> i32 {
    return a + b;
}

fn main() -> i32 {
    let result: i32 = add(5, 10);
    println!("5 + 10 = {}", result);
    return 0;
}
