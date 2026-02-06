namespace CSBackend;

public static class Tokens
{
    public readonly record struct SourceSpan(int Line, int Column, int Length);

    // Main Token class that the lexer produces
    public class Token(Type tokenType, MetaType metaType, string lexeme, SourceSpan span)
    {
        /// <summary>
        /// Gets the category or classification of the token (e.g., Identifier, Keyword, or Operator).
        /// </summary>
        public Type TokenType { get; } = tokenType;

        /// <summary>
        /// Gets or sets the metadata type of the token, which provides additional context or categorization
        /// beyond the primary token type, such as variable mutability, control flow constructs, or other semantic details.
        /// </summary>
        public MetaType TokenInfo { get; set; } = metaType;

        /// <summary>
        /// Represents the syntactic or semantic classification of a token as determined by the parser,
        /// which indicates the role or construct in the programming language, such as declarations,
        /// statements, expressions, or other significant elements.
        /// </summary>
        /// <remarks>This field should only be touched by parser, not the lexer.</remarks>
        public ParsedType ParsedType { get; set; }

        /// <summary>
        /// Gets the raw text sequence from the source code that matched this token.
        /// </summary>
        public string Lexeme { get; } = lexeme;

        /// <summary>
        /// At which line number this token starts.
        /// </summary>
        public int Line => Span.Line;

        /// <summary>
        /// Which column this token starts at.
        /// </summary>
        public int Column => Span.Column;

        /// <summary>
        /// Gets the length of the token's lexeme in characters.
        /// </summary>
        public int Length => Span.Length;

        public SourceSpan Span { get; } = span;

        public override string ToString() => $"{TokenType} '{Lexeme}'";
    }

    // Flat TokenType enum for the lexer (what you actually scan)

    public enum Type
    {
        Keyword,
        Identifier,
        Literal,
        Symbol,
        Newline,
        Eof
    }

    public enum MetaType
    {
        // Keyword
        Var, 
        Mut, Unmut, 
        If, Else, 
        While, For, Foreach,
        Return, 
        Struct, Trait, 
        Define, 
        Enum, Bundles, 
        Using, As, 
        From, Where,
        Unsafe, Rust, 
        UnsafeRust, Asm,
        Null, 
        Match, Caught,
        Drop, Defer,
            // Syntactic sugar (strip this)
            Static, New, Function,
        
        
        // Identifier (Limit to semantically visible types)
        MutBorrow, Borrow,     // &foo, &!foo 
        Macro,                 // #foo
        Pointer, MutPointer,   // *foo, *!foo
        Generic,               // foo<T>
        
        // Literal
        Binary,   // 0b----
        Hex,      // 0x----
        String,   // "String"
        Char,     // 'E'
        Bool,     // true false
        Integer,  // 3280727
        Float,    // 420.67 or 21E5
        
        // Symbol
            // Operators
            Plus, Minus, Star, Slash, Percent, Pound,   // + - * / % #
            Ampersand, Pipe, Caret, Bang, At,           // & | ^ ! @
            AmpersandAmpersand, PipePipe,               // && ||
            ShiftLeft,  ShiftRight,                     // << >>
            EqualEqual, BangEqual,                      // == !=
            Less,       Greater,                        // < >
            LessEqual,  GreaterEqual,                   // <= >=
            Equal,      EqualGreater,                   // = =>
            Question,   QuestionQuestion,               // ? ??
            QuestionQuestionEqual,                      // ??=
            DotDot,                                     // .. (not sure if we gonna use this)
            
            // Delimiters
            DoubleQuote, SingleQuote,                   // " '
            LeftAngle, RightAngle,                      // < >
            LeftParen, RightParen,                      // ( )
            LeftBrace, RightBrace,                      // { }
            LeftBracket, RightBracket,                  // [ ]
            Semicolon, Comma, Dot,                      // ; , .
            Colon, ColonColon,                          // : ::
    }
    
    // Only added by parsers
    public enum ParsedType
    {
        None, // Default, or for lexemes that do not represent a semantic symbol.

        // === Variable & Parameter Symbols ===
        // Distinguishes between where a variable is defined and where it is used.
        VariableDeclaration,
        VariableUsage,
        ParameterDeclaration, // The definition in a function signature.
        ParameterUsage,       // The use of a parameter within a function body.
    
