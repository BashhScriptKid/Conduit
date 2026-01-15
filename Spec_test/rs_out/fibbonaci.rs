fn main() {
    let mut x: (u128, u128) = (0, 1);

    let mut list: Vec<u128> = Vec::new();

    list.push(x.0);
    list.push(x.1);

    loop {
        let (t, overflow) = x.1.overflowing_add(x.0);

        if overflow {
            println!(
                "Stopping execution, hitting limit! Sequence generated: {}",
                list.len()
            );
            break;
        }

        list.push(t);

        x.0 = x.1;
        x.1 = t;
    }

    const useNewline: bool = false;

    if useNewline {
        println!("{:#?}", list);
    } else {
        println!("{:?}", list)
    }
}
