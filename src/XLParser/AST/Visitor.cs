using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLParser.AST
{
    /// <summary>
    /// Interface for AST visitors with parameters and return types.
    /// If you want more than one parameter, make a tuple.
    /// Note that these visitors <strong>have to call the childnodes themselves</strong>.
    /// </summary>
    /// <seealso cref="Asts.PreOrder(IAstNode)"/>
    /// <seealso cref="Asts.PostOrder(IAstNode)"/>
    /// <seealso cref="Asts.LevelOrder(IAstNode)"/>
    /// <example>
    /// public class MultipleParameterVisitor : IAstVisitor{Tuple{Param1,Param2},MyReturnType}
    /// {}
    /// </example>
    public interface IAstVisitor<in TParams, out TReturn>
    {
        TReturn Visit(IAstNode node, TParams Params);
    }

    /// <summary>
    /// Interface for AST visitors.
    /// Note that these visitors <strong>have to call the childnodes themselves</strong>.
    /// </summary>
    /// <seealso cref="Asts.PreOrder(IAstNode)"/>
    /// <seealso cref="Asts.PostOrder(IAstNode)"/>
    /// <seealso cref="Asts.LevelOrder(IAstNode)"/>
    public interface IAstVisitor<out TReturn>
    {
        TReturn Visit(IAstNode node);
    }

    /// <summary>
    /// Interface for AST visitors
    /// </summary>
    public interface IAstVisitor
    {
        void Visit(IAstNode node);
    }

    /// <summary>
    /// Visitor interface for AST
    /// </summary>
    public interface IVisitable
    {
        TReturn Accept<TParam, TReturn>(IAstVisitor<TParam, TReturn> visitor, TParam Param);
        TReturn Accept<TReturn>(IAstVisitor<TReturn> visitor);
        void Accept(IAstVisitor visitor);
    }

    public static class Visitors
    {
        public class LamdbaVisitor : IAstVisitor
        {
            private Action<IAstNode> f { get; }
            public LamdbaVisitor(Action<IAstNode> f) { this.f = f; }
            public void Visit(IAstNode node) => f(node);
        }

        public class LamdbaVisitor<TReturn> : IAstVisitor<TReturn>
        {
            private Func<IAstNode,TReturn> f { get; }
            public LamdbaVisitor(Func<IAstNode, TReturn> f) { this.f = f; }
            public TReturn Visit(IAstNode node) => f(node);
        }

        public class LamdbaVisitor<TParam,TReturn> : IAstVisitor<TParam,TReturn>
        {
            private Func<IAstNode, TParam, TReturn> f { get; }
            public LamdbaVisitor(Func<IAstNode, TParam, TReturn> f) { this.f = f; }
            public TReturn Visit(IAstNode node, TParam p) => f(node, p);
        }

        public static IAstVisitor AsVisitor(Action<IAstNode> f) => new LamdbaVisitor(f);
        public static IAstVisitor<TReturn> AsVisitor<TReturn>(Func<IAstNode,TReturn> f) => new LamdbaVisitor<TReturn>(f);
        public static IAstVisitor<TParam, TReturn> AsVisitor<TParam,TReturn>(Func<IAstNode, TParam, TReturn> f) => new LamdbaVisitor<TParam,TReturn>(f);
    }
}