        // === Function & Method Symbols ===
        // Distinguishes between defining a function/method and calling it.
        FunctionDeclaration,
        FunctionUsage,
        MethodDeclaration,
        MethodUsage,

        // === Type Definition Symbols ===
        // Specific categories for what kind of type is being defined.
        StructDeclaration,
        EnumDeclaration,
        TraitDeclaration,
        TypeAliasDeclaration, // For 'using MyInt = int;' or similar constructs.
        TypeUsage,            // When a type name is used (e.g., in a variable declaration, parameter, or cast).

        // === Member & Variant Symbols ===
        // Symbols that exist within a type definition.
        FieldDeclaration,     // The definition of a field in a struct.
        FieldUsage,
        EnumVariantDeclaration, // The definition of a variant in an enum.
        EnumVariantUsage,

        // === Organizational Symbols ===
        // Symbols related to code structure and imports.
        Module,               // A native Conduit module.
        Namespace,            // A component in a path, e.g., 'Collections' in 'System.Collections'.
        Crate,                // Represents an imported Rust Crate.

        // === Generics & Lifetimes ===
        // Symbols for generic programming and memory management.
        GenericParameterDeclaration, // The 'T' in 'struct Vec<T>'.
        GenericParameterUsage,       // The 'T' in 'fn new() -> T'.
        LifetimeDeclaration,         // The 'a in 'fn foo^[a]()'.
        LifetimeUsage,               // The 'a in '&'a str'.

        // === Metaprogramming Symbols ===
        Macro,
        Attribute
    }

    /// <summary>
    /// Represents the base class for different type expressions in the language syntax,
    /// acting as the foundation for various type definitions such as named types, arrays,
    /// references, and compile-time constants.
    /// <br/><br/>
    /// Part of the abstract syntax tree (AST) representation.
    /// </summary>
    public abstract class TypeNode
    {
        /// <summary>
        /// Represents a simple named type,
        /// such as a built-in scalar (i32, f64) or a user-defined type name.
        /// </summary>
        public class Named(string name) : TypeNode
        {
            /// <summary>Gets the raw identifier string of the type.</summary>
            public string Name { get; } = name;
        }

        /// <summary>
        /// Represents a fixed-size or unsized sequential collection of elements.
        /// </summary>
        public class Array(TypeNode elementType, int? size = null) : TypeNode
        {
            /// <summary>Gets the type of the individual items stored in the array.</summary>
            public TypeNode ElementType { get; } = elementType;
            
            /// <summary>Gets the compile-time constant size of the array,
            /// or null if the size is dynamic or inferred.</summary>
            public int? Size { get; } = size;
        }

        /// <summary>
        /// Represents a pointer or reference to another type, potentially carrying mutability permissions.
        /// </summary>
        public class Reference(TypeNode referent, bool mutable) : TypeNode
        {
            /// <summary>Gets the underlying type being pointed to.</summary>
            public TypeNode Referent { get; } = referent;

            /// <summary>Gets a value indicating whether the data at the referent address
            /// can be modified through this reference.</summary>
            public bool Mutable { get; } = mutable;
        }
        
        /// <summary>
        /// Represents a type or value that has been evaluated at compile-time (JIT/Constant Folding).
        /// For example, [i32; 5 + 5] becomes [i32; Constant(10)]
        /// </summary>
        public class Constant(object value, TypeNode originalType) : TypeNode
        {
            public object Value { get; } = value;
            public TypeNode OriginalType { get; } = originalType;
        }
    }

