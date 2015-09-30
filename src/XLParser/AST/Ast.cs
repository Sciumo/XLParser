using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Irony.Ast;
using Irony.Parsing;

namespace XLParser.AST
{
    public interface IAstNodeFromParseTree
    {
        /// <summary>
        /// Corresponding parse tree node
        /// </summary>
        ParseTreeNode ParseTreeNode { get; set; }
    }

    
    /// <summary>
    /// Interface for XLParser AST Nodes.
    /// </summary>
    public interface IAstNode : IEquatable<IAstNode>, IVisitable
    {
        /// <summary>
        /// All child nodes
        /// </summary>
        /// <remarks>
        /// Prefer implementing this with a list or array
        /// </remarks>
        IEnumerable<IAstNode> ChildNodes { get; }

        /// <summary>
        /// Whether this is a leaf node
        /// </summary>
        bool IsLeaf { get; }
    }

    /// <summary>
    /// Base class for XLParser AST Nodes
    /// </summary>
    /// <remarks>
    /// Irony has an AST system, but we did not find it very user-friendly. This also allows us to decouple Irony should the need arise.
    /// 
    /// Unfortunatly C# 6 (the newest version at the type of writing) does not support Algebraic Data Types/Pattern matching yet, which would be *really* convenient for this class.
    /// The properties of <a href="https://github.com/dotnet/roslyn/issues/206">the proposal</a> as it was in late 2015 have been followed as closely as possible, but we are obviously limited by language support at the time.
    /// Inspiration has also been taken from <a href="http://bugsquash.blogspot.co.uk/2012/01/encoding-algebraic-data-types-in-c.html">this blog post</a> which bases it's implementation on F#.
    /// </remarks>
    public abstract class AstNode : IAstNode
    {
        /// <summary>
        /// Return all child nodes
        /// </summary>
        public abstract IEnumerable<IAstNode> ChildNodes { get; }

        public bool IsLeaf => !ChildNodes.Any();

        public override bool Equals(object other) => Equals(other as IAstNode);

        public virtual bool Equals(IAstNode other)
        {
            if (ReferenceEquals(other, null)) return false;

            return GetType() == other.GetType()
                // Compare all children
                && ChildNodes.Count() == other.ChildNodes.Count()
                && ChildNodes.Zip(other.ChildNodes, (a,b)=> a.Equals(b)).All(p=>p);
        }

        public override int GetHashCode()
        {
            const int prime1 = 1631027;
            const int prime2 = 4579711;
            int hash = unchecked (prime1*prime2 + GetType().GetHashCode());
            return ChildNodes.Aggregate(hash, (current, child) => unchecked (current*prime2 + child.GetHashCode()));
        }

        public TReturn Accept<TParam, TReturn>(IAstVisitor<TParam, TReturn> visitor, TParam Params)
        {
            dynamic node = this;
            return visitor.Visit(node, Params);
        }

        public TReturn Accept<TReturn>(IAstVisitor<TReturn> visitor)
        {
            dynamic node = this;
            return visitor.Visit(node);
        }

        public void Accept(IAstVisitor visitor)
        {
            dynamic node = this;
            visitor.Visit(node);
        }
    }

    public class Formula : AstNode
    {
        public Expr Expr { get; }
        public bool IsArrayFormula { get; }

        public Formula(Expr expr, bool isArrayFormula = false)
        {
            IsArrayFormula = isArrayFormula;
            Expr = expr;
        }

        public override IEnumerable<IAstNode> ChildNodes => new[] { Expr };
    }


    public abstract class Expr : AstNode
    {}

    /// <summary>
    /// This is a Dummy node for empty arguments of functions
    /// </summary>
    public class EmptyArgument : Expr
    {
        public override IEnumerable<IAstNode> ChildNodes => Enumerable.Empty<IAstNode>();
    }

    public abstract class FunctionCall : Expr
    {
        public bool IsBuiltIn { get; }

