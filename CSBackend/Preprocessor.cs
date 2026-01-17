using System.Text;
using System.Text.RegularExpressions;
using CSBackend.Transpiler;

namespace CSBackend;

public static partial class Preprocessor
{
    // This preprocessor performs purely lexical normalization.
    // It does NOT validate syntax, lifetimes, scopes, or semantics.
    // Any correctness errors are deferred to Rust or later compiler stages.

    public static void PreprocessToCore(StreamReader @in, StreamWriter @out)
    {
        void Log(string message) => ConduitProgram.Log(message, "Preprocessor");
        
        Log("Starting PreprocessToCore");
        using MemoryStream sourceBuffer = new();
        using MemoryStream targetBuffer = new();

        // 1. Initial Pass: Copy raw input to sourceBuffer
        @in.BaseStream.CopyTo(sourceBuffer);

        void RunPass(Action<StreamReader, StreamWriter> passAction)
        {
            Log($"Running pass: {passAction.Method.Name}");
            sourceBuffer.Position = 0;
            targetBuffer.SetLength(0);
        
            // We use 'using' here to ensure the Writer is FLUSHED before we copy
            using (var reader = new StreamReader(sourceBuffer, leaveOpen: true))
            using (var writer = new StreamWriter(targetBuffer, leaveOpen: true))
            {
                passAction(reader, writer);
                writer.Flush(); // Crucial: push remaining bits to targetBuffer
            }

            sourceBuffer.SetLength(0);
            targetBuffer.Position = 0;
            targetBuffer.CopyTo(sourceBuffer);
            Log($"Pass {passAction.Method.Name} completed. Buffer size: {sourceBuffer.Length} bytes");
        }

        // Pass iterations here
        RunPass(MaskQuotes); // Mask quotes before stripping comments (ahem, links)
        
        RunPass(StripComments);
        
        RunPass(ConvertNativeTypeKeyword);
        
        // Convert native syntax
        RunPass(StripStorageQualifiers);
        RunPass(ConvertReferencesAndPointers);
        RunPass(ConvertMacros);
        
        RunPass(UnmaskQuotes);

        // 3. Final Pass: write sourceBuffer to output
        Log("Finalizing output");
        @out.Flush(); // Ensure output is ready
        sourceBuffer.Position = 0;
        sourceBuffer.CopyTo(@out.BaseStream);
        
        @out.Flush();
        Log("PreprocessToCore completed");
    }

    private static void StripComments(StreamReader @in, StreamWriter @out)
    {
        bool inBlock = false;
        string? line;

        while ((line = @in.ReadLine()) != null)
        {
            var result = new StringBuilder();
            int column_Ptr = 0;

            // Dynamic line sub-reader with column pointer
            while (column_Ptr < line.Length)
            {
                // CASE 1: Currently inside a /* ... */ block
                if (inBlock)
                {
                    int endIndex = line.IndexOf("*/", column_Ptr, StringComparison.InvariantCulture);
                    if (endIndex == -1) break; // Entire rest of line is hidden

                    column_Ptr = endIndex + 2; // Move past the closing '*/'
                    inBlock = false;
                    continue;
                }

                // CASE 2: Not in a block, looking for the next comment marker
                int nextBlockStart = line.IndexOf("/*", column_Ptr, StringComparison.InvariantCulture);
                int nextLineComment = line.IndexOf("//", column_Ptr, StringComparison.InvariantCulture);

                // Determine which comment comes first
                int commentIndex = GetFirstCommentIndex(nextBlockStart, nextLineComment);

                if (commentIndex == -1)
                {
                    // No more comments on this line! Append the rest and finish
                    result.Append(line.AsSpan(column_Ptr));
                    break;
                }

                // Append the code that appeared BEFORE the comment
                result.Append(line.AsSpan(column_Ptr, commentIndex - column_Ptr));

                // Figure out WHICH comment we just hit
                if (commentIndex == nextLineComment)
                {
                    break; // Skip the rest of the line for //
                }
                else
                {
                    inBlock = true; // Enter block mode for /*
                    column_Ptr = commentIndex + 2;
                }
            }

            // Write the code if any exists (helps maintain file structure)
            string output = result.ToString();
            if (!string.IsNullOrWhiteSpace(output))
            {
                @out.WriteLine(output.TrimEnd());
            }
        }
        if (inBlock)
            throw new InvalidOperationException("Unterminated comment block! Did you forget a closing '*/'? (Unexpected EOF)");
        
        int GetFirstCommentIndex(int block, int line)
        {
            if (block == -1) return line;
            if (line == -1) return block;
            return Math.Min(block, line);
        }
        
    }
    