    /// <summary>
    /// Represents a mapping between token types and their corresponding semantic operator representations.
    /// This dictionary is used to associate lexer-level token types with their respective higher-level
    /// operations in the abstract syntax tree (AST), such as arithmetic, boolean comparisons, and bitwise operations.
    /// </summary>
    public static readonly Dictionary<Type, object> OperatorMap = new()
    {
        { Type.Plus,               Operator.Arithmetic.Add      },
        { Type.Minus,              Operator.Arithmetic.Subtract },
        { Type.Star,               Operator.Arithmetic.Multiply },
        { Type.Slash,              Operator.Arithmetic.Divide   },
        { Type.Percent,            Operator.Arithmetic.Modulo   },
        
        { Type.AmpersandAmpersand, Operator.BoolComparison.And                },
        { Type.PipePipe,           Operator.BoolComparison.Or                 },
        { Type.Less,               Operator.BoolComparison.LessThan           },
        { Type.Greater,            Operator.BoolComparison.GreaterThan        },
        { Type.LessEqual,          Operator.BoolComparison.LessThanOrEqual    },
        { Type.GreaterEqual,       Operator.BoolComparison.GreaterThanOrEqual },
        { Type.EqualEqual,         Operator.BoolComparison.Equal              },
        { Type.BangEqual,          Operator.BoolComparison.NotEqual           },
        
        { Type.ShiftLeft,          Operator.Bit.ShiftLeft  },
        { Type.ShiftRight,         Operator.Bit.ShiftRight },
        { Type.Ampersand,          Operator.Bit.And        },
        { Type.Pipe,               Operator.Bit.Or         },
        { Type.Caret,              Operator.Bit.Xor        }
    };

    /// <summary>
    /// Represents a mapping of keyword strings to their corresponding token types,
    /// enabling the lexer to identify reserved words in the source code.
    /// </summary>
    public static readonly Dictionary<string, MetaType> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "var",         MetaType.Var         },
        { "mut",         MetaType.Mut         },
        { "unmut",       MetaType.Unmut       },
        { "if",          MetaType.If          },
        { "else",        MetaType.Else        },
        { "while",       MetaType.While       },
        { "for",         MetaType.For         },
        { "foreach",     MetaType.Foreach     },
        { "return",      MetaType.Return      },
        { "struct",      MetaType.Struct      },
        { "trait",       MetaType.Trait       },
        { "define",      MetaType.Define      },
        { "enum",        MetaType.Enum        },
        { "bundles",     MetaType.Bundles     },
        { "using",       MetaType.Using       },
        { "as",          MetaType.As          },
        { "from",        MetaType.From        },
        { "where",       MetaType.Where       },
        { "unsafe",      MetaType.Unsafe      },
        { "rust",        MetaType.Rust        },
        { "unsafe_rust", MetaType.UnsafeRust  },
        { "asm",         MetaType.Asm         },
        { "null",        MetaType.Null        },
        { "match",       MetaType.Match       },
        { "caught",      MetaType.Caught      },
        { "drop",        MetaType.Drop        },
        { "defer",       MetaType.Defer       },
        { "static",      MetaType.Static      },
        { "new",         MetaType.New         },
        { "function",    MetaType.Function    }
    };
}
public class Lexer
{
    // Source text to scan. This is the preprocessed Conduit "core" input,
    // but the lexer is robust enough to handle raw source as well.
    private readonly string _Source;

    // _start marks the beginning of the current token.
    // _current is the "cursor" pointing at the next char to consume.
    private int _Start;
    private int _Current;
    
    private readonly string _FilePath;

    // 1-based line counter and raw ccndt string for diagnostics.
    private int _Line;
    private int _LineStart;
    
    private readonly string[] _LinesData;

    // Token collection produced by LexAll.
    private readonly List<Tokens.Token> _Tokens = new();

    private readonly List<Diagnostic> _diagnostics = new();
    
    public Lexer(string source, string filePath)
    {
        _Source = string.IsNullOrEmpty(source) ? string.Empty : source;
        _Start = 0;
        _Current = 0;
        _Line = 1;
        _LineStart = 0;
        _FilePath = filePath; // Store file path for diagnostics
        _LinesData = _Source.Replace("\r\n", "\n").Split('\n');
    }
    
    public string FilePath => _FilePath;
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.AsReadOnly();
    
    // Add diagnostic instead of throwing
    private void ReportError(string message, int column = -1)
    {
        if (column == -1) column = _Current + 1; // Approximate column
        _diagnostics.Add(new Diagnostic(
            Diagnostic.Severity.Error,
            message,
            _FilePath,
            _Line,
            column,
            1
        ));
    }
    
    public record LexResult(List<Tokens.Token> Tokens, List<Diagnostic> Diagnostics);

