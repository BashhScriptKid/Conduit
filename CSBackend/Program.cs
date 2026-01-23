
using System.Diagnostics;
using CSBackend.Transpiler;

namespace CSBackend;

public static class IdentifierRegex
{
    public const string alphanumeric = @"[a-zA-Z0-9_]\w*";
    public const string numeric      = @"[0-9]+";
    public const string alpha        = @"[a-zA-Z]\w*";
    
    public const string @default     = alpha;
}


public static class ConduitProgram
{
    
    
    private const bool VERBOSE = true;

    internal static void Log(string message, string header = "Program")
    {
        if (VERBOSE)
        {
            Console.WriteLine($"[{header}] {message}");
        }
    }

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

        Log("Got arguments: out_type=" + outType + ", input=" + inputPath + ", output=" + outputPath + "");

        // Manipulate output path
        string outputFilename = "";

        if (outputPath != "stdout")
        {
            outputFilename = Path.GetFileName(outputPath);

            outputPath = Path.GetDirectoryName(outputPath) ?? "";

            if (string.IsNullOrEmpty(outputFilename))
                outputFilename = Path.GetFileNameWithoutExtension(inputPath);
        }

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
            if (!File.Exists(input))
                throw new FileNotFoundException($"Input file '{input}' not found.");

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
            string finalOutPath;

            if (output != "stdout")
            {
                string directory = string.IsNullOrEmpty(outputPath) ? defaultDir : outputPath;
                outputFilename += extension;

                finalOutPath = Path.Combine(directory, outputFilename);
            }
            else
            {
                finalOutPath = "stdout";
            }

            Log($"Processing: Input={input}, Type={compileType}, Output={finalOutPath}");

            Transpile(inputPath, compileType, finalOutPath);
        }

        void Transpile(string inputPath, string compileTarget, string finalOutPath)
        {
            // Handle stdout special case immediately
            if (finalOutPath == "stdout")
            {
                Log($"Transpiling {inputPath} to {compileTarget} target (stdout)");

                if (compileTarget == "core")
                {
                    using var input = new StreamReader(inputPath);
                    Preprocessor.PreprocessToCore(input, new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

                    return;
                }

                if (compileTarget == "rust" || compileTarget == "rs")
                {
                    // Preprocess to memory, then transpile to stdout
                    using var input = new StreamReader(inputPath);
                    using var coreStream = new MemoryStream();
                    using var coreWriter = new StreamWriter(coreStream);

                    Preprocessor.PreprocessToCore(input, coreWriter);
                    coreWriter.Flush();

                    coreStream.Position = 0;
                    using var coreReader = new StreamReader(coreStream);
                    Transpiler.Transpile.ToRust(coreReader, new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

                    return;
                }

                Console.WriteLine("Error: stdout output only supported for 'core' and 'rust' targets");

                return;
            }

            Log($"Transpiling {inputPath} to {compileTarget} target. Final path: {finalOutPath}");

            // 1. Preprocess to Core (Always happens)
            string corePath = Path.ChangeExtension(finalOutPath, ".ccndt");

            using (var input = new StreamReader(inputPath))
            using (var coreOut = new StreamWriter(corePath))
            {
                Preprocessor.PreprocessToCore(input, coreOut);
            } // coreOut is FLUSHED and CLOSED here automatically

            if (compileTarget == "core")
                return;

            // 2. Transpile to Rust
            string rustPath = Path.ChangeExtension(finalOutPath, ".rs");
            Log($"Generating Rust code at {rustPath}");

            using (var coreIn = new StreamReader(corePath))
            using (var rustOut = new StreamWriter(rustPath))
            { 
                Transpiler.Transpile.ToRust(coreIn, rustOut);
            }

            if (compileTarget == "rust") return;

            // 3. Compile Binary (via CLI)
            if (compileTarget == "binary")
            {
                Log("Starting binary compilation");

                // Check rustc is installed and is a valid version
                Console.Write("Checking for rustc... ");

                using var processChk = System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "rustc",
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });

                processChk?.WaitForExit();

                if (processChk == null || processChk.ExitCode != 0)
                {
                    Console.WriteLine("FAILED. rustc not found in PATH.");

                    return;
                }

                string version = processChk.StandardOutput.ReadToEnd()
                    .Trim();

                Console.WriteLine($"OK ({version})");

                // Compile time, yay
                Console.WriteLine($"[Compiling] {rustPath} -> {finalOutPath}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "rustc",

                    // -o specifies the output binary name
                    Arguments = $"\"{rustPath}\" -o \"{finalOutPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);

                if (process == null)
                    return;

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string errors = process.StandardError.ReadToEnd();
                    Log("Compilation FAILED");
                    Console.WriteLine("Compilation Error:");
                    Console.WriteLine(errors);
                }
                else
                {
                    Log("Compilation SUCCESS");
                    Console.WriteLine("Successfully compiled to binary.");
                }
            }
        }

    }
}