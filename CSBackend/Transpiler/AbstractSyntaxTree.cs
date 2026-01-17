namespace CSBackend;
// --- Top Level ---
public partial class AbstractSyntaxTree
{
    public List<Declaration> Declarations = new();
}

public abstract class Declaration { }

public class FunctionDecl : Declaration
{
    public string          Name       = "";
    public Type            ReturnType = new NamedType("void"); // default to void
    public List<Parameter> Parameters = new();
    public Block           Body       = new();
}

public class Parameter
{
    public Type   Type = new NamedType("()");
    public string Name = "";
}

// --- Types ---
public abstract class Type { }

public class NamedType : Type
{
    public string Name;
    public NamedType(string name) => Name = name;
}

public class ArrayType : Type
{
    public Type    ElementType;
    public string? Size; // could be "256" or null for dynamic
    public ArrayType(Type element, string? size = null)
    {
        ElementType = element;
        Size = size;
    }
}

// --- Statements ---
public abstract class Statement { }

public class Block : Statement
{
    public List<Statement> Statements = new();
}

public class ReturnStmt : Statement
{
    public Expression? Value;
}

public class ExprStmt : Statement
{
    public Expression Expr = new LiteralExpr("");
}

// --- Expressions ---
public abstract class Expression { }

public class BinaryExpr : Expression
{
    public Expression Left     = new LiteralExpr("");
    public string     Operator = "";
    public Expression Right    = new LiteralExpr("");
}

public class CallExpr : Expression
{
    public string           Name      = "";
    public List<Expression> Arguments = new();
}

public class IdentifierExpr : Expression
{
    public string Name = "";
}

public class LiteralExpr : Expression
{
    public string Value;
    public LiteralExpr(string value) => Value = value;
}