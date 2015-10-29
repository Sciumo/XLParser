using System;
using System.Diagnostics;
using Irony.Parsing;
//using XLParser;

// Compiling this file and placing the resulting DLL in My Documents\Visual Studio 20XX\Visualizers will improve what the VS debugger displays for parse trees
[assembly: DebuggerTypeProxy(typeof(VisualizerPrint), Target = typeof(Irony.Parsing.ParseTreeNode))]

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

    public readonly string Blah = "This is a test";

    public override string ToString() => "This is another test";

    // Computed properties
    //public string Print => _base.Print();
    //public string Type => _base.Type();
    //public ParseTreeNode RelevantChildNode => _base.SkipToRelevant();
}