using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLParser.AST
{
    /// <summary>
    /// Formula language Dialects
    /// </summary>
    public enum Dialect
    {
        Excel2007
    };

    /// <summary>
    /// Prints an AST to an Excel formula string
    /// </summary>
    public class Printer : IAstVisitor<string>
    {
        /// <summary>
        /// Whether to include the precedeing "=" for formulas and "{=...}" for array formulas.
        /// </summary>
        public bool IncludeEquals { get; set; } = true;

        /// <summary>
        /// Dialect to use
        /// </summary>
        public Dialect Dialect { get; set; } = Dialect.Excel2007;

        public string Visit(IAstNode node)
        {
            throw new NotImplementedException($"Printer cannot handle node type {node.GetType()}");
        }

        public string Visit(Formula node)
        {
            var sub = node.Accept(this);
            if (IncludeEquals)
            {
                return node.IsArrayFormula ? $"{{={sub}}}" : $"={sub}";
            } 
            return sub;
        }
        
        public string Visit(EmptyArgument n) => "";

        public string Visit(NamedFunctionCall n)
        {
            var childs = string.Join(",", n.ChildNodes.Select(child => Parenthesize(n, child)));
            return $"{n.FunctionName}({childs})";
        }

        public string Visit(UnOp n)
        {
            if (n.Operator.IsUnaryPreFix())
            {
                return n.Operator.Symbol() + n.Argument.Accept(this);
            }
            else
            {
                return n.Argument.Accept(this) + n.Operator.Symbol();
            }
        }
        
        public string Visit(BinOp n)
        {
            if (n.Operator.IsReferenceOperator())
            {
                return n.LArgument.Accept(this) + n.Operator.Symbol() + n.RArgument.Accept(this);
            }
            else
            {
                return n.LArgument.Accept(this) + " " + n.Operator.Symbol() + " " + n.RArgument.Accept(this);
            }
        }

        private string Parenthesize(FunctionCall parent, IAstNode child) => MustBeParenthesised(parent, child) ? $"({child.Accept(this)})" : child.Accept(this);

        private static bool MustBeParenthesised(FunctionCall parentraw, IAstNode childraw)
        {
            var child = childraw as Op;
            if (child == null) return false;
            
            // Unions must be parenthesised as arguments
            if (child.Operator == Operator.Union && parentraw is NamedFunctionCall) return true;

            // Check if parent child precedence is smaller than parent precedence
            var parent = parentraw as Op;
            return parent != null && parent.Precedence > child.Precedence;
        }
    }
}
