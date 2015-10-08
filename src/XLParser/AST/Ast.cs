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
    public interface IAstNode : IEquatable<IAstNode>
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

    public interface IExpr : IAstNode
    {

    }

    public interface IRefExpr : IExpr
    {

    }

    public interface IFunctionCall : IAstNode
    {
        bool IsBuiltIn { get; }
        bool IsConditional { get; }
        bool CanReturnReference { get; }
        IEnumerable<IExpr> Arguments { get; }
        string FunctionName { get; }
    }

    public interface IOp : IFunctionCall
    {
        Operator Operator { get; }
        int Precedence { get; }
    }

    public interface IConstant : IAstNode { }

    public interface IPrefix : IAstNode { }


    /// <summary>
    /// Base class for XLParser AST Nodes
    /// </summary>
    /// <remarks>
    /// Irony has an AST system, but we did not find it very user-friendly. This also allows us to decouple Irony should the need arise.
    /// 
    /// Unfortunatly C# 6 (the newest version at the type of writing) does not support Algebraic Data Types/Pattern matching yet, which would be *really* convenient for the AST.
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
            if (ReferenceEquals(this, other)) return true;

            return GetType() == other.GetType()
                // Compare all children
                && ChildNodes.Count() == other.ChildNodes.Count()
                && ChildNodes.Zip(other.ChildNodes, (a,b)=> a.Equals(b)).All(p=>p);
        }

        public override int GetHashCode()
        {
            int hash = unchecked (PRIME1*PRIME2 + GetType().GetHashCode());
            return ChildNodes.Aggregate(hash, (current, child) => unchecked (current*PRIME2 + child.GetHashCode()));
        }

        protected const int PRIME1 = 1631027;
        protected const int PRIME2 = 4579711;
    }

    public class Root : AstNode
    {
        public IExpr Expr { get; }
        public bool IsArrayFormula { get; }

        public Root(IExpr expr, bool isArrayFormula = false)
        {
            IsArrayFormula = isArrayFormula;
            Expr = expr;
        }

        public override IEnumerable<IAstNode> ChildNodes => new[] { Expr };

        public override bool Equals(IAstNode other) => (other as Root)?.IsArrayFormula == IsArrayFormula && base.Equals(other);

        public override int GetHashCode() => unchecked(base.GetHashCode() * PRIME2 + IsArrayFormula.GetHashCode());
    }

    /// <summary>
    /// This is a Dummy node for empty arguments of functions
    /// </summary>
    public class EmptyArgument : AstNode, IExpr
    {
        public override IEnumerable<IAstNode> ChildNodes => Enumerable.Empty<IAstNode>();
    }

    public abstract class FunctionCall : AstNode, IExpr, IFunctionCall
    {
        protected readonly List<IExpr> arguments;

        public IEnumerable<IExpr> Arguments => arguments.AsReadOnly();

        public string FunctionName { get; }

        public override IEnumerable<IAstNode> ChildNodes => Arguments;

        public abstract bool IsBuiltIn { get; }
        // TODO: Keep? Implement?
        public virtual bool IsConditional { get { throw new NotImplementedException(); } }
        public virtual bool CanReturnReference => this is IRefExpr;

        protected FunctionCall(string functionName, IEnumerable<IExpr> args)
        {
            FunctionName = functionName;
            arguments = new List<IExpr>(args);
        }

        public override bool Equals(IAstNode other) => (other as FunctionCall)?.FunctionName == FunctionName && base.Equals(other);

        public override int GetHashCode() => unchecked(base.GetHashCode() * PRIME2 + FunctionName.GetHashCode());
    }

    public class NamedFunctionCall : FunctionCall
    {
        public override bool IsBuiltIn => true;

        public NamedFunctionCall(string functionName, IEnumerable<IExpr> args) : base(functionName, args)
        {}
    }

    public class NamedRefFunctionCall : NamedFunctionCall, IRefExpr
    {
        public NamedRefFunctionCall(string functionName, IEnumerable<IExpr> args) : base(functionName, args)
        { }
    }

    public class UDFunctionCall : AstNode, IFunctionCall, IRefExpr
    {
        public bool IsBuiltIn => false;

        public bool IsConditional => false;

        public bool CanReturnReference => true;

        private readonly List<IExpr> arguments;

        public IEnumerable<IExpr> Arguments => arguments.AsReadOnly();

        public string FunctionName { get; }

        public override IEnumerable<IAstNode> ChildNodes => Arguments;

        public UDFunctionCall(string functionName, IEnumerable<IExpr> args)
        {
            FunctionName = FunctionName;
            arguments = new List<IExpr>(args);
        }

        public override bool Equals(IAstNode other) => (other as FunctionCall)?.FunctionName == FunctionName && base.Equals(other);

        public override int GetHashCode() => unchecked(base.GetHashCode() * PRIME2 + FunctionName.GetHashCode());
    }

    public abstract class Op : FunctionCall, IOp
    {
        public Operator Operator { get; }

        public override bool IsBuiltIn => true;

        protected Op(Operator op, IEnumerable<IExpr> args) : base(op.Symbol(), args)
        {
            Operator = op;
        }

        public virtual int Precedence => Operator.Precedence();
    }

    public class UnOp : Op
    {
        public IExpr Argument => arguments[0];

        public override int Precedence => Operator.IsUnaryPreFix() ? Operators.Precedences.UnaryPreFix : Operators.Precedences.UnaryPostFix;

        public UnOp(Operator op, IExpr argument) :  base(op, new List<IExpr> { argument })
        {
            if(!op.IsUnary()) throw new ArgumentException($"Not an unary operator <<{op.Symbol()}>>", nameof(op));
        }
    }

    public class BinOp : Op
    {
        public IExpr LArgument => arguments[0];
        public IExpr RArgument => arguments[1];

        public BinOp(Operator op, IExpr lArgument, IExpr rArgument) : base(op, new [] { lArgument, rArgument} )
        {
            if (!op.IsBinary()) throw new ArgumentException($"Not a binary operator <<{op.Symbol()}>>", nameof(op));
        }
    }

    public class RefOp : BinOp, IRefExpr
    {
        public new IRefExpr LArgument => (IRefExpr)base.LArgument;
        public new IRefExpr RArgument => (IRefExpr)base.RArgument;

        public RefOp(Operator op, IRefExpr lArgument, IRefExpr rArgument) : base(op, lArgument, rArgument )
        {
            if (!op.IsReferenceOperator()) throw new ArgumentException($"Not a reference operator <<{op.Symbol()}>>", nameof(op));
        }
    }

    public abstract class Reference : AstNode, IRefExpr
    {
        Prefix Prefix { get; }
        ReferenceItem ReferenceItem { get; }

        protected Reference(Prefix prefix, ReferenceItem item)
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
