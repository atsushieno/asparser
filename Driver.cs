using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Irony.Parsing;

namespace FreeActionScript
{
	public class Driver
	{
		static Dictionary<BnfTerm,string> node_names = new Dictionary<BnfTerm, string> ();

		static void ProcessParseTreeNode (BnfTerm node)
		{
			var nt = node as NonTerminal;
			if (nt == null)
				return;
			if (node_names.ContainsKey (nt))
				return;
			string name = "AS" + ToPascalCase (nt.Name) + "AstNode";
			node_names.Add (nt, name);

			// process descendants
			foreach (var p in nt.Productions)
				foreach (var c in p.RValues)
					ProcessParseTreeNode (c);

			foreach (var p in nt.Productions)
				Console.WriteLine ("\t// {0}", p.GetType ());
			Console.WriteLine ("\tpublic partial class {0} : ActionScriptAstNode", name);
			Console.WriteLine ("\t{");
			Console.Write ("\t\t// {0} productions: ", nt.Productions.Count);
			foreach (var p in nt.Productions)
				foreach (var c in p.RValues) {
					Console.Write (ToPascalCase (c.Name));
					Console.Write (' ');
				}
			Console.WriteLine ();
			if ((nt.Flags & TermFlags.IsList) != 0 && nt.Productions.Count > 0 && nt.Productions [0].RValues [0] == nt) {
				var cnt = nt.Productions [0].RValues.Last () as NonTerminal;
				if (cnt != null)
					Console.WriteLine ("\t\tpublic {0} {1} {{ get; set; }}", node_names [cnt], ToPascalCase (cnt.Name));
			} else {
				foreach (var p in nt.Productions)
					foreach (var c in p.RValues) {
						var cnt = c as NonTerminal;
						if (cnt == null)
							continue;
						Console.WriteLine ("\t\tpublic {0} {1} {{ get; set; }}", node_names [cnt], ToPascalCase (cnt.Name));
					}
			}
			Console.WriteLine ("\t}");
		}

		static string ToPascalCase (string source)
		{
			string ret = "";
			foreach (var s in source.Split ('_'))
				ret += Char.ToUpper (s [0]) + s.Substring (1);
			return ret;
		}

		public static void Main (string [] args)
		{
			if (args.Length < 2) {
				ProcessParseTreeNode (new ActionScriptGrammar ().Root);
				return;
			}

			var grammar = new ActionScriptGrammar ();
			var parser = new Parser (grammar);

			string [] files = Directory.GetFiles (args [0], "*.as", SearchOption.AllDirectories);
			if (!Directory.Exists (args [1]))
				Directory.CreateDirectory (args [1]);

			var trees = new List<ParseTree> ();
			foreach (var arg in files) {
				Console.Write ("parsing {0} ...", arg);
				var s = File.ReadAllText (arg);
				var pt = parser.Parse (s, arg);
				foreach (var msg in pt.ParserMessages)
					Console.WriteLine ("{0} {1} {2} {3}", msg.Level, msg.ParserState, msg.Location, msg.Message);
				if (pt.ParserMessages.Count > 0)
					break;
				grammar.CreateAstNode (parser.Context, pt.Root);
				trees.Add (pt);
				
				string newpath = new Uri (Path.GetFullPath (args [0])).MakeRelative (new Uri (Path.GetFullPath (arg)));
				newpath = Path.Combine (args [1], Path.ChangeExtension (newpath, ".cs"));
				string dir = Path.GetDirectoryName (newpath);
				if (!Directory.Exists (dir))
					Directory.CreateDirectory (dir);
				using (var sw = File.CreateText (newpath))
					new CSharpCodeGenerator ((CompileUnit) pt.Root.AstNode, sw).GenerateCode ();
				Console.WriteLine ("done");
			}
		}
	}
}
