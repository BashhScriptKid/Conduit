using System.Diagnostics.CodeAnalysis;

namespace CSBackend;

public static class Tokens
{
    // Main Token class that the lexer produces
    public class Token(Type tokenType, string lexeme, int line)
    {
        /// <summary>
        /// Gets the category or classification of the token (e.g., Identifier, Keyword, or Operator).
        /// </summary>
        public Type TokenType { get; } = tokenType;

        /// <summary>
        /// Gets the raw text sequence from the source code that matched this token.
        /// </summary>
        public string Lexeme { get; } = lexeme;

        /// <summary>
        /// Gets the 1-based line number in the source file where this token was encountered.
        /// </summary>
        public int Line { get; } = line;

        public override string ToString() => $"{TokenType} '{Lexeme}'";
    }

    // Flat TokenType enum for the lexer (what you actually scan)
    public enum Type
    {
        // Literals
        IntegerLiteral, // Default to i32 at minimum
        FloatLiteral,   // Default to f32 at minimum
        StringLiteral,  // Default to string (obviously, but there's more than one way to make a string actually)
        
        // Identifiers
        Identifier, // Enforce IdentifierRegex.Alphanumeric rule
        
        // Keywords
        Var, Mut, Fn, If, Else, While, For, Return, Void,
        
        // Type keywords (from Core IR output)
        
        // ReSharper disable InconsistentNaming
        i8, i16, i32, i64, iSize,
        u8, u16, u32, u64, uSize,
                 f32, f64,
        // ReSharper restore InconsistentNaming
        
        // Operators
        Plus, Minus, Star, Slash, Percent,     // + - * / %
        Ampersand, Pipe, Caret,                // & | ^
        AmpersandAmpersand, PipePipe,          // && ||
        ShiftLeft, ShiftRight,                 // << >>
        EqualEqual, BangEqual,                 // == !=
        Less,      Greater,                    // < >
        LessEqual, GreaterEqual,               // <= >=
        Assignment,                            // =
        
        // Delimiters
        LeftAngle, RightAngle,                 // < >
        LeftParen, RightParen,                 // ( )
        LeftBrace, RightBrace,                 // { }
        LeftBracket, RightBracket,             // [ ]
        Semicolon, Comma, Dot,                 // ; , .
        Colon, ColonColon,                     // : ::
        
        // Special
        Exclamation, Question,                 // ! ?
        EndOfFile
    }

    // Semantic operator types (for AST, not lexer)
    public static class Operator
    {
        public enum Arithmetic
        {
            Add, Subtract, Multiply, Divide, Modulo
        }

        public enum BoolComparison
        {
            And, Or,
            LessThan,        GreaterThan,
            LessThanOrEqual, GreaterThanOrEqual,
            Equal, NotEqual,
        }

        public enum Bit
        {
            ShiftLeft, ShiftRight,
            And, Or, Xor
        }
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
    public static readonly Dictionary<string, Type> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        { "var",    Type.Var    },
        { "mut",    Type.Mut    },
        { "fn",     Type.Fn     },
        { "if",     Type.If     },
        { "else",   Type.Else   },
        { "while",  Type.While  },
        { "for",    Type.For    },
        { "return", Type.Return },
        
        // Types from preprocessor
        { "i8", Type.i8 }, { "i16", Type.i16 }, { "i32", Type.i32 }, { "i64", Type.i64 }, { "isize", Type.iSize },
        { "u8", Type.u8 }, { "u16", Type.u16 }, { "u32", Type.u32 }, { "u64", Type.u64 }, { "usize", Type.uSize },
                                                { "f32", Type.f32 }, { "f64", Type.f64 }, { "void",   Type.Void }
    };
}
public class Lexer
{
    
}