namespace ArxLang.AST;

public abstract class AstNode
{
    public abstract string ToString(int indent = 0);
}

public class ProgramNode : AstNode
{
    public List<StatementNode> Statements { get; } = new();
    public override string ToString(int indent = 0)
    {
        var result = $"ProgramNode:\n";
        foreach (var stmt in Statements)
            result += $"  {stmt.ToString(indent + 1)}\n";
        return result;
    }
}

public abstract class StatementNode : AstNode { }
public abstract class ExpressionNode : AstNode { }