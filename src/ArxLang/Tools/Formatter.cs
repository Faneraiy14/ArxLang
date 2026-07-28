using ArxLang.Core;
using ArxLang.AST;

namespace ArxLang.Tools;

public class Formatter
{
    public string Format(string sourceCode)
    {
        try
        {
            var lexer = new Lexer(sourceCode);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var program = parser.ParseProgram();

            return FormatNode(program, 0);
        }
        catch
        {
            return sourceCode;
        }
    }

    private string FormatNode(AstNode node, int indent)
    {
        var padding = new string(' ', indent * 4);

        switch (node)
        {
            case ProgramNode program:
                var result = "";
                foreach (var stmt in program.Statements)
                    result += FormatNode(stmt, indent) + "\n";
                return result.TrimEnd();

            case VariableDeclaration varDecl:
                var init = varDecl.Initializer != null ? $" = {FormatNode(varDecl.Initializer, 0)}" : "";
                var type = varDecl.TypeAnnotation != null ? $": {varDecl.TypeAnnotation}" : "";
                return $"{padding}var {varDecl.Name}{type}{init}";

            case IfStatement ifStmt:
                var cond = FormatNode(ifStmt.Condition, 0);
                var then = FormatNode(ifStmt.ThenBlock, indent);
                var elsePart = ifStmt.ElseBlock != null ? $" else {FormatNode(ifStmt.ElseBlock, indent)}" : "";
                return $"{padding}if ({cond}) {then}{elsePart}";

            case WhileStatement whileStmt:
                var wCond = FormatNode(whileStmt.Condition, 0);
                var body = FormatNode(whileStmt.Body, indent);
                return $"{padding}while ({wCond}) {body}";

            case ForStatement forStmt:
                var start = FormatNode(forStmt.Start, 0);
                var range = forStmt.End != null ? $"{start}..{FormatNode(forStmt.End, 0)}" : start;
                var forBody = FormatNode(forStmt.Body, indent);
                return $"{padding}for {forStmt.VariableName} in {range} {forBody}";

            case BlockStatement block:
                var blockResult = "{\n";
                foreach (var stmt in block.Statements)
                    blockResult += FormatNode(stmt, indent + 1) + "\n";
                blockResult += $"{padding}}}";
                return blockResult;

            case FunctionDeclaration func:
                var paramsStr = string.Join(", ", func.Parameters.Select(p => $"{p.Name}: {p.Type}"));
                var returnStr = func.ReturnType != null ? $" -> {func.ReturnType}" : "";
                var funcBody = FormatNode(func.Body, indent);
                return $"{padding}func {func.Name}({paramsStr}){returnStr} {funcBody}";

            case PrintStatement print:
                var printExpr = FormatNode(print.Expression, 0);
                return $"{padding}print({printExpr})";

            case ReturnStatement ret:
                var retVal = ret.Value != null ? FormatNode(ret.Value, 0) : "";
                return $"{padding}return {retVal}".TrimEnd();

            case BinaryExpression binary:
                var left = FormatNode(binary.Left, 0);
                var right = FormatNode(binary.Right, 0);
                return $"{left} {binary.Operator} {right}";

            case LiteralExpression literal:
                if (literal.Value is string s)
                    return $"\"{s}\"";
                return literal.Value?.ToString() ?? "null";

            case VariableExpression varExpr:
                return varExpr.Name;

            case CallExpression call:
                var args = string.Join(", ", call.Arguments.Select(a => FormatNode(a, 0)));
                return $"{call.FunctionName}({args})";

            case ArrayLiteralExpression arr:
                var elements = string.Join(", ", arr.Elements.Select(e => FormatNode(e, 0)));
                return $"[{elements}]";

            case IndexExpression idx:
                var array = FormatNode(idx.Array, 0);
                var index = FormatNode(idx.Index, 0);
                return $"{array}[{index}]";

            case FunctionExpression fnExpr:
                var lambdaParams = string.Join(", ", fnExpr.Parameters.Select(p => p.Name));
                return $"func({lambdaParams}) {FormatNode(fnExpr.Body, indent)}";

            case TryStatement tryStmt:
                var tryBody = FormatNode(tryStmt.TryBlock, indent);
                var catchBody = FormatNode(tryStmt.CatchBlock, indent);
                return $"{padding}try {tryBody} catch ({tryStmt.CatchVariableName}) {catchBody}";

            case ThrowStatement throwStmt:
                return $"{padding}throw {FormatNode(throwStmt.Value, 0)}";

            case ImportStatement importStmt:
                return $"{padding}import \"{importStmt.Path}\"";

            default:
                return node.ToString();
        }
    }
}