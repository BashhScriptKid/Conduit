
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
    private const string Stdout = "stdout";
    private const bool EmitNewlinesInLexOutput = true;

    internal static void Log(string message, string header = "Program")
    {
        if (VERBOSE)
        {
            Console.WriteLine($"[{header}] {message}");
        }
    }

    private static void Main(string[] args)
    {
        if (!TryParseArgs(args, out var outType, out var inputPath, out var outputPath))
            return;

        Log($"Got arguments: out_type={outType}, input={inputPath}, output={outputPath}");

        try
        {
            Process(outType, inputPath, outputPath);
        }
        catch (NotImplementedException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static bool TryParseArgs(string[] args, out string outType, out string inputPath, out string outputPath)
    {
        outType = string.Empty;
        inputPath = string.Empty;
        outputPath = string.Empty;

        if (args.Length < 2)
        {
            Console.WriteLine("Usage: CSBackend <out_type> <input> <output (optional)>");
            Console.WriteLine("Options for out_type: core, rs/rust, binary/bin, lex");
            return false;
        }

        outType = args[0].ToLower();
        inputPath = args[1];
        outputPath = args.Length >= 3 ? args[2] : string.Empty;
        return true;
    }

    private static void Process(string compileType, string input, string output)
    {
        if (!File.Exists(input))
            throw new FileNotFoundException($"Input file '{input}' not found.");

        (string defaultDir, string extension, string? label) = compileType switch
        {
            "core"            => (SpecTestEnvPath.Root.Core, ".ccndt", "Core"),
            "rs" or "rust"    => (SpecTestEnvPath.Root.Rust, ".rs", "Rust"),
            "binary" or "bin" => (SpecTestEnvPath.Root.Binary, ".bin", "Binary"),
            "lex"             => (SpecTestEnvPath.Root.Lex, ".lex", "Lex"),
            _                 => (SpecTestEnvPath.Root, string.Empty, null)
        };

        if (label == null)
        {
            Console.WriteLine($"Error: Invalid out_type '{compileType}'.");
            Console.WriteLine("Supported types: core, rs/rust, binary/bin, lex");
            return;
        }

        string finalOutPath = ResolveOutputPath(output, input, defaultDir, extension);
        Log($"Processing: Input={input}, Type={compileType}, Output={finalOutPath}");

        Transpile(input, compileType, finalOutPath);
    }

    private static string ResolveOutputPath(string outputArg, string inputPath, string defaultDir, string extension)
    {
        if (outputArg == Stdout)
            return Stdout;

        string outputFilename = Path.GetFileName(outputArg);
        string outputDir = Path.GetDirectoryName(outputArg) ?? string.Empty;

        if (string.IsNullOrEmpty(outputFilename))
            outputFilename = Path.GetFileNameWithoutExtension(inputPath);

        string directory = string.IsNullOrEmpty(outputDir) ? defaultDir : outputDir;
        return Path.Combine(directory, outputFilename + extension);
    }

    private static void Transpile(string inPath, string compileTarget, string outPath)
    {
        // Handle stdout special case immediately
        if (outPath == Stdout)
        {
            StreamWriter WriteToStdout(bool autoFlush = true) => 
                new(Console.OpenStandardOutput()) { AutoFlush = autoFlush };
            
            Log($"Transpiling {inPath} to {compileTarget} target (stdout)");
            
            if (compileTarget == "core")
            {
                using var input = new StreamReader(inPath);
                Preprocessor.PreprocessToCore(input, WriteToStdout());
                return;
            }

            if (compileTarget == "lex")
            {
                using var input = new StreamReader(inPath);
                using var coreStream = new MemoryStream();
                using var coreWriter = new StreamWriter(coreStream);

                Preprocessor.PreprocessToCore(input, coreWriter);
                coreWriter.Flush();

                coreStream.Position = 0;
                using var coreReader = new StreamReader(coreStream);
                WriteLexedTokens(coreReader, WriteToStdout());
                return;
            }

            if (compileTarget == "rust" || compileTarget == "rs")
            {
                // Preprocess to memory, then transpile to stdout
                using var input = new StreamReader(inPath);
                using var coreStream = new MemoryStream();
                using var coreWriter = new StreamWriter(coreStream);

                Preprocessor.PreprocessToCore(input, coreWriter);
                coreWriter.Flush();

                coreStream.Position = 0;
                using var coreReader = new StreamReader(coreStream);
                Transpiler.Transpile.ToRust(coreReader, WriteToStdout());
                return;
            }

            Console.WriteLine("Error: stdout output only supported for 'core', 'rust', and 'lex' targets");
            return;
        }

        Log($"Transpiling {inPath} to {compileTarget} target. Final path: {outPath}");

        // 1. Preprocess to Core (Always happens)
        string corePath = Path.ChangeExtension(outPath, ".ccndt");

        using (var input = new StreamReader(inPath))
        using (var coreOut = new StreamWriter(corePath))
        {
            Preprocessor.PreprocessToCore(input, coreOut);
        } // coreOut is FLUSHED and CLOSED here automatically

        if (compileTarget == "core")
            return;
        
        // 2. Lex tokens (Always happens)
        string lexPath = Path.ChangeExtension(outPath, ".lex");
        if (compileTarget == "lex")
        {
            using var input = new StreamReader(corePath);
            using var lexOut = new StreamWriter(lexPath);
            WriteLexedTokens(input, lexOut);
            return;
        }

        // 3. Transpile to Rust
        string rustPath = Path.ChangeExtension(outPath, ".rs");
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

            string version = processChk.StandardOutput.ReadToEnd().Trim();
            Console.WriteLine($"OK ({version})");

            // Compile time, yay
            Console.WriteLine($"[Compiling] {rustPath} -> {outPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = "rustc",
                Arguments = $"\"{rustPath}\" -o \"{outPath}\"",
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
    
    private static List<Tokens.Token> WriteLexedTokens(StreamReader @in)
    {
        string coreSource = @in.ReadToEnd();
        return new Lexer(coreSource).LexAll();
    }

    private static void WriteLexedTokens(StreamReader @in, StreamWriter @out)
    {
        // Assumes input is already in core IR form.
        string coreSource = @in.ReadToEnd();

        var lexer = new Lexer(coreSource);
        List<Tokens.Token> tokens = lexer.LexAll();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (token.TokenType == Tokens.Type.Newline) 
                continue;

            string lexeme = EscapeLexeme(token.Lexeme);
            string lexstring = ($"{token.Line}\t{token.TokenType}\t\"{lexeme}\"");

            bool nextIsNewline = EmitNewlinesInLexOutput
                                 && i + 1 < tokens.Count // Prevent out of bounds
                                 && tokens[i + 1].TokenType == Tokens.Type.Newline;

            @out.WriteLine(nextIsNewline ? $"{lexstring}\t ;" : lexstring);
        }
    }

    private static string EscapeLexeme(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }
}
