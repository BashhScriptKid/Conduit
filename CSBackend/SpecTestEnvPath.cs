namespace CSBackend;


// For prototyping environment only
public static class SpecTestEnvPath
{
    // Now 'Root' is an object that acts like a string
    public static PathWrapper Root { get; } = new(Path.GetFullPath("./Spec_test"));

    public class PathWrapper(string path)
    {
        // ReSharper disable once InconsistentNaming
        private readonly string _path = path;

        // The "extensions" move inside this wrapper
        public string CoreRef => Path.Combine(_path, "ccndt_out");
        public string Core => Path.Combine(_path, "ccndt_gen");
        public string RustRef => Path.Combine(_path, "rs_out");
        public string Rust => Path.Combine(_path, "rs_gen");
        public string Binary => Path.Combine(_path, "bin_gen");
        public string ConduitRef => Path.Combine(_path, "cndt_in");

        // The magic: implicit conversion to string
        public static implicit operator string(PathWrapper pw) => pw._path;
        public override                 string ToString()      => _path;
    }
}