struct Point {
    x: i32,
    y: i32,
}

fn main() -> i32 {
    let p: Point = Point { x: 10, y: 20 };
    println!("Point: ({}, {})", p.x, p.y);
    return 0;
}
