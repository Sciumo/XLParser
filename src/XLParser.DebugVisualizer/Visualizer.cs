using System;
using System.Diagnostics;
using Irony.Parsing;
//using XLParser;

// Compiling this file and placing the resulting DLL in My Documents\Visual Studio 20XX\Visualizers will improve what the VS debugger displays for parse trees
[assembly: DebuggerTypeProxy(typeof(XLParser.DebugVisualizer.VisualizerPrint), Target = typeof(Irony.Parsing.ParseTreeNode))]

namespace XLParser.DebugVisualizer
{
    public class VisualizerPrint
    {
        public ParseTreeNode _base { get; }

        public VisualizerPrint(ParseTreeNode node)
        {
            _base = node;
        }

        // Native properties
        public ParseTreeNodeList ChildNodes => _base.ChildNodes;
        public string Token => _base.Token?.Text ?? "";

        // Computed properties
        //public string Print => _base.Print();
        //public string Type => _base.Type();
        //public ParseTreeNode RelevantChildNode => _base.SkipToRelevant();
    }
}