        public abstract IEnumerable<Expr> Arguments { get; }

        public string FunctionName { get; }

        public override IEnumerable<IAstNode> ChildNodes => Arguments;

        public bool CanReturnReference { get; }

        protected FunctionCall(string functionName, bool isBuiltIn = true, bool canReturnReference = false)
        {
            FunctionName = functionName;
            IsBuiltIn = isBuiltIn;
            CanReturnReference = canReturnReference;
        }
    }

    public class NamedFunctionCall : FunctionCall
    {
        private readonly List<Expr> arguments;

        public override IEnumerable<Expr> Arguments => arguments.AsReadOnly();

        public NamedFunctionCall(string functionName, IEnumerable<Expr> args , bool isBuiltIn = true, bool canReturnReference = false) : base(functionName, isBuiltIn, canReturnReference)
        {
            arguments = new List<Expr>(args);
        }
    }

    public abstract class Op : FunctionCall
    {
        public Operator Operator { get; }

        protected Op(Operator op) : base(op.Symbol(), true, op.IsReferenceOperator())
        {
            Operator = op;
        }

        public virtual int Precedence => Operator.Precedence();

        
    }

    public class UnOp : Op
    {
        public Expr Argument { get; }

        public override IEnumerable<Expr> Arguments => new [] { Argument };

        public override int Precedence => Operator.IsUnaryPreFix() ? Operators.Precedences.UnaryPreFix : Operators.Precedences.UnaryPostFix;

        public UnOp(Operator op, Expr argument) :  base(op)
        {
            if(!op.IsUnary()) throw new ArgumentException("Not an unary operator", nameof(op));
            Argument = argument;
        }
    }

    public class BinOp : Op
    {
        public Expr LArgument { get; }
        public Expr RArgument { get; }

        public override IEnumerable<Expr> Arguments => new[] {LArgument, RArgument};

        public BinOp(Operator op, Expr lArgument, Expr rArgument) : base(op)
        {
            if (!op.IsBinary()) throw new ArgumentException("Not an binary operator", nameof(op));

            LArgument = lArgument;
            RArgument = rArgument;
        }
    }

    public abstract class Reference : Expr
    {
        Prefix Prefix { get; }
        ReferenceItem ReferenceItem { get; }

        protected Reference(Prefix prefix, Expr expr)
        {
        }
    }

    public abstract class ReferenceItem : IAstNode
    {
        
    }

    public class Prefix : AstNode
    {
        public string FilePath { get; }
        public bool HasFilePath => FilePath != null;

        private readonly int? fileNumber;
        public int FileNumber => fileNumber.Value;
        public bool HasFileNumber => fileNumber.HasValue;

        public string FileName { get; }
        public bool HasFileName => FileName != null;

        public bool HasFile => HasFileName || HasFileNumber;

        public string Sheet { get; }
        public bool HasSheet => Sheet != null;

        public string MultipleSheets { get; }
        public bool HasMultipleSheets => MultipleSheets != null;

        public bool IsQuoted { get; }

        public Prefix(Reference parent, string sheet = null, int? fileNumber = null, string fileName = null, string filePath = null, string multipleSheets = null, bool isQuoted = false) : base(parent)
        {
            Sheet = sheet;
            this.fileNumber = fileNumber;
            FileName = fileName;
            FilePath = filePath;
            MultipleSheets = multipleSheets;
            IsQuoted = isQuoted;
        }

        public override string Print()
        {
            string res = "";
            if (IsQuoted) res += "'";
            if (HasFilePath) res += FilePath;
            if (HasFileNumber) res += $"[{FileNumber}]";
            if (HasFileName) res += $"[{FileName}]";
            if (HasSheet) res += Sheet;
            if (HasMultipleSheets) res += MultipleSheets;
            if (IsQuoted) res += "'";
            res += "!";
            return res;
        }

        public override IEnumerable<IAstNode> ChildNodes => Enumerable.Empty<IAstNode>();
    }
}
