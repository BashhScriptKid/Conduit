fn sum_to_n(n: i32) -> i32 {
    let mut sum: i32 = 0;
    let mut i: i32 = 1;

    while i <= n {
        sum = sum + i;
        i = i + 1;
    }

    return sum;
}

fn main() -> i32 {
    let result: i32 = sum_to_n(10);
    println!("Sum 1 to 10: {}", result);
    return 0;
}
