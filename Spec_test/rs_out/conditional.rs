fn max(a: i32, b: i32) -> i32 {
    if a > b {
        return a;
    } else {
        return b;
    }
}

fn main() -> i32 {
    let bigger: i32 = max(42, 17);
    println!("Max: {}", bigger);
    return 0;
}