    /// <summary>
    /// Convenience entry point: scan the entire input and return tokens.
    /// </summary>
    public LexResult LexAll()
    {
        while (!IsAtEnd())
        {
            // Each iteration scans exactly one token.
            _Start = _Current;
            ScanToken();
        }

        var eofSpan = new Tokens.SourceSpan(_Line, _Current - _LineStart + 1, 0);
        _Tokens.Add(new Tokens.Token(Tokens.Type.EndOfFile, string.Empty, eofSpan));
        return new LexResult(_Tokens, _diagnostics);
    }
    
    private int GetLineCol() => _Current - _LineStart;

    /// <summary>
    /// Scans a single token based on the current cursor position.
    /// </summary>
    private void ScanToken()
    { 
        char c = Advance();

        switch (c)
        {
            // Whitespace handling.
            case ' ':
            case '\r':
            case '\t':
                // Ignore non-newline whitespace.
                return;
            
            case '\n':
                AddToken(Tokens.Type.Newline);
                _Line++;
                _LineStart = _Current;
                return;

            // Single-character delimiters and operators.
            case '(': AddToken(Tokens.Type.LeftParen);    return;
            case ')': AddToken(Tokens.Type.RightParen);   return;
            case '{': AddToken(Tokens.Type.LeftBrace);    return;
            case '}': AddToken(Tokens.Type.RightBrace);   return;
            case '[': AddToken(Tokens.Type.LeftBracket);  return;
            case ']': AddToken(Tokens.Type.RightBracket); return;
            case ';': AddToken(Tokens.Type.Semicolon);    return;
            case ',': AddToken(Tokens.Type.Comma);        return;
            case '.': AddToken(Tokens.Type.Dot);          return;
            case '?': AddToken(Tokens.Type.Question);     return;

            // Potentially multi-character operators.
            case '+': AddToken(Tokens.Type.Plus); return;
            case '-': AddToken(Tokens.Type.Minus); return;
            case '*': AddToken(Tokens.Type.Star); return;
            case '%': AddToken(Tokens.Type.Percent); return;
            case '^': AddToken(Tokens.Type.Caret); return;

            case '!':
                AddToken(Match('=') ? Tokens.Type.BangEqual : Tokens.Type.Exclamation);
                return;

            case '=':
                AddToken(Match('=') ? Tokens.Type.EqualEqual : Tokens.Type.Assignment);
                return;

            case '<':
                if (Match('<')) { AddToken(Tokens.Type.ShiftLeft); return; }
                if (Match('=')) { AddToken(Tokens.Type.LessEqual); return; }
                AddToken(Tokens.Type.Less);
                return;

            case '>':
                if (Match('>')) { AddToken(Tokens.Type.ShiftRight); return; }
                if (Match('=')) { AddToken(Tokens.Type.GreaterEqual); return; }
                AddToken(Tokens.Type.Greater);
                return;

            case '&':
                AddToken(Match('&') ? Tokens.Type.AmpersandAmpersand : Tokens.Type.Ampersand);
                return;

            case '|':
                AddToken(Match('|') ? Tokens.Type.PipePipe : Tokens.Type.Pipe);
                return;

            case ':':
                AddToken(Match(':') ? Tokens.Type.ColonColon : Tokens.Type.Colon);
                return;

            case '/':
                // Support comment skipping as a fallback even if preprocessor already stripped them.
                if (Match('/'))
                {
                    // Line comment: skip until newline or EOF.
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                    return;
                }

                if (Match('*'))
                {
                    // Block comment: skip until closing "*/".
                    SkipBlockComment();
                    return;
                }

                AddToken(Tokens.Type.Slash);
                return;

            // String literals (single or double quotes).
            case '"':
            case '\'':
                ReadString(c);
                return;
        }

        if (IsDigit(c))
        {
            ReadNumber();
            return;
        }

        if (IsIdentifierStart(c))
        {
            ReadIdentifierOrKeyword();
            return;
        }

        ReportError($"Unexpected character '{c}'", GetLineCol());
    }

