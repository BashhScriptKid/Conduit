
namespace CSBackend
{

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
            public override string ToString() => _path;
        }
    }

public static class ConduitProgram
    {
        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: CSBackend <out_type> <input> <output (optional)>");
                Console.WriteLine("Options for out_type: core, rs/rust, binary/bin");

                return;
            }
            

            string outType = args[0]
                .ToLower();

            string inputPath = args[1];

            string outputPath = String.Empty;
            if (args.Length >= 3)
                outputPath = args[2];
            
            // Manipulate output path
            string outputFilename = Path.GetFileName(outputPath);
            outputPath = Path.GetDirectoryName(outputPath) ?? "";
            
            if (string.IsNullOrEmpty(outputFilename))
                outputFilename = Path.GetFileNameWithoutExtension(inputPath);

            try
            {
                Process(outType, inputPath, outputPath);
            }
            catch (NotImplementedException ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            void Process(string compileType, string input, string output)
            {
                (string defaultDir, string extension, string? label) = compileType switch
                {
                    "core"            => (SpecTestEnvPath.Root.Core, ".ccndt", "Core"),
                    "rs" or "rust"    => (SpecTestEnvPath.Root.Rust, ".rs", "Rust"),
                    "binary" or "bin" => (SpecTestEnvPath.Root.Binary, ".bin", "Binary"),
                    _                 => (SpecTestEnvPath.Root, String.Empty, null)
                };
                
                if (label == null)
                {
                    Console.WriteLine($"Error: Invalid out_type '{compileType}'.");
                    Console.WriteLine("Supported types: core, rs/rust, binary/bin");
                    return;
                }

                // 3. Execute the common path logic
                string directory = string.IsNullOrEmpty(outputPath) ? defaultDir : outputPath;
                outputFilename += extension;
                    
                string finalOutPath = Path.Combine(directory, outputFilename);

                throw new NotImplementedException($"{label} output generation is not implemented, but it will be saved as: {finalOutPath}");
            }
        }
    }
}