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
        LifeTimeSpecifier,
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
        MutBorrow, Borrow,                 // &foo, &!foo 
        Macro,                             // #foo
        Pointer, MutPointer,               // *foo, *!foo
        IdentifierNegate,                  // !foo
        
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
            Plus, Minus, Star, Slash, Percent,          // + - * / %
            Ampersand, Pipe, Caret, Bang,               // & | ^ ! 
            AmpersandAmpersand, PipePipe,               // && ||
            ShiftLeft,  ShiftRight,                     // << >>
            PlusPlus, MinusMinus,                       // ++ --
            PlusEqual, MinusEqual,                      // += -=
            StarEqual, SlashEqual,                      // *= /=
            AmpersandEqual, CaretEqual, PipeEqual,      // &= ^= |=
            ShiftLeftEqual, ShiftRightEqual,            // <<= >>=
            EqualEqual, BangEqual,                      // == !=
            Less_LeftAngle, Greater_RightAngle,         // < >
            LessEqual,  GreaterEqual,                   // <= >=
            Equal,      EqualGreater,                   // = =>
            Question,   QuestionQuestion,               // ? ??
            QuestionQuestionEqual,                      // ??=
            DotDot,                                     // .. (not sure if we gonna use this)
            
            // Delimiters
            LeftParen, RightParen,                      // ( )
            LeftBrace, RightBrace,                      // { }
            LeftBracket, RightBracket,                  // [ ]
            Semicolon, Comma, Dot,                      // ; , .
            Colon, ColonColon,                          // : ::
            None,
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
        { "function",    MetaType.Function    },
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
        _Tokens.Add(new Tokens.Token(Tokens.Type.Eof, Tokens.MetaType.None, string.Empty, eofSpan));
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
                AddToken(Tokens.Type.Newline, Tokens.MetaType.None);
                _Line++;
                _LineStart = _Current;
                return;

            // Single-character delimiters and operators.
            case '(': AddToken(Tokens.Type.Symbol, Tokens.MetaType.LeftParen);    return;
            case ')': AddToken(Tokens.Type.Symbol, Tokens.MetaType.RightParen);   return;
            case '{': AddToken(Tokens.Type.Symbol, Tokens.MetaType.LeftBrace);    return;
            case '}': AddToken(Tokens.Type.Symbol, Tokens.MetaType.RightBrace);   return;
            case '[': AddToken(Tokens.Type.Symbol, Tokens.MetaType.LeftBracket);  return;
            case ']': AddToken(Tokens.Type.Symbol, Tokens.MetaType.RightBracket); return;
            case ';': AddToken(Tokens.Type.Symbol, Tokens.MetaType.Semicolon);    return;
            case ',': AddToken(Tokens.Type.Symbol, Tokens.MetaType.Comma);        return;
            case '.':
                // Handle range/operator '..' first
                if (Match('.'))
                {
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.DotDot);
                    return;
                }

                // Leading-dot float: `.123` -> Float literal
                if (IsDigit(Peek()))
                {
                    // Consume fractional digits after the dot
                    while (IsDigit(Peek())) Advance();

                    // Optional exponent part
                    if (Peek() == 'e' || Peek() == 'E')
                    {
                        Advance(); // consume 'e' or 'E'
                        if (Peek() == '+' || Peek() == '-') Advance(); // optional sign
                        if (!IsDigit(Peek()))
                        {
                            ReportError("Invalid float literal. Expected at least one digit in exponent.", GetLineCol());
                        }
                        else
                        {
                            while (IsDigit(Peek())) Advance();
                        }
                    }

                    AddToken(Tokens.Type.Literal, Tokens.MetaType.Float);
                    return;
                }

                AddToken(Tokens.Type.Symbol, Tokens.MetaType.Dot);
                return;
            case '?':
                if (Match('?'))
                {
                    if (Match('='))
                        AddToken(Tokens.Type.Symbol, Tokens.MetaType.QuestionQuestionEqual);
                    else
                        AddToken(Tokens.Type.Symbol, Tokens.MetaType.QuestionQuestion);

                    return;
                }
                AddToken(Tokens.Type.Symbol, Tokens.MetaType.Question);
                return;

            // Potentially multi-character operators.
            case '+':
                if (Match('='))
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.PlusEqual); // Handles +=
                else if (Match('+'))
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.PlusPlus); // Handles ++
                else
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.Plus);      // Handles +
                return;

            case '-': 
                if (Match('='))
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.MinusEqual); // Handles -=
                else if (Match('-'))
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.MinusMinus); // Handles --
                else
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.Minus);      // Handles -
                return;
                
            case '*':
                if (Match('='))
                {
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.StarEqual); // Emits *=
                }
                else if (Match('!'))
                {
                    // Check if it's '*!' followed by an identifier, for a mutable pointer type context
                    if (TryConsumeIdentifier(out _))
                    {
                        AddToken(Tokens.Type.Identifier, Tokens.MetaType.MutPointer); // Emits *!
                    }
                    else
                    {
                        // Report the error and do not emit a token.
                        ReportError("Invalid token sequence '*!'. A mutable pointer must be followed by an identifier.");
                    }
                }
                else if (TryConsumeIdentifier(out _))
                {
                    // Check if it's '*' followed by an identifier, for a pointer type context
                    AddToken(Tokens.Type.Identifier, Tokens.MetaType.Pointer); // Emits *
                }
                else
                {
                    // It's just a multiplication operator
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.Star); // Emits *
                }
                return; // return after handling all cases for '*'
            
            case '%': AddToken(Tokens.Type.Symbol, Tokens.MetaType.Percent); return;
            
            case '^':
                if (Match('='))
                {
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.CaretEqual); // Handles ^=
                }
                // Check if a lifetime specifier is starting (^ followed by [)
                else if (Peek() == '[')
                {
                    // _Start already points to the '^'. Now we consume the rest.
                    Advance(); // Consume '['

                    bool validLifetime = true;

                    // Consume lifetime identifiers (may be multiple: ^['a, 'b])
                    consumeInnerBrace:

                    // Optional: handle the single quotes for named lifetimes.
                    if (Peek() is '\'')
                    {
                        Advance(); // Consume opening ' for lifetime usage (^['foo])
                    }

                    // Consume the identifier/name part of the lifetime.
                    if (IsIdentifierStart(Peek()))
                    {
                        if (!TryConsumeIdentifier(out _))
                        {
                            // Error already reported by TryConsumeIdentifier
                            validLifetime = false;
                        }
                    }
                    else
                    {
                        ReportError("Expected lifetime identifier after '^[' or ','.", GetLineCol());
                        validLifetime = false;
                    }

                    // Check for multiple lifetimes or end of specifier
                    if (Peek() is ',' or ' ') // Multiple lifetime usage/declaration
                    {
                        Advance();
                        goto consumeInnerBrace;
                    }
                    
                    // The lifetime specifier MUST end with a ']'
                    if (Match(']')) // Match() consumes the ']' if it exists.
                    {
                        // We have successfully consumed the entire ^[...] structure.
                        if (validLifetime)
                        {
                            AddToken(Tokens.Type.LifeTimeSpecifier, Tokens.MetaType.None);
                        }
                        // If not valid, error was already reported, don't emit token
                    }
                    else
                    {
                        // We saw '^[' but never found the closing ']'. This is a syntax error.
                        ReportError("Unterminated lifetime specifier. Expected ']'.", GetLineCol());
                    }
                }
                else
                {
                    // It wasn't '^=' or '^[' so it must be the XOR operator.
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.Caret); 
                }
                return;

            case '!':
                if (Match('='))
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.BangEqual);
                else if (TryConsumeIdentifier(out _))
                    AddToken(Tokens.Type.Identifier, Tokens.MetaType.IdentifierNegate);
                else 
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.Bang);
                return;

            case '=':
                if (Match('='))
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.EqualEqual);
                else if (Match('>'))
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.EqualGreater);
                else
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.Equal);
                return;

            case '<':
                if (Match('<')) // <<
                {
                    if (Match('=')) // <<=
                        AddToken(Tokens.Type.Symbol, Tokens.MetaType.ShiftLeftEqual);
                    else // <<
                        AddToken(Tokens.Type.Symbol, Tokens.MetaType.ShiftLeft);
                    return;
                }
                if (Match('=')) // <=
                {
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.LessEqual);
                    return;
                }
                // <
                AddToken(Tokens.Type.Symbol, Tokens.MetaType.Less_LeftAngle);
                return;

            case '>':
                if (Match('>')) // >>
                {
                    if (Match('=')) // >>=
                        AddToken(Tokens.Type.Symbol, Tokens.MetaType.ShiftRightEqual);
                    else // >>
                        AddToken(Tokens.Type.Symbol, Tokens.MetaType.ShiftRight);
                    return;
                }
                if (Match('=')) // >=
                {
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.GreaterEqual);
                    return;
                }
                // >
                AddToken(Tokens.Type.Symbol, Tokens.MetaType.Greater_RightAngle);
                return;

            case '&':
                if (Match('!'))
                {
                    // Check if it's '&!' followed by an identifier, for a mutable borrow type context
                    if (TryConsumeIdentifier(out _))
                    {
                        AddToken(Tokens.Type.Identifier, Tokens.MetaType.MutBorrow); // &!foo
                    }
                    else
                    {
                        // Report the error and do not emit a token.
                        ReportError("Invalid token sequence '&!'. A mutable borrow must be followed by an identifier.");
                    }
                }
                else if (TryConsumeIdentifier(out _))
                {
                    // Check if it's '&' followed by an identifier, for a borrow type context
                    AddToken(Tokens.Type.Identifier, Tokens.MetaType.Borrow); // &foo
                }
                else if (Match('=')) // &=
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.AmpersandEqual) ;
                else
                {
                    // It's just an ampersand operator (or double ampersand)
                    AddToken(Tokens.Type.Symbol, Match('&') ? Tokens.MetaType.AmpersandAmpersand : Tokens.MetaType.Ampersand);
                }
                return;

            case '|':
                if (Match('=')) // |=
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.PipeEqual);
                else if (Match('|')) // ||
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.PipePipe);
                else // |
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.Pipe);
                return;

            case ':':
                AddToken(Tokens.Type.Symbol, Match(':') ? Tokens.MetaType.ColonColon : Tokens.MetaType.Colon);
                return;
            
            case '#':
                if (TryConsumeIdentifier(out _))
                    AddToken(Tokens.Type.Identifier, Tokens.MetaType.Macro);
                else
                    ReportError("Expected identifier after '#'.", GetLineCol());
                return;

            case '/':
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

                if (Match('='))
                {
                    AddToken(Tokens.Type.Symbol, Tokens.MetaType.SlashEqual);
                    return;
                }

                AddToken(Tokens.Type.Symbol, Tokens.MetaType.Slash);
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
    /// Reads an identifier, then decides if it is a keyword or a plain user-defined name.
    /// </summary>
    private void ReadIdentifierOrKeyword()
    {
        bool escapeKeyword = false;

        // 1. Check for the optional escape character FIRST.
        if (Peek() == '@')
        {
            escapeKeyword = true;
            Advance(); // Consume the '@'.
        }

        // 2. Consume the identifier characters.
        while (IsIdentifierPart(Peek())) Advance();

        // 3. Extract the full lexeme (including '@' if present) and the identifier part (without '@').
        string fullLexeme = _Source.Substring(_Start, _Current - _Start);
        string identifierPart = escapeKeyword ? fullLexeme.Substring(1) : fullLexeme;

        if (IsBool(identifierPart))
        {
            AddToken(Tokens.Type.Literal, Tokens.MetaType.Bool);
            return;
        }

        // 4. Check if it's a reserved keyword (only if not escaped).
        if (Tokens.Keywords.TryGetValue(identifierPart, out Tokens.MetaType keywordType) && !escapeKeyword)
        {
            AddToken(Tokens.Type.Keyword, keywordType);
            return;
        }
        
        // 5. Otherwise it's a regular identifier (possibly escaped).
        AddToken(Tokens.Type.Identifier, Tokens.MetaType.None);
    }

    /// <summary>
    /// Attempts to consume an identifier from the current position, handling escaped keywords.
    /// Does not add a token. The cursor must be positioned at the start of an identifier or '@' escape.
    /// </summary>
    /// <param name="identifier">The consumed identifier string, or null if consumption fails.</param>
    /// <returns>True if a valid identifier was consumed, otherwise false.</returns>
    private bool TryConsumeIdentifier(out string identifier)
    {
        int positionAtStart = _Current; // Save position in case we need to report errors accurately
        bool escapeKeyword = false;

        // 1. Check for the optional escape character FIRST.
        if (Peek() == '@')
        {
            escapeKeyword = true;
            Advance(); // Consume the '@'.
        }

        // 2. NOW, check if a valid identifier starts at the current position.
        if (!IsIdentifierStart(Peek()))
        {
            // This handles cases where there's no identifier, OR a standalone '@'.
            if (escapeKeyword)
            {
                ReportError("Expected an identifier after the '@' escape character.", positionAtStart - _LineStart + 1);
            }
            identifier = null;
            return false;
        }

        // 3. Mark the start and consume the identifier.
        int startOfIdentifier = _Current;
        while (IsIdentifierPart(Peek()))
        {
            Advance();
        }
        identifier = _Source.Substring(startOfIdentifier, _Current - startOfIdentifier);

        // 4. Validate against keywords (only if not escaped).
        if ((Tokens.Keywords.TryGetValue(identifier, out _) || IsBool(identifier)) && !escapeKeyword)
        {
            ReportError($"Unexpected keyword or bool literal '{identifier}'. To use it as an identifier, prefix it with '@'.", _Current - _LineStart + 1);
            return false; // Indicate failure, but the 'identifier' out parameter will hold the keyword.
        }
    
        // Success!
        return true;
    }

    /// <summary>
    /// Reads an integer or float literal.
    /// </summary>
    private void ReadNumber()
    {
        // Check for binary (0b) or hexadecimal (0x) prefixes
        if (Peek() == '0')
        {
            char next = PeekNext();

            switch (next)
            {
                case 'b' or 'B':
                {
                    Advance(); // Consume '0'
                    Advance(); // Consume 'b' or 'B'
                    if (!IsDigit(Peek()) || (Peek() != '0' && Peek() != '1'))
                    {
                        ReportError("Invalid binary literal. Expected at least one binary digit (0-1) after '0b'.", GetLineCol());
                        return;
                    }
                    while (Peek() == '0' || Peek() == '1') Advance();
                    AddToken(Tokens.Type.Literal, Tokens.MetaType.Binary);
                    return;
                }
                case 'x' or 'X':
                {
                    Advance(); // Consume '0'
                    Advance(); // Consume 'x' or 'X'
                    if (!IsHexDigit(Peek()))
                    {
                        ReportError("Invalid hexadecimal literal. Expected at least one hex digit (0-9, a-f, A-F) after '0x'.", GetLineCol());
                        return;
                    }
                    while (IsHexDigit(Peek())) Advance();
                    AddToken(Tokens.Type.Literal, Tokens.MetaType.Hex);
                    return;
                }
            }
        }

        // Decimal integer or float
        while (IsDigit(Peek()))
            Advance();

        if (Peek() == '_')
            Advance(); // Syntactic separator

        // Fractional part: only if we see ".<digit>".
        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance(); // Consume '.'
            while (IsDigit(Peek())) Advance();
            // Check for exponent
            if (Peek() == 'e' || Peek() == 'E')
            {
                Advance(); // Consume 'e' or 'E'
                if (Peek() == '+' || Peek() == '-') Advance(); // Optional sign
                if (!IsDigit(Peek()))
                {
                    ReportError("Invalid float literal. Expected at least one digit in exponent.", GetLineCol());
                    return;
                }
                while (IsDigit(Peek())) Advance();
            }
            AddToken(Tokens.Type.Literal, Tokens.MetaType.Float);
            return;
        }

        // Check for exponent on integer (e.g., 12E3)
        if (Peek() == 'e' || Peek() == 'E')
        {
            Advance(); // Consume 'e' or 'E'
            if (Peek() == '+' || Peek() == '-') Advance(); // Optional sign
            if (!IsDigit(Peek()))
            {
                ReportError("Invalid float literal. Expected at least one digit in exponent.", GetLineCol());
                return;
            }
            while (IsDigit(Peek())) Advance();
            AddToken(Tokens.Type.Literal, Tokens.MetaType.Float);
            return;
        }

        AddToken(Tokens.Type.Literal, Tokens.MetaType.Integer);
        
        bool IsHexDigit(char c) => IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
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
                // Determine literal type based on quote character
                Tokens.MetaType literalType = quote == '"' ? Tokens.MetaType.String : Tokens.MetaType.Char;
                AddToken(Tokens.Type.Literal, literalType);
                return;
            }
        }

        ReportError("Unterminated string literal", GetLineCol());
    }
    
    private bool IsBool(string s) => s == "true" || s == "false";

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
    private void AddToken(Tokens.Type type, Tokens.MetaType metaType)
    {
        string lexeme = _Source.Substring(_Start, _Current - _Start);
        int column = _Start - _LineStart + 1;
        var span = new Tokens.SourceSpan(_Line, column, _Current - _Start);
        _Tokens.Add(new Tokens.Token(type, metaType, lexeme, span));
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
