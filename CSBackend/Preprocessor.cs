using System.Text.RegularExpressions;

namespace CSBackend;

public static class Preprocessor
{

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
        RunPass(StripComments);
        
        RunPass(MaskQuotes);
        
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
        string? line;
        while ((line = @in.ReadLine()) != null)
        {
            // Process the line through both replacements before writing it once
            string p = Regex.Replace(line, @"//.*", string.Empty);
            p = Regex.Replace(p, @"/\*.*?\*/", string.Empty);
            
            // Skip empty lines (which happens when comments are stripped)
            if (string.IsNullOrWhiteSpace(p)) 
                continue;
            @out.WriteLine(p);
        }
    }

    private static readonly Regex StringLiteralRegex = new(@"""([^""\\]|\\.)*""", RegexOptions.Compiled);
    
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
            // &!var -> &mut-var
            p = Regex.Replace(p, $@"&!({IdentifierRegex.@this})", $@"&mut-$1");
            // &var -> &-var
            p = Regex.Replace(p, $@"&({IdentifierRegex.@this})", $@"&-$1");
            // *!var -> *mut-var
            p = Regex.Replace(p, $@"\*!({IdentifierRegex.@this})", $@"*mut-$1");
            // *var -> *-var
            p = Regex.Replace(p, $@"\*({IdentifierRegex.@this})", $@"*-$1");
                
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
            p = p.Replace("#asm", "std::arch::asm!");
            // General macros: #name -> name!
            p = Regex.Replace(p, $@"#({IdentifierRegex.@this})", $@"$1!");
                
            @out.WriteLine(p);
        }
    }
}