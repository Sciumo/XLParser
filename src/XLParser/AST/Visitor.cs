using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLParser.AST
{
    // Confused about visitors?
    // I found this article excellent: http://www.codeproject.com/Articles/588882/TheplusVisitorplusPatternplusExplained
    // It really explains what visitors do, and the differences between iterations
    // Note C# 4 introduced native double dispatch, which is why we do not have an interface and only a base class you can inherit from if you want

    /// <summary>
    /// Base class for AST Visitors.
    /// To use this, subclass this and implement Visit(? : IAstNode, TParams param) methods for every node type you want to handle,
    /// and optionally the VisitUnhandled(IAstNode, TParams param) method.
    /// </summary>
    /// <seealso cref="Asts.PreOrder"/>
    /// <seealso cref="Asts.PostOrder"/>
    public abstract class Visitor<TParams, TReturn>
    {
        protected virtual TReturn VisitUnhandled(IAstNode node, TParams param)
        {
            throw new ArgumentException($"Class {GetType()} cannot handle node type {node.GetType()}", nameof(node));
        }

        private IAstNode previousVisited;
        public TReturn Visit(IAstNode node, TParams param)
        {
            // If we visited this node, we're recursively calling this dispatcher, so go to the default handler
            if (ReferenceEquals(node, previousVisited))
            {
                return VisitUnhandled(node, param);

            }
            else
            {
                previousVisited = node;
                try
                {
                    return Visit((dynamic) node, param);
                }
                finally
                {
                    // Make sure to set previousVisited to null, otherwise consecutive calls could go wrong.
                    previousVisited = null;
                }
            }
        }
    }

    /// <summary>
    /// Base class for AST Visitors.
    /// To use this, subclass this and implement Visit(? : IAstNode) methods for every node type you want to handle,
    /// and optionally the VisitUnhandled(IAstNode) method.
    /// </summary>
    /// <seealso cref="Asts.PreOrder"/>
    /// <seealso cref="Asts.PostOrder"/>
    public abstract class Visitor<TReturn>
    {
        protected virtual TReturn VisitUnhandled(IAstNode node)
        {
            throw new ArgumentException($"Class {GetType()} cannot handle node type {node.GetType()}", nameof(node));
        }

        private IAstNode previousVisited;
        public TReturn Visit(IAstNode node)
        {
            // If we visited this node, we're recursively calling this dispatcher, so go to the default handler
            if (ReferenceEquals(node, previousVisited))
            {
                return VisitUnhandled(node);
            }
            else
            {
                previousVisited = node;
                try
                {
                    return Visit((dynamic) node);
                }
                finally
                {
                    // Make sure to set previousVisited to null, otherwise consecutive calls could go wrong.
                    previousVisited = null;
                }
            }
        }
    }

    /// <summary>
    /// Base class for AST Visitors.
    /// To use this, subclass this and implement Visit(? : IAstNode) methods for every node type you want to handle,
    /// and optionally the VisitUnhandled(IAstNode) method.
    /// </summary>
    /// <seealso cref="Asts.PreOrder"/>
    /// <seealso cref="Asts.PostOrder"/>
    public abstract class Visitor
    {
        protected virtual void VisitUnhandled(IAstNode node)
        {
            throw new ArgumentException($"Class {GetType()} cannot handle node type {node.GetType()}", nameof(node));
        }

        private IAstNode previousVisited;
        public void Visit(IAstNode node)
        {
            // If we visited this node, we're recursively calling this dispatcher, so go to the default handler
            if (ReferenceEquals(node, previousVisited))
            {
                VisitUnhandled(node);
            }
            else
            {
                previousVisited = node;
                try
                {
                    Visit((dynamic) node);
                }
                finally
                {
                    // Make sure to set previousVisited to null, otherwise consecutive calls could go wrong.
                    previousVisited = null;
                }
            }
        }
    }
}