    /// <summary>
    /// Reads an identifier, then decides if it is a keyword or a user-defined name.
    /// </summary>
    private void ReadIdentifierOrKeyword()
    {
        while (IsIdentifierPart(Peek())) Advance();

        string text = _Source.Substring(_Start, _Current - _Start);

        // First check reserved keywords
        if (Tokens.Keywords.TryGetValue(text, out Tokens.Type keywordType))
        {
            AddToken(keywordType);
            return;
        }

        // Then check if it's a known type name (i32, f64, etc.)
        if (IsBuiltInType(text))
        {
            AddToken(Tokens.Type.TypeKeyword);
            return;
        }

        // Otherwise it's a regular identifier
        AddToken(Tokens.Type.Identifier);
        
        bool IsBuiltInType(string name)
        {
            return name switch
            {
                "var" or
                "i8" or "i16" or "i32" or "i64" or "isize" or 
                "u8" or "u16" or "u32" or "u64" or "usize" or
                                 "f32" or "f64" or 
                "bool" or "void" or
                "char" or "string" => true,
                _ => false
            };
        }
    }

    /// <summary>
    /// Reads an integer or float literal.
    /// </summary>
    private void ReadNumber()
    {
        while (IsDigit(Peek()))
            Advance();

        // Fractional part: only if we see ".<digit>".
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance(); // Consume '.'
            while (IsDigit(Peek())) Advance();
            AddToken(Tokens.Type.FloatLiteral);
            return;
        }

        AddToken(Tokens.Type.IntegerLiteral);
    }

    /// <summary>
    /// Reads a quoted string literal, supporting simple escape sequences.
    /// </summary>
    private void ReadString(char quote)
    {
        while (!IsAtEnd())
        {
            char c = Advance();

            if (c == '\n')
            {
                _Line++;
            }

            if (c == '\\')
            {
                // Skip escaped character so it doesn't terminate the string.
                if (!IsAtEnd()) Advance();
                continue;
            }

            if (c == quote)
            {
                AddToken(Tokens.Type.StringLiteral);
                return;
            }
        }

        ReportError("Unterminated string literal", GetLineCol());
    }

    /// <summary>
    /// Skip a /* ... */ comment. Tracks newlines for correct line numbers.
    /// </summary>
    private void SkipBlockComment()
    {
        int initialBlockCommentLine = _Line;
        while (!IsAtEnd())
        {
            if (Peek() == '\n')
            {
                _Line++;
                Advance();
                continue;
            }

            if (Peek() == '*' && PeekNext() == '/')
            {
                Advance(); // '*'
                Advance(); // '/'
                return;
            }

            Advance();
        }

        ReportError($"Unterminated block comment, started from line {initialBlockCommentLine}");
    }
    
    /// <summary>
    /// Consume the next char and advance the cursor.
    /// </summary>
    private char Advance()
    {
        return _Source[_Current++];
    }

    /// <summary>
    /// Adds a token using the lexeme from _start.._current.
    /// </summary>
    private void AddToken(Tokens.Type type)
    {
        string lexeme = _Source.Substring(_Start, _Current - _Start);
        int column = _Start - _LineStart + 1;
        var span = new Tokens.SourceSpan(_Line, column, _Current - _Start);
        _Tokens.Add(new Tokens.Token(type, lexeme, span));
    }

    /// <summary>
    /// Conditional consume: if next char matches expected, consume it and return true.
    /// </summary>
    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (_Source[_Current] != expected) return false;
        _Current++;
        return true;
    }

    /// <summary>
    /// Look at current char without consuming it.
    /// </summary>
    private char Peek()
    {
        if (IsAtEnd()) return '\0';
        return _Source[_Current];
    }

    /// <summary>
    /// Look ahead by one char without consuming it.
    /// </summary>
    private char PeekNext()
    {
        if (_Current + 1 >= _Source.Length) return '\0';
        return _Source[_Current + 1];
    }

    private bool IsAtEnd()
    {
        return _Current >= _Source.Length;
    }

    // Pattern instead of regex for efficiency
    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    // Conduit identifiers start with alpha or underscore (see IdentifierRegex.alpha + common "_" binding).
    private static bool IsIdentifierStart(char c) => char.IsLetter(c) || c == '_';

    // Remaining identifier characters allow digits and underscore.
    private static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';
}
