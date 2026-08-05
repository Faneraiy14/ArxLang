namespace NyxilumLang.AST;

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

public abstract class StatementNode : AstNode
{
    // Рядок вихідного файлу, з якого почався цей statement — Parser
    // проставляє це один раз, у центральному ParseStatement(), а не в
    // кожному окремому ParseXxxStatement(): так номер рядка гарантовано
    // є на КОЖНОМУ statement-вузлі без ризику забути десь одну гілку.
    public int Line { get; set; }
}
public abstract class ExpressionNode : AstNode { }