    private static readonly Regex LiteralRegex = new(
        @"""([^""\\]|\\.)*""|'([^'\\]|\\.)*'", 
        RegexOptions.Compiled
    );
    
    static readonly Dictionary<string, string> _maskedStrings = new();
    
    private static void MaskQuotes(StreamReader @in, StreamWriter @out)
    {
        // Safety: ensure clean state (helps catch logic errors during dev)
        if (_maskedStrings.Count > 0)
            throw new InvalidOperationException("MaskQuote called with non-empty masked strings. Did the previous run fail?");
    
        string? line;
        while ((line = @in.ReadLine()) != null)
        {
            string maskedLine = StringLiteralRegex.Replace(line, match =>
            {
                string placeholder = $"__STRING_{_maskedStrings.Count}__";
                _maskedStrings[placeholder] = match.Value;
                return placeholder;
            });
            @out.WriteLine(maskedLine);
        }
    }

    private static void UnmaskQuotes(StreamReader @in, StreamWriter @out)
    {
        string? line;
        while ((line = @in.ReadLine()) != null)
        {
            string finalLine = line;
            foreach (var (placeholder, value) in _maskedStrings)
            {
                finalLine = finalLine.Replace(placeholder, value);
            }
            @out.WriteLine(finalLine);
        }
    
        // Critical: always clear after unmasking
        _maskedStrings.Clear();
    }

    private static void ConvertNativeTypeKeyword(StreamReader @in, StreamWriter @out)
    {
        // Map conduit native types to Rust-like types
        Dictionary<string, string> types = new()
        {
            { "var",        "let" },
            { "archint",  "isize" },
            { "uarchint", "usize" },   // Additional alias
            { "sbyte",       "i8" },  { "int8",      "i8" },
            { "byte",        "u8" },  { "uint8",     "u8" },
            { "short",      "i16" },  { "int16",    "i16" },
            { "ushort",     "u16" },  { "uint16",   "u16" },
            { "int",        "i32" },  { "int32",    "i32" },
            { "uint",       "u32" },  { "uint32",   "u32" },
            { "long",       "i64" },  { "int64",    "i64" },
            { "ulong",      "u64" },  { "uint64",   "u64" },
            { "loong",     "i128" },  { "int128",  "i128" },
            { "uloong",    "u128" },  { "uint128", "u128" },
            { "float",     "f32"  },  { "float32",  "f32" },
            { "double",    "f64"  },  { "float64",  "f64" },
        };
        
        // Sort by length to avoid partial match-replacements (long vs loong)
        var sortedKeys = types.Keys.OrderByDescending(k => k.Length).ToList();


        string? line;
        while ((line = @in.ReadLine()) != null)
        {
            string processedLine = line;
            
            foreach (var key in sortedKeys)
            {
                processedLine = Regex.Replace(processedLine, $@"\b{key}\b", types[key]);
            }
            @out.WriteLine(processedLine);
        }
    }

    private static void StripStorageQualifiers(StreamReader @in, StreamWriter @out)
    {
        string? line;
        while ((line = @in.ReadLine()) != null)
        {
            // Removes 'function' or 'static' when they precede a type/identifier
            string p = Regex.Replace(line, @"\b(function|static)\s+", string.Empty);
            @out.WriteLine(p);
        }
    }

    private static void ConvertReferencesAndPointers(StreamReader @in, StreamWriter @out)
    {
        string? line;
        while ((line = @in.ReadLine()) != null)
        {
            string p = line;

            // The reason - is put between annotation is to make sure the relationship
            // between the annotation and the variable stays clear.
            // The hyphen preserves the relationship between annotation and variable in Conduit Core IR.
            
            // NOTE: Reference operators must be adjacent to identifiers (&x, &!x).
            // Whitespace-separated forms (& x) are not supported at this stage.

            
            // 1. Handle Mutable cases first (Specific matches)
            // '&!var' -> '&mut-var'
            p = Regex.Replace(p, $"&!({IdentifierRegex.@this})", "&mut-$1");
            // '*!var' -> '*mut-var'
            p = Regex.Replace(p, $@"\*!({IdentifierRegex.@this})", "*mut-$1");

            // 2. Handle Immutable cases (General matches)
            // (?!mut-) means "Match & only if it's NOT followed by the string 'mut-'"
            // This prevents the rule from re-processing what the first rule just changed.
            
            // '&var' -> '&-var' 
            p = Regex.Replace(p, $"&(?!mut-)({IdentifierRegex.@this})", "&-$1");
            // '*var' -> '*-var'
            p = Regex.Replace(p, $@"\*(?!mut-)({IdentifierRegex.@this})", "*-$1");
                
            @out.WriteLine(p);
        }
    }

    private static void ConvertMacros(StreamReader @in, StreamWriter @out)
    {
        string? line;
        while ((line = @in.ReadLine()) != null)
        {
            string p = line;
            // Special case for asm first
            var specialMacros = new Dictionary<string, string>
            {
                { "#asm", "std::arch::asm!" }
            };

            foreach (var (from, to) in specialMacros)
            {
                p = p.Replace(from, to);
            }

            // General macros (but don't match already-processed ones)
            p = Regex.Replace(p, @"#(\w+)(?!!)", "$1!");  // Negative lookahead for !
            @out.WriteLine(p);
        }
    }
}