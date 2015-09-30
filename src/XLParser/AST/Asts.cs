using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLParser.AST
{
    public static class Asts
    {
        /// <summary>
        /// Iterate over all nodes in the most efficient way available.
        /// </summary>
        public static IEnumerable<IAstNode> AllNodes(this IAstNode root) => PreOrder(root); 

        /// <summary>
        /// Traverse the tree in pre-order
        /// </summary>
        public static IEnumerable<IAstNode> PreOrder(this IAstNode root)
        {
            var stack = new Stack<IAstNode>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                yield return node;

                // Push children on in reverse order so that they will
                // be evaluated left -> right when popped.
                foreach (var child in ReverseChildrenEfficient(node))
                {
                    stack.Push(child);
                }
            }
        }

        private static IEnumerable<IAstNode> ReverseChildrenEfficient(IAstNode node)
        {
            // Check if it's a list, if so we can do it a lot more efficiently than the LINQ Reverse method which always buffers
            // Using the XLParser implementation it should always be a list/array
            if (node.ChildNodes is IList<IAstNode>)
            {
                var childL = (IList<AstNode>)node.ChildNodes;
                for (int i = childL.Count - 1; i >= 0; i--)
                {
                    yield return childL[i];
                }
            }
            else
            {
                foreach (var child in node.ChildNodes.Reverse())
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Traverse the tree in post-order
        /// </summary>
        public static IEnumerable<IAstNode> PostOrder(this IAstNode root)
        {
            var discoverStack = new Stack<IAstNode>();
            var returnStack = new Stack<IAstNode>();
            discoverStack.Push(root);
            while (discoverStack.Count > 0)
            {
                var node = discoverStack.Pop();
                returnStack.Push(node);
                // Left childnode get to the bottom of the discoverstack, so to the top of the return stack
                foreach (var child in node.ChildNodes)
                {
                    discoverStack.Push(child);
                }
            }
            return returnStack;
        }

        /// <summary>
        /// Traverse the tree in level-order
        /// </summary>
        public static IEnumerable<IAstNode> LevelOrder(this IAstNode root)
        {
            var queue = new Queue<IAstNode>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                yield return node;

                foreach (var child in node.ChildNodes)
                {
                    queue.Enqueue(child);
                }
            }
        }

        /// <summary>
        /// Returns a map with the parent of every node.
        /// </summary>
        public static IDictionary<IAstNode, IAstNode> Parents(this IAstNode root)
        {
            var dict = new Dictionary<IAstNode, IAstNode>();

            foreach (var node in root.AllNodes())
            {
                foreach (var child in node.ChildNodes)
                {
                    dict.Add(child, node);
                }
            }

            return dict;
        }
    }
}
