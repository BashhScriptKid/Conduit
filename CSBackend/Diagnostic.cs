using System.Text;

namespace CSBackend;

public class Diagnostic(Diagnostic.Severity level, string message, string filePath, int line, int column, int length = 1)
{
    public enum Severity
    {
        Error,
        Warning,
        Info
    }

    public Severity Level { get; } = level;
    public string Message { get; } = message;
    public string FilePath { get; } = filePath;
    public int Line { get; } = line;
    public int Column { get; } = column;
    public int Length { get; } = length;

    /// <summary>
    /// Returns a simple one-line representation of the diagnostic.
    /// </summary>
    public override string ToString() => $"{FilePath}:{Line}:{Column}: {Level.ToString().ToLower()}: {Message}";
    
    public static void HandleDiagnostics(List<Diagnostic> diagnostics, string file)
    {
        try
        {
            var sourceLines = File.ReadAllLines(file);
            foreach (var diag in diagnostics)
            {
                if (diag.Line > 0 && diag.Line <= sourceLines.Length)
                {
                    Console.WriteLine(diag.FormatForConsole(sourceLines[diag.Line - 1]));
                }
                else
                {
                    Console.WriteLine(diag.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback if we can't read the file
            Console.WriteLine($"Failed to read source file for error context: {ex.Message}");
            foreach (var diag in diagnostics)
            {
                Console.WriteLine(diag.ToString());
            }
        }
    }

// Simple custom exception for compilation failures
    public class CompilationFailedException(string message) : Exception(message);
}

// Separate formatter class - this is your "helper/abstraction"
public static class DiagnosticFormatter
{
    extension(Diagnostic diag)
    {
        /// <summary>
        /// Formats a diagnostic with code context for console display.
        /// </summary>
        public string FormatForConsole(string codeLine)
        {
            var builder = new StringBuilder();
        
            // Color codes (optional - you can handle coloring in the driver instead)
            string levelPrefix = diag.Level switch
            {
                Diagnostic.Severity.Error => "error",
                Diagnostic.Severity.Warning => "warning",
                _ => "info"
            };
        
            builder.AppendLine($"{diag.FilePath} at ({diag.Line}:{diag.Column}): {levelPrefix}: {diag.Message}");
            builder.AppendLine(codeLine);
        
            // Build pointer line
            var pointer = new StringBuilder();
            int col = Math.Max(1, diag.Column);
            int colIndex = Math.Min(col - 1, codeLine.Length); // clamp to end
            pointer.Append(' ', colIndex);

            int availableLength = Math.Max(0, codeLine.Length - colIndex);
            int pointerLength = Math.Min(Math.Max(1, diag.Length), Math.Max(1, availableLength));
            pointer.Append('^', pointerLength);
        
            builder.Append(pointer);
            return builder.ToString();
        }

        /// <summary>
        /// Creates a formatted error message ready for immediate display.
        /// </summary>
        public void PrintToConsole(string sourceFile)
        {
            try
            {
                var lines = File.ReadAllLines(sourceFile);
                if (diag.Line > 0 && diag.Line <= lines.Length)
                {
                    string codeLine = lines[diag.Line - 1];
                    string formatted = diag.FormatForConsole(codeLine);

                    Console.ForegroundColor = diag.Level switch
                    {
                        // Apply colors
                        Diagnostic.Severity.Error   => ConsoleColor.Red,
                        Diagnostic.Severity.Warning => ConsoleColor.Yellow,
                        _ => Console.ForegroundColor
                    };

                    Console.WriteLine(formatted);
                    Console.ResetColor();
                }
                else
                {
                    // Fallback if we can't get code context
                    Console.WriteLine(diag.ToString());
                }
            }
            catch (Exception ex)
            {
                // Fallback to basic output if file reading fails
                Console.WriteLine($"[Failed to read {sourceFile}: {ex.Message}]");
                Console.WriteLine(diag.ToString());
            }
        }
    }
}