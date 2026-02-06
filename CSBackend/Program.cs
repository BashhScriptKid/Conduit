using System.Diagnostics;
using CSBackend.Transpiler;
using YamlDotNet.Serialization;

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
    private const bool Verbose = true;
    private const string Stdout = "stdout";
    private const bool EmitNewlinesInLexOutput = true;
    private static readonly ISerializer AstSerializer = new SerializerBuilder().Build();

    internal static void Log(string message, string header = "Program")
    {
        if (Verbose)
        {
            Console.WriteLine($"[{header}] {message}");
        }
    }

    
    private static MemoryStream ProcessSource(MemoryStream @in, Action<StreamReader, StreamWriter> func)
    {
        void Log(string message) => ConduitProgram.Log(message, "SourceProcessor");
        MemoryStream sourceBuffer = new();
        MemoryStream targetBuffer = new();

        // 1. Initial Pass: Copy raw input to sourceBuffer
        @in.CopyTo(sourceBuffer);

        Log($"Running pass: {func.Method.Name}");
        sourceBuffer.Position = 0;
        targetBuffer.SetLength(0);
    
        // We use 'using' here to ensure the Writer is FLUSHED before we copy
        using (var reader = new StreamReader(sourceBuffer, leaveOpen: false))
        using (var writer = new StreamWriter(targetBuffer, leaveOpen: true))
        {
            func(reader, writer);
            writer.Flush(); // Crucial: push remaining bits to targetBuffer
        }
        
        Log($"Pass {func.Method.Name} completed. Buffer size: {sourceBuffer.Length} bytes");
            
        return targetBuffer;
    }

    private static void WriteTo(string outPath, MemoryStream buffer)
    {
        if (outPath == "stdout")
            buffer.WriteTo(Console.OpenStandardOutput());
        else
        {
            using FileStream fileStream = File.Create(outPath);
            buffer.CopyTo(fileStream);
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
            Console.WriteLine("Options for out_type: core, rs/rust, binary/bin, lex, ast");
            return false;
        }

        outType = args[0].ToLower();
        inputPath = args[1];
        outputPath = args.Length >= 3 ? args[2] : string.Empty;
        return true;
    }
    
    private static void Process(string compileType, string input, string output)
    {
        void Log(string message) => ConduitProgram.Log(message, "Main.Process")
        ;
        if (!File.Exists(input))
            throw new FileNotFoundException($"Input file '{input}' not found.");

        (string defaultDir, string extension, string? label) = compileType switch
        {
            "lex"             => (SpecTestEnvPath.Root.Lex,    ".lex",   "Lex"),
            "ast"             => (SpecTestEnvPath.Root.AST,    ".ast",   "AST"),
            "core"            => (SpecTestEnvPath.Root.Core,   ".ccndt", "Core"),
            "rs" or "rust"    => (SpecTestEnvPath.Root.Rust,   ".rs",    "Rust"),
            "binary" or "bin" => (SpecTestEnvPath.Root.Binary, ".bin",   "Binary"),
            _                 => (SpecTestEnvPath.Root,        "",        null)
        };

        if (label == null)
        {
            Console.WriteLine($"Error: Invalid out_type '{compileType}'.");
            Console.WriteLine("Supported types: core, rs/rust, binary/bin, lex, ast");
            return;
        }

        string finalOutPath = ResolveOutputPath(output, input, defaultDir, extension);
        Log($"Processing: Input={input}, Type={compileType}, Output={finalOutPath}");

        Transpile(input, compileType, finalOutPath);
        
        string ResolveOutputPath(string outputArg, string inputPath, string defaultDir, string extension)
        {
            const string stdout = "stdout";
            if (outputArg == stdout)
                return stdout;

            string outputFilename = Path.GetFileName(outputArg);
            string outputDir = Path.GetDirectoryName(outputArg) ?? string.Empty;

            if (string.IsNullOrEmpty(outputFilename))
                outputFilename = Path.GetFileNameWithoutExtension(inputPath);

            string directory = string.IsNullOrEmpty(outputDir) ? defaultDir : outputDir;
            return Path.Combine(directory, outputFilename + extension);
        }
    }

    private static void Transpile(string inPath, string compileType, string outPath)
    {
        using MemoryStream sourcebuffer = new MemoryStream();
        MemoryStream? finalbuffer;

        using (var file = new FileStream(inPath, FileMode.Open))
        {
            file.CopyTo(sourcebuffer);
        }
        
        MemoryStream lexedbuffer = ProcessSource(sourcebuffer, Preprocessor.PreprocessToCore);

        if (compileType == "lex")
        {
            finalbuffer = lexedbuffer;
            goto writefile;
        }

        // AST isn't implemented yet
        MemoryStream tokenbuffer = ProcessSource(lexedbuffer, (_, _) => throw new NotImplementedException());

        if (compileType == "ast")
        {
            finalbuffer = tokenbuffer;
            goto writefile;
        }

        // TODO: Repurpose ccndt preprocessor as optimised IR instead
        MemoryStream corebuffer = ProcessSource(tokenbuffer, Preprocessor.PreprocessToCore);

        if (compileType == "core")
        {
            finalbuffer = corebuffer;
            goto writefile;
        }

        MemoryStream rustbuffer = ProcessSource(corebuffer, Transpiler.Transpile.ToRust);

        if (compileType is "rs" or "rust")
        {
            finalbuffer = rustbuffer;
            goto writefile;
        }

        if (compileType is "binary" or "bin")
        {
            Compile(rustbuffer, outPath);

            return;
        }
        
        // You shouldn't reach here.
        Console.Beep();
        throw new Exception($"[{DateTime.UtcNow}] Illegal operation executed, emitting Cobalt-60 Alpha particle. DROP THIS DEVICE AND RUN.");
        
        writefile: 
        WriteTo(outPath, finalbuffer);
    }

    private static void Compile(MemoryStream rustbuffer, string outPath)
    {
        void Log(string message) => ConduitProgram.Log(message, "Compile");
        Log("Starting binary compilation");

        // Check rustc
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

        // Write Rust code to temp file
        string tempRustFile = Path.Combine(Path.GetTempPath(), $"conduit_temp_{Guid.NewGuid()}.rs");
        
        try
        {
            Log($"Writing Rust code to temp file: {tempRustFile}");
            rustbuffer.Position = 0;
            
            using (var fs = File.Create(tempRustFile))
            {
                rustbuffer.CopyTo(fs);
            }

            // Compile temp file
            Console.WriteLine($"[Compiling] {tempRustFile} -> {outPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = "rustc",
                Arguments = $"\"{tempRustFile}\" -o \"{outPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                Log("Failed to start rustc process");
                return;
            }

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
                Console.WriteLine($"Successfully compiled to: {outPath}");
            }
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempRustFile))
            {
                try
                {
                    File.Delete(tempRustFile);
                    Log($"Cleaned up temp file: {tempRustFile}");
                }
                catch (Exception ex)
                {
                    Log($"Warning: Could not delete temp file: {ex.Message}");
                }
            }
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
    
    private static List<Tokens.Token> WriteLexedTokens(StreamReader @in, string file)
    {
        string coreSource = @in.ReadToEnd();
        var lexer = new Lexer(coreSource, file);
        var lexResult = lexer.LexAll();
        
        // Handle lexer errors
        if (lexResult.Diagnostics.Any())
        {
            Diagnostic.HandleDiagnostics(lexResult.Diagnostics, file);
            throw new Diagnostic.CompilationFailedException($"Failed to process; {lexResult.Diagnostics.Count} errors encountered");
        }
        
        return lexResult.Tokens;
    }

    private static void WriteLexedTokens(StreamReader @in, StreamWriter @out, string file)
    {
        // Assumes input is already in core IR form.
        string coreSource = @in.ReadToEnd();

        var lexer = new Lexer(coreSource, file);
        var lexResult = lexer.LexAll();

        // Handle lexer errors
        if (lexResult.Diagnostics.Any())
        {
            Diagnostic.HandleDiagnostics(lexResult.Diagnostics, file);
            throw new Diagnostic.CompilationFailedException($"Failed to process; {lexResult.Diagnostics.Count} errors encountered");
        }

        var tokens = lexResult.Tokens;
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

    private static void WriteAst(StreamReader @in, StreamWriter @out, string file)
    {
        string coreSource = @in.ReadToEnd();
        var lexer = new Lexer(coreSource, file);
        var lexResult = lexer.LexAll();
        
        // Handle lexer errors before parsing
        if (lexResult.Diagnostics.Any())
        {
            Diagnostic.HandleDiagnostics(lexResult.Diagnostics, file);
            throw new Diagnostic.CompilationFailedException($"Failed to process; {lexResult.Diagnostics.Count} errors encountered");
        }
        
        var parser = new Parser(lexResult.Tokens);
        var ast = parser.ParseSource();
        AstSerializer.Serialize(@out, ast);
